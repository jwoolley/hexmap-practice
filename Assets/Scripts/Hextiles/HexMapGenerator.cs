using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using cakeslice;

public class HexMapGenerator : MonoBehaviour {
    [SerializeField]
    GameObject HexTilePrefab;

    [SerializeField]
    [Range(1, 16)]
    int numHexRingLevels;

    [SerializeField]
    TilePlacementMode tilePlacementMode = TilePlacementMode.RANDOM_CONNECTED;

    [SerializeField]
    [Range(1.0f, 5.0f)]
    float freeEdgeWeight = 1.0f;

    [SerializeField]
    [Range(0, 5)]
    int bufferCellsForRandMode = 0;

    private bool randomizePlacement = false;

    [SerializeField]
    bool randomizeColors = true;

    [SerializeField]
    bool randomizeRegionTileColors;

    [SerializeField]
    bool useExperimentalPlacement;

    [SerializeField]
    bool showHexGuiDebugText;

    enum TilePlacementMode {
        FILLED,
        RANDOM_CONNECTED,
        FILLED_IMPROVED,
    }

    // MATERIALS LOGIC (START) ======+=================================================================================
    // used for randomizeRegionTileColors
    // should use regionId as key instead; leaving as-is now for clarity/waiting until regionId type is changed to uuid

    private static Dictionary<HexTileRegion, Material> REGION_MATERIALS = new Dictionary<HexTileRegion, Material>();    
    private static Material referenceMaterial;

    public static void assignRandomRegionMaterial(HexTileRegion region) {
        if (!REGION_MATERIALS.ContainsKey(region)) {
            Material material = getNewMaterialWithColor(UnityEngine.Random.ColorHSV());
            REGION_MATERIALS[region] = material;
        } else {
            Debug.LogWarning($"Tried to assign material for region with existing material (Region: {region.regionId})");
        }
    }

    public static void assignHexMaterialFromRegion(HexTileRegion region, UnityMapHex hex) {
        if (region == null) {
            Debug.LogWarning($"Can't find material for region null");
            return;
        } else if (!REGION_MATERIALS.ContainsKey(region)) {
            Debug.LogWarning($"Can't find material for region {region.regionId}");
            return;
        }
        if (_generatorInstance.randomizeRegionTileColors) {
            hex.changeMeshMaterial(REGION_MATERIALS[region]);
        }
    }
    // MATERIALS LOGIC (END) ==========================================================================================

    // ANIMATION CONSTANTS
    const float HEX_FALL_DURATION_INITIAL_TIME = 0.67f;
    const float HEX_FALL_DURATION_FINAL_TIME = 0.33f;

    // PHYSICAL MAP CONSTANTS 
    readonly float hexTileOffset_X = 1.76f;
    readonly float hexTileOffset_Y = 1.52f;
    float startZOffset = -8.0f;

    // DEBUG UI VARIABLES
    static public Dictionary<UnityMapHex, string> DEBUG_HEX_LABEL_LIST = new Dictionary<UnityMapHex, string>();
    static private Color debugGuiLabelColor;
    static private List<UnityMapHex> labelHexes = new List<UnityMapHex>();
    static readonly private Color guiLabelColor = Color.white;

    private Tuple<int, int> mapMaxDimensions;

    private UnityMapHex[,] hexPositionGrid;
    private static HexTileRegionCatalog hexTileRegionGroup;

    private EnqueuedRelativePlacementTile[,] placementGrid;
    List<EnqueuedRelativePlacementTile> enqueuedTiles = new List<EnqueuedRelativePlacementTile>();
    List<HexEdgeEnum> _orderedEdges;

    static Dictionary<HexTileColor, Material> referenceHexes;

    //readonly TimeTransformer timeTransformer = new TimeTransformerSmoothStop4();
    readonly TimeTransformer timeTransformer = new TimeTransformerSmoothStep2();

    private bool readyToGenerateMap = false;

    static HexMapGenerator _generatorInstance;

    void Start() {
        _generatorInstance = this;
        GenerateHexMap();
    }

    void OnDrawGizmos() {
        #if UNITY_EDITOR
        if (showHexGuiDebugText) {
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = guiLabelColor;
            labelStyle.alignment = TextAnchor.LowerCenter;
            labelHexes
                .Where(hex => hex != null && hex.gameObject != null)
                .ToList()
                .ForEach(hex => {
                    HexTileRegion region = hexTileRegionGroup.getRegionContainingHex(hex);
                    if (randomizeRegionTileColors) {
                        labelStyle.normal.textColor = referenceHexes[region.color].color;
                    }
                    Handles.Label(hex.gameObject.transform.position,
                        $"{hex.hexId}\n[R:{region.regionId}/{region.color.ToString().Substring(0,1)}]",
                        labelStyle);
                });

            labelStyle.normal.textColor = debugGuiLabelColor;
            DEBUG_HEX_LABEL_LIST.Keys
                .Where(hex => hex != null)
                .ToList()
                .ForEach(hex => {
                    Handles.Label(hex.gameObject.transform.position, $"\n\n<b>{DEBUG_HEX_LABEL_LIST[hex]}</b>", labelStyle);
                });
        }
        #endif
    }
    void OnApplicationQuit() {
        labelHexes.Clear();
        DEBUG_HEX_LABEL_LIST.Clear();
    }

    public static Material getNewMaterialWithColor(Color color) {
        Material material = Instantiate(referenceMaterial);
        material.SetColor("_Color", color);
        return material;
    }

    static Material getMaterial(HexTileColor color) {
        return referenceHexes[color];
    }

    void intializeReferenceHexes() {
        referenceHexes = new Dictionary<HexTileColor, Material>();
        referenceHexes.Add(HexTileColor.RED, getNewMaterialWithColor(Color.red));
        referenceHexes.Add(HexTileColor.BLUE, getNewMaterialWithColor(Color.blue));
        referenceHexes.Add(HexTileColor.GREEN, getNewMaterialWithColor(Color.green));
        referenceHexes.Add(HexTileColor.PURPLE, getNewMaterialWithColor(new Color(0.6f, 0.0f, 1.0f, 1.0f)));
        referenceHexes.Add(HexTileColor.ORANGE, getNewMaterialWithColor(new Color(1.0f, 0.5f, 0.0f, 1.0f)));
        referenceHexes.Add(HexTileColor.YELLOW, getNewMaterialWithColor(Color.yellow));
    }

    void GenerateHexMap() {
        // initialize static data
        referenceMaterial = HexTilePrefab.GetComponent<MeshRenderer>().sharedMaterial;

        UnityMapHex.initializeReferenceMesh(HexTilePrefab.GetComponent<MeshRenderer>());

        if (useExperimentalPlacement) {
            GenerateHexMapExperimental(numHexRingLevels);
        } else {
            GenerateHexMapSimple();
        }
    }

    void GenerateHexMapSimple() {
        const int MAP_HEX_WIDTH_SIMPLE = 12;
        const int MAP_HEX_HEIGHT_SIMPLE = 8;

        float initialOffsetX = (1 - MAP_HEX_WIDTH_SIMPLE) / 2.0f;
        float initialOffsetY = (1 - MAP_HEX_HEIGHT_SIMPLE) / 2.0f;

        intializeReferenceHexes();

        for (int i = 0; i < MAP_HEX_HEIGHT_SIMPLE; i++) {
            float y = i + initialOffsetY;
            for (int j = 0; j < MAP_HEX_WIDTH_SIMPLE; j++) {
                float x = j + initialOffsetX;
                GameObject tempGameObject = Instantiate(HexTilePrefab);

                if (randomizeColors) {
                    tempGameObject.GetComponent<MeshRenderer>().sharedMaterial = getMaterial(HexTileColorExtensions.getRandomColor());
                }

                if (i % 2 == 0) {
                    tempGameObject.transform.position = new Vector3(x * hexTileOffset_X, y * hexTileOffset_Y, 0);
                } else {
                    tempGameObject.transform.position = new Vector3(x * hexTileOffset_X + hexTileOffset_X / 2.0f, y * hexTileOffset_Y, 0);
                }
            }
        }
    }

    // TODO: this should take color enum (and possibly material map) and map it to a material
    UnityMapHex placeNewHex(UnityMapHex referenceHex, HexEdgeEnum edge, Material material) {
        UnityMapHex newHex = new UnityMapHex(Instantiate(HexTilePrefab));
        newHex.gameObject.transform.position = referenceHex.calculateAdajcentPostition(edge);
        newHex.changeMeshMaterial(material);
        return newHex;
    }

    UnityMapHex placeNewHex(HexPositionPair referenceHexPair, HexMapPosition indexPosition, Material material) {
        UnityMapHex newHex = new UnityMapHex(Instantiate(HexTilePrefab));
        newHex.gameObject.transform.position = UnityMapHex.calculateTilePosition(referenceHexPair, indexPosition);
        newHex.changeMeshMaterial(material);
        return newHex;
    }

    private AnimatableMapHex animateHex(UnityMapHex hex) {
        Vector3 startPos = hex.gameObject.transform.position;
        hex.gameObject.transform.position = new Vector3(startPos.x, startPos.y, startPos.z + startZOffset);
        AnimatableMapHex animatableHex = new AnimatableMapHex(hex, timeTransformer, cycleTiming);
        animatableHex.register(StaticAnimationManager.instance);
        return animatableHex;
    }

    class HexMapPlacementData {
        public HexMapPlacementData(UnityMapHex[,] positionGrid, HexTileRegionCatalog hexTileRegionGroup) {
            this.positionGrid = positionGrid;
            this.hexTileRegionGroup = hexTileRegionGroup;
        }

        public UnityMapHex[,] positionGrid { get; private set; }
        public HexTileRegionCatalog hexTileRegionGroup { get; private set; }
    }

    abstract class BaseEnqueuedPlacementTile {

        public BaseEnqueuedPlacementTile(HexPositionPair referenceHexPositionPair) {
            this.referenceHexPositionPair = referenceHexPositionPair;
        }

        abstract public void placeAndAnimateHex(UnityMapHex[,] positionGrid);

        public virtual bool wasEnqueued { get; protected set; }
        public virtual bool isDone { get { return this.hex != null && this.hex.isDone(); } }

        abstract public HexMapPosition getPosition();

        public UnityMapHex getHex() {
            return hex.getHex();
        }

        protected HexPositionPair referenceHexPositionPair;
        protected AnimatableMapHex hex;
    }

    class EnqueuedIndexedPlacementTile : BaseEnqueuedPlacementTile {
        public EnqueuedIndexedPlacementTile(HexPositionPair referenceHexPosition, HexMapPosition indexPosition
            ) : base(referenceHexPosition) {

            this.indexPosition = indexPosition;
        }

        public override HexMapPosition getPosition() {
            // lazy substitute for a read-only/frozen version
            return new HexMapPosition(indexPosition);
        }

        public override void placeAndAnimateHex(UnityMapHex[,] positionGrid) {
            if (hex == null) {
                HexTileColor color =
                  (_generatorInstance.randomizeColors
                    ? HexTileColorExtensions.getRandomColor() : HexTileColor.BLUE);

                UnityMapHex unityMapHex = _generatorInstance.placeNewHex(referenceHexPositionPair, getPosition(),
                    getMaterial(color));

                // DEBUG
                labelHexes.Add(unityMapHex);

                hex = _generatorInstance.animateHex(unityMapHex);
                wasEnqueued = true;

                /*
                // REGION LOGIC TBD
                */
            }

        }

        private HexMapPosition indexPosition;
    }

    class EnqueuedRelativePlacementTile : BaseEnqueuedPlacementTile {
        public EnqueuedRelativePlacementTile(HexPositionPair referenceHexPositionPair, HexEdgeEnum placement)
            : base(referenceHexPositionPair) {
            this.placement = placement;
        }

        public override void placeAndAnimateHex(UnityMapHex[,] positionGrid) {
            UnityMapHex referenceHex = referenceHexPositionPair.hex;

            if (hex == null) {
                HexTileColor color =
                  (_generatorInstance.randomizeColors
                    ? HexTileColorExtensions.getRandomColor() : HexTileColor.BLUE);

                UnityMapHex unityMapHex = _generatorInstance.placeNewHex(referenceHex, placement,
                    getMaterial(color));

                // DEBUG
                labelHexes.Add(unityMapHex);

                hex = _generatorInstance.animateHex(unityMapHex);
                wasEnqueued = true;

                HexMapPosition position = UnityMapHex.getAdjacentIndex(referenceHexPositionPair.position, placement);

                hexTileRegionGroup.addHex(referenceHex, hex.getHex(), referenceHexPositionPair.position, placement, color);

                // this is done after hexTileRegionGroup.addHex since it's needed to assign the region
                if (_generatorInstance.randomizeRegionTileColors) {
                    HexTileRegion region = hexTileRegionGroup.getRegionContainingHex(unityMapHex);
                    assignHexMaterialFromRegion(region, unityMapHex);
                }

                positionGrid[position.x, position.y] = unityMapHex;
            }
        }

        public override HexMapPosition getPosition() {
            return UnityMapHex.getAdjacentIndex(referenceHexPositionPair.position, placement);
        }

        public HexEdgeEnum placement { get; private set; }
    }

    class EnqueuedPlacementTileAnchor : EnqueuedRelativePlacementTile {
        public EnqueuedPlacementTileAnchor(HexPositionPair referenceHexPosition)
            : base(referenceHexPosition, HexEdgeEnum.RIGHT) {
        }
        // public override bool wasEnqueued { get { return true; } }
        public override bool isDone { get { return true; } }
    }

    class EnqueuedPlacementTilePlaceholder : EnqueuedRelativePlacementTile {
        public EnqueuedPlacementTilePlaceholder(HexMapPosition hexMapPosition, HexEdgeEnum placement)
            : base(new HexPositionPair(null, hexMapPosition), placement) { }

        public HexPositionPair getReferenceHexPositionPair() {
            return this.referenceHexPositionPair;
        }
    }

    private UnityMapHex selectedHexTile;
    bool showHexSelector = false;
    private HexEdgeEnum defaultAboveSelection = HexEdgeEnum.TOP_LEFT;
    private HexEdgeEnum defaultBelowSelection = HexEdgeEnum.BOTTOM_RIGHT;

    private void swapAboveBelowSelectionDefaults() {
        defaultAboveSelection = (defaultAboveSelection == HexEdgeEnum.TOP_LEFT)
            ? HexEdgeEnum.TOP_RIGHT : HexEdgeEnum.TOP_LEFT;
        defaultBelowSelection = (defaultBelowSelection == HexEdgeEnum.BOTTOM_LEFT)
            ? HexEdgeEnum.BOTTOM_RIGHT : HexEdgeEnum.BOTTOM_LEFT;
    }

    public void Update() {
        if (readyToGenerateMap) {
            if (enqueuedTiles.Count > 0) {
                //string positions = String.Join(", ", enqueuedTiles.Select(tile =>
                //    $"{{{tile.getPosition().x}, {tile.getPosition().y}}}"
                //));
                // Debug.Log($"Placements: {positions}");

                EnqueuedRelativePlacementTile nextTile = enqueuedTiles[0];

                if (!nextTile.wasEnqueued) {
                    if (nextTile is EnqueuedPlacementTilePlaceholder) {
                        HexMapPosition referencePosition =
                            ((EnqueuedPlacementTilePlaceholder)nextTile).getReferenceHexPositionPair().position;

                        UnityMapHex referenceHex = hexPositionGrid[referencePosition.x, referencePosition.y];
                        nextTile = new EnqueuedRelativePlacementTile(
                            new HexPositionPair(referenceHex, referencePosition),
                            nextTile.placement
                        );
                        enqueuedTiles.RemoveAt(0);
                        enqueuedTiles.Insert(0, nextTile);
                    }
                    nextTile.placeAndAnimateHex(hexPositionGrid);
                } else if (nextTile.isDone) {
                    enqueuedTiles.Remove(nextTile);
                }
            }

            // (SLOPPY) INPUT HANDLING
            if (Input.GetKeyDown(KeyCode.G)) {
                Debug.Log("Pressed G");
                showHexGuiDebugText = !showHexGuiDebugText;
            }


            if (Input.GetKeyDown(KeyCode.Space)) {
                showHexSelector = !showHexSelector;
                Outline outline = getOutline(selectedHexTile);
                if (outline != null) {
                    outline.enabled = showHexSelector;
                }
            }

            if (Input.GetKeyDown(KeyCode.I)) {
                Debug.Log("Pressed I");
                if (showHexSelector) {
                    MapHex newSelection = selectedHexTile.getAdjacentHex(defaultAboveSelection);
                    if (newSelection == null) {
                        newSelection = selectedHexTile.getAdjacentHex((defaultAboveSelection == HexEdgeEnum.TOP_LEFT)
                            ? HexEdgeEnum.TOP_RIGHT : HexEdgeEnum.TOP_LEFT);
                    } else {
                        // TODO: always do this if that's better
                        swapAboveBelowSelectionDefaults();
                    }
                    if (newSelection != null) {
                        if (!(newSelection is UnityMapHex)) {
                            Debug.Log($"Selection is of unexpected type {newSelection.GetType()}!");
                        } else {
                            updateSelectedHex((UnityMapHex)newSelection);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.M)) {
                Debug.Log("Pressed M");
                if (showHexSelector) {
                    MapHex newSelection = selectedHexTile.getAdjacentHex(defaultBelowSelection);
                    if (newSelection == null) {
                        newSelection = selectedHexTile.getAdjacentHex((defaultBelowSelection == HexEdgeEnum.BOTTOM_LEFT)
                            ? HexEdgeEnum.BOTTOM_RIGHT : HexEdgeEnum.BOTTOM_LEFT);
                    } else {
                        // TODO: always do this if that's better
                        swapAboveBelowSelectionDefaults();
                    }
                    if (newSelection != null) {
                        if (!(newSelection is UnityMapHex)) {
                            Debug.Log($"Selection is of unexpected type {newSelection.GetType()}!");
                        } else {
                            updateSelectedHex((UnityMapHex)newSelection);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.J)) {
                Debug.Log("Pressed J");
                if (showHexSelector) {
                    MapHex newSelection = selectedHexTile.getAdjacentHex(HexEdgeEnum.LEFT);
                    if (newSelection != null) {
                        if (!(newSelection is UnityMapHex)) {
                            Debug.Log($"Selection is of unexpected type {newSelection.GetType()}!");
                        } else {
                            updateSelectedHex((UnityMapHex)newSelection);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.K)) {
                Debug.Log("Pressed K");
                if (showHexSelector) {
                    MapHex newSelection = selectedHexTile.getAdjacentHex(HexEdgeEnum.RIGHT);
                    if (newSelection != null) {
                        if (!(newSelection is UnityMapHex)) {
                            Debug.Log($"Selection is of unexpected type {newSelection.GetType()}!");
                        } else {
                            updateSelectedHex((UnityMapHex)newSelection);
                        }
                    }
                }
            }
        }
    }

    private Outline getOutline(UnityMapHex hex) {
        return hex != null ? hex.gameObject.GetComponent<Outline>() : null;
    }

    private void updateSelectedHex(UnityMapHex hex) {
        if (selectedHexTile != null) {
            getOutline(selectedHexTile).enabled = false;
        }
        addOutlineScriptIfNeeded(hex);
        getOutline(hex).enabled = showHexSelector;
        selectedHexTile = hex;
    }

    private void addOutlineScriptIfNeeded(UnityMapHex hex) {
        Outline outline = hex.gameObject.GetComponent<Outline>();
        if (outline == null) {
            outline = hex.gameObject.AddComponent<Outline>();
        }
        outline.enabled = showHexSelector;
    }


    static HexMapPosition getCenterHexPosition(int mapWidth, int mapHeight) {
        return new HexMapPosition(mapWidth / 2, mapHeight / 2);
    }

    // private const int NUM_HEXES = 1 + 6 + 12 + 18;
    private int calculateNumHexes() {
        return 1 + (numHexRingLevels * (numHexRingLevels + 1) / 2) * 6;
    }

    private void generatePlacementPlan(UnityMapHex startHex, HexTileColor startingColor) {
        int numHexes = calculateNumHexes();

        HexMapPosition startingPosition = getCenterHexPosition(mapMaxDimensions.Item1, mapMaxDimensions.Item2);
        hexPositionGrid[startingPosition.x, startingPosition.y] = startHex;


        startHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getMaterial(startingColor);

        List<HexEdgeEnum> placements = new List<HexEdgeEnum> {
            HexEdgeEnum.RIGHT,
            HexEdgeEnum.BOTTOM_RIGHT,
            HexEdgeEnum.BOTTOM_LEFT,
            HexEdgeEnum.LEFT,
            HexEdgeEnum.TOP_LEFT,
            HexEdgeEnum.TOP_RIGHT,
        };

        setOrderedEdges(placements);

        placementGrid = new EnqueuedRelativePlacementTile[mapMaxDimensions.Item1, mapMaxDimensions.Item2];
        placementGrid[startingPosition.x, startingPosition.y] =
            new EnqueuedPlacementTileAnchor(new HexPositionPair(startHex, startingPosition));

        switch (tilePlacementMode) {
            case TilePlacementMode.RANDOM_CONNECTED:
                generateRandomConnectedPlacement(startHex, startingPosition, numHexes);
                break;
            case TilePlacementMode.FILLED:
            default:
                generateFilledAreaPlacement(startHex, startingPosition, numHexes);
                break;
        }
    }

    void generateFilledAreaPlacement(UnityMapHex startHex, HexMapPosition startingPosition, int numHexes) {
        int hexCount = 1;
        HexPositionPair startHexPositionPair = new HexPositionPair(startHex, startingPosition);

        // subsequent hexes will be placed relative to these
        List<EnqueuedRelativePlacementTile> nextHexes = new List<EnqueuedRelativePlacementTile>();

        List<HexEdgeEnum> edges = getOrderedEdges();
        foreach (HexEdgeEnum placement in edges) {
            EnqueuedRelativePlacementTile placementTile = new EnqueuedRelativePlacementTile(startHexPositionPair, placement);
            enqueuedTiles.Add(placementTile);
            nextHexes.Add(placementTile);

            // precalculate placement positions to simplify subsequent placements
            HexMapPosition placementPosition = MapHex.getAdjacentIndex(startingPosition, placement);
            placementGrid[placementPosition.x, placementPosition.y] = placementTile;
            hexCount++;
        }
        while (hexCount < numHexes && nextHexes.Count > 0) {
            EnqueuedRelativePlacementTile newReferenceHex = nextHexes[0];
            nextHexes.RemoveAt(0);
            rotateOrderedEdges();
            bool skippedAHexClockwise = false;
            List<EnqueuedRelativePlacementTile> tilesToEnqueueFromFront = new List<EnqueuedRelativePlacementTile>();
            List<EnqueuedRelativePlacementTile> tilesToEnqueueFromEnd = new List<EnqueuedRelativePlacementTile>();

            foreach (HexEdgeEnum placement in getOrderedEdges()) {
                HexMapPosition newPosition = UnityMapHex.getAdjacentIndex(newReferenceHex.getPosition(), placement);
                if (hexCount < numHexes && placementGrid[newPosition.x, newPosition.y] == null) {
                    EnqueuedRelativePlacementTile placementTile = new EnqueuedPlacementTilePlaceholder(
                        newReferenceHex.getPosition(),
                        placement);

                    if (!skippedAHexClockwise) {
                        tilesToEnqueueFromFront.Add(placementTile);
                    } else {
                        tilesToEnqueueFromEnd.Add(placementTile);
                    }

                    // precalculate placement positions to simplify subsequent placements
                    placementGrid[newPosition.x, newPosition.y] = placementTile;
                    hexCount++;
                } else {
                    // Debug.Log($"Hexmap tile already exists at {newPosition.x},{newPosition.y}");
                    skippedAHexClockwise = true;
                }
            }
            tilesToEnqueueFromEnd.AddRange(tilesToEnqueueFromFront);
            tilesToEnqueueFromEnd.ForEach(tile => {
                enqueuedTiles.Add(tile);
                nextHexes.Add(tile);
            });
        }
    }

    private enum HostSelectionMode {
        PURELY_RANDOM,
        WEIGHT_BY_EDGES,
    }
    // for v3 mode; can't define an inline class! so here we are
    class CandidatePosition {
        public CandidatePosition(HexMapPosition position, float probability) {
            this.position = position;
            this.probability = probability;
        }
        public HexMapPosition position { get; private set; }
        public float probability { get; private set; }
    }
    private HostSelectionMode hostSelectionMode = HostSelectionMode.WEIGHT_BY_EDGES;

    private HexMapPosition selectHostPosition(List<HexMapPosition> unsurroundedHexes, MapHex[,] claimedPositions) {
        switch (hostSelectionMode) {
            case HostSelectionMode.WEIGHT_BY_EDGES:
                Dictionary<int, List<HexMapPosition>> edgeCounts = new Dictionary<int, List<HexMapPosition>>();
                for (int i = 1; i <= 6; i++) {
                    edgeCounts[i] = new List<HexMapPosition>();
                }
                unsurroundedHexes.ForEach(position => {
                    List<HexEdgeEnum> freeHexes = MapHex.getFreeEdges(position, claimedPositions);
                    if (freeHexes.Count > 0) {
                        edgeCounts[freeHexes.Count].Add(position);
                    }
                });

                float edgeCountWeight = freeEdgeWeight;
                float[] probabilities = new float[6];
                for (int i = 0; i < 6; i++) {
                    probabilities[i] = (float)Math.Pow(i, edgeCountWeight);
                }

                List<CandidatePosition> candidatePositions = new List<CandidatePosition>();
                float totalProbabilityRange = 0.0f;
                edgeCounts.Keys.ToList()
                    .ForEach(count => {
                        edgeCounts[count].ForEach(position => {
                            candidatePositions.Add(new CandidatePosition(position, probabilities[count - 1] + totalProbabilityRange));
                            totalProbabilityRange += probabilities[count - 1];
                        });
                    });

                float dieRoll = UnityEngine.Random.Range(0, totalProbabilityRange);
      
                for (int i = 0; i < candidatePositions.Count; i++) {
                   if (candidatePositions[i].probability > dieRoll) {
                        return candidatePositions[i].position;
                    }
                }
                return candidatePositions.Last().position; // this shouldn't happen

            case HostSelectionMode.PURELY_RANDOM:
                default:
                    return unsurroundedHexes[UnityEngine.Random.Range(0, unsurroundedHexes.Count)];
        }
    }

    private HexEdgeEnum selectPlacementEdge(HexMapPosition hostPosition, MapHex[,] claimedPositions) {
        List<HexEdgeEnum> freeEdges = MapHex.getFreeEdges(hostPosition, claimedPositions);
        HexEdgeEnum freeEdge = freeEdges[UnityEngine.Random.Range(0, freeEdges.Count)];
        return freeEdge;
    }

    void generateRandomConnectedPlacement(UnityMapHex startHex, HexMapPosition startingPosition, int numHexes) {
        int hexCount = 1;
        HexPositionPair startHexPositionPair = new HexPositionPair(startHex, startingPosition);

        // subsequent hexes will be placed relative to these
        List<HexMapPosition> unsurroundedHexes = new List<HexMapPosition>();
        unsurroundedHexes.Add(startingPosition);

        MapHex[,] claimedPositions = new MapHex[placementGrid.GetLength(0), placementGrid.GetLength(1)];
        claimedPositions[startingPosition.x, startingPosition.y] = startHex;

        MapHex placeholderHex = startHex;  // THIS IS HACKY, SHOULD JUST NEED ANY HEX HERE
        while (unsurroundedHexes.Count > 0 && hexCount < numHexes) {
            // identify neighbor
            HexMapPosition hostHexPosition = selectHostPosition(unsurroundedHexes, claimedPositions);
            // find a free edge on neighbor
            HexEdgeEnum freeEdge = selectPlacementEdge(hostHexPosition, claimedPositions);
            // enqueue placement next to neighbor
           
            EnqueuedRelativePlacementTile placementTile = new EnqueuedPlacementTilePlaceholder(hostHexPosition, freeEdge);
            HexMapPosition newPosition = MapHex.getAdjacentIndex(hostHexPosition, freeEdge);
            placementGrid[newPosition.x, newPosition.y] = placementTile;
            claimedPositions[newPosition.x, newPosition.y] = placeholderHex;
            enqueuedTiles.Add(placementTile);

            // check neighbors for surrounded (including map edges); if so, remove from unsurrounded hexes
            MapHex.getAdjacentHexes(newPosition, claimedPositions)
                .Where(entry => entry.Value != null)
                .Where(entry => {
                    // find surrounded neighbors
                    HexMapPosition adjacentPosition = MapHex.getAdjacentIndex(newPosition, entry.Key);
                    return MapHex.getFreeEdges(adjacentPosition, claimedPositions).Count == 0;
                })
                .ToList()
                .ForEach(entry => {
                    // remove from unsurrounded hexes
                    HexMapPosition adjacentPosition = MapHex.getAdjacentIndex(newPosition, entry.Key);
                    unsurroundedHexes.RemoveAll(position => position.Equals(adjacentPosition));
                });

            if (MapHex.getFreeEdges(newPosition, claimedPositions).Count > 0) {
                unsurroundedHexes.Add(newPosition);
            }
            // check self for surrounded; if not, add to unsurrounded hexes
            hexCount++;
        }
    }

    void rotateOrderedEdges() {
        HexEdgeEnum newLastEdge = _orderedEdges[0];
        _orderedEdges.RemoveAt(0);
        _orderedEdges.Add(newLastEdge);
    }

    void setOrderedEdges(List<HexEdgeEnum> edges) {
        if (_orderedEdges == null) {
            _orderedEdges = new List<HexEdgeEnum>();
        } else {
            _orderedEdges.Clear();
        }
        _orderedEdges.AddRange(edges);
    }

    List<HexEdgeEnum> getOrderedEdges() {
        return _orderedEdges;
    }

    private AnimatableMapHex.AnimatableHexCycleTiming cycleTiming;

    void GenerateHexMapExperimental(int numLevels) {
        initalizeAnimationTiming(numLevels);
        intializeReferenceHexes();

        switch (tilePlacementMode) {
            case TilePlacementMode.RANDOM_CONNECTED:
                int extraCells = bufferCellsForRandMode;
                int cellsPerAxis = 2 * (numLevels + extraCells + 1) + 1;
                mapMaxDimensions = new Tuple<int, int>(cellsPerAxis, cellsPerAxis);
                break;
            case TilePlacementMode.FILLED:
            default:
                mapMaxDimensions = new Tuple<int, int>(2 * (numLevels + 1) + 1, 2 * (numLevels + 1) + 1);
                break;
        }

        hexPositionGrid = new UnityMapHex[mapMaxDimensions.Item1, mapMaxDimensions.Item2];

        UnityMapHex startHex = new UnityMapHex(Instantiate(HexTilePrefab));
        HexTileColor startColor = randomizeColors ? HexTileColorExtensions.getRandomColor() : HexTileColor.BLUE;

        hexTileRegionGroup = new HexTileRegionCatalog(hexPositionGrid, startHex, startColor);

        generatePlacementPlan(startHex, startColor);

        // TODO: randomizePlacement doesn't work because calculateAdjacentPosition relies on a reference tile.
        //       fix this or replace with an equivalent method that calculates position based on placement coordinates
        //       and an origin tile.
        if (randomizePlacement) {
            List<HexMapPosition> debugPositions = enqueuedTiles.Select(tile => tile.getPosition()).ToList();
            String debugPositionsBeforeString = String.Join(", ", debugPositions);
            enqueuedTiles = enqueuedTiles
                .OrderBy(tile => UnityEngine.Random.value)
                .ToList();
            debugPositions = enqueuedTiles.Select(tile => tile.getPosition()).ToList();
            String debugPositionsAfterString = String.Join(", ", debugPositions);
            Debug.Log("Positions before after");
        }

        updateSelectedHex(startHex);
        readyToGenerateMap = true;      
    }

    private void initalizeAnimationTiming(int numLevels) {
        float timeToMaxSpeed = Math.Min(
           Math.Max(
               numLevels * 1.0f, // calculateNumHexes() * 1.0f,
               AnimatableMapHex.AnimatableHexCycleTiming.DEFAULT_MIN_TIME_TO_MAX_SPEED
           ),
           AnimatableMapHex.AnimatableHexCycleTiming.DEFAULT_MAX_TIME_TO_MAX_SPEED
       );
        cycleTiming = new AnimatableMapHex.AnimatableHexCycleTiming(
            timeToMaxSpeed,
            HEX_FALL_DURATION_INITIAL_TIME,
            HEX_FALL_DURATION_FINAL_TIME);
    }


    HexPositionPair originHexPair; // TODO: encapsulate this into the placement plan instead of using a local variable

    // TODO: generate more than one level (dumb version: iterate over all hexes except outer ring and dedupe)
    List<HexMapPosition> generateFilledPlacementPlan(HexMapPosition center, int numLevels) {
        List<HexMapPosition> positions = new List<HexMapPosition>();

        HexEdgeEnumExtensions.getEnumValues().ForEach(edge => {
            positions.Add(UnityMapHex.getAdjacentIndex(center, edge));
        });

        return positions;
    }

    void GenerateHexMapExperimentalNew(int numLevels) {
        initalizeAnimationTiming(numLevels);
        intializeReferenceHexes();

        mapMaxDimensions = new Tuple<int, int>(2 * numLevels + 1, 2 * numLevels + 1);

        UnityMapHex startHex = new UnityMapHex(Instantiate(HexTilePrefab));
        HexTileColor startColor = randomizeColors ? HexTileColorExtensions.getRandomColor() : HexTileColor.BLUE;
        originHexPair = new HexPositionPair(startHex, new HexMapPosition(numLevels, numLevels));

        hexPositionGrid = new UnityMapHex[mapMaxDimensions.Item1, mapMaxDimensions.Item2];
        hexTileRegionGroup = new HexTileRegionCatalog(hexPositionGrid, startHex, startColor);

        // enqueuedTiles.AddRange(generateFilledPlacementPlan(originHexPair.position, numLevels));

        readyToGenerateMap = true;
    }
}