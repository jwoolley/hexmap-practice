using System;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {
    [SerializeField]
    GameObject HexTilePrefab;

    [SerializeField]
    [Range(1, 12)]
    int numHexRingLevels;

    [SerializeField]
    bool randomizeColors;

    [SerializeField]
    bool useExperimentalPlacement;

    readonly float hexTileOffset_X = 1.76f;
    readonly float hexTileOffset_Y = 1.52f;
    float startZOffset = -8.0f;

    private Tuple<int, int> mapMaxDimensions;

    List<HexEdgeEnum> _orderedEdges;
    private UnityMapHex[,] hexPositionGrid;
    private EnqueuedPlacementTile[,] placementGrid;
    List<EnqueuedPlacementTile> enqueuedTiles = new List<EnqueuedPlacementTile>();

    readonly TimeTransformer timeTransformer = new TimeTransformerSmoothStop4();
    private bool readyToGenerateMap = false;

    static HexMapGenerator _generatorInstance;

    void Start() {
        _generatorInstance = this;
        GenerateHexMap();
    }

    enum TileColor {
        RED,
        BLUE,
        GREEN,
        PURPLE,
        ORANGE,
        YELLOW
    };

    Material getNewMaterialWithColor(Color color) {
        MeshRenderer meshRenderer = HexTilePrefab.GetComponent<MeshRenderer>();
        Material material = Instantiate(meshRenderer.sharedMaterial);
        material.SetColor("_Color", color);
        return material;
    }

    Material getRandomHexColorMaterial() {
        TileColor tileColorEnum = (TileColor)UnityEngine.Random.Range(0, Enum.GetNames(typeof(TileColor)).Length);
        return referenceHexes[tileColorEnum];
    }

    Dictionary<TileColor, Material> referenceHexes;

    void intializeReferenceHexMap() {
        referenceHexes = new Dictionary<TileColor, Material>();
        referenceHexes.Add(TileColor.RED, getNewMaterialWithColor(Color.red));
        referenceHexes.Add(TileColor.BLUE, getNewMaterialWithColor(Color.blue));
        referenceHexes.Add(TileColor.GREEN, getNewMaterialWithColor(Color.green));
        referenceHexes.Add(TileColor.PURPLE, getNewMaterialWithColor(new Color(0.6f, 0.0f, 1.0f, 1.0f)));
        referenceHexes.Add(TileColor.ORANGE, getNewMaterialWithColor(new Color(1.0f, 0.5f, 0.0f, 1.0f)));
        referenceHexes.Add(TileColor.YELLOW, getNewMaterialWithColor(Color.yellow));
    }

    void GenerateHexMap() {
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

        intializeReferenceHexMap();

        for (int i = 0; i < MAP_HEX_HEIGHT_SIMPLE; i++) {
            float y = i + initialOffsetY;
            for (int j = 0; j < MAP_HEX_WIDTH_SIMPLE; j++) {
                float x = j + initialOffsetX;
                GameObject tempGameObject = Instantiate(HexTilePrefab);

                if (randomizeColors) {
                    tempGameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
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
        newHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        return newHex;
    }

    private AnimatableMapHex animateHex(UnityMapHex hex) {
        Vector3 startPos = hex.gameObject.transform.position;
        hex.gameObject.transform.position = new Vector3(startPos.x, startPos.y, startPos.z + startZOffset);
        AnimatableMapHex animatableHex = new AnimatableMapHex(hex, timeTransformer);
        animatableHex.register(StaticAnimationManager.instance);
        return animatableHex;
    }

    class EnqueuedPlacementTile {
        public EnqueuedPlacementTile(HexPositionPair referenceHexPosition, HexEdgeEnum placement) {
            this.referenceHexPosition = referenceHexPosition;
            this.placement = placement;
        }

        public void placeAndAnimateHex(UnityMapHex[,] positionGrid) {
            UnityMapHex referenceHex = referenceHexPosition.hex;

            if (hex == null) {
                UnityMapHex unityMapHex = _generatorInstance.placeNewHex(referenceHex, placement,
                    _generatorInstance.randomizeColors
                    ? _generatorInstance.getRandomHexColorMaterial()
                    : referenceHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial);

                hex = _generatorInstance.animateHex(unityMapHex);
                wasEnqueued = true;

                HexMapPosition position = UnityMapHex.getAdjacentIndex(referenceHexPosition.position, placement);
                positionGrid[position.x, position.y] = unityMapHex;
            }
        }

        public virtual bool wasEnqueued { get; private set; }
        public virtual bool isDone { get { return this.hex != null && this.hex.isDone(); } }

        public HexMapPosition getPosition() {
            return UnityMapHex.getAdjacentIndex(referenceHexPosition.position, placement);
        }

        
        public HexEdgeEnum placement { get; private set; }
        protected HexPositionPair referenceHexPosition;
        private AnimatableMapHex hex;
    }

    class EnqueuedPlacementTileAnchor : EnqueuedPlacementTile {
        public EnqueuedPlacementTileAnchor(HexPositionPair referenceHexPosition)
            : base(referenceHexPosition, HexEdgeEnum.RIGHT) {
        }
        // public override bool wasEnqueued { get { return true; } }
        public override bool isDone { get { return true; } }
    }

    class EnqueuedPlacementTilePlaceholder : EnqueuedPlacementTile {
        public EnqueuedPlacementTilePlaceholder(HexMapPosition hexMapPosition, HexEdgeEnum placement)
            : base(new HexPositionPair(null, hexMapPosition), placement) { }

        public HexPositionPair getReferenceHexPositionPair() {
            return this.referenceHexPosition;
        }
    }

    public void Update() {
        if (readyToGenerateMap && enqueuedTiles.Count > 0) {
            //string positions = String.Join(", ", enqueuedTiles.Select(tile =>
            //    $"{{{tile.getPosition().x}, {tile.getPosition().y}}}"
            //));
            // Debug.Log($"Placements: {positions}");

            EnqueuedPlacementTile nextTile = enqueuedTiles[0];

            if (!nextTile.wasEnqueued) {
                if (nextTile is EnqueuedPlacementTilePlaceholder) {
                    HexMapPosition referencePosition =
                        ((EnqueuedPlacementTilePlaceholder)nextTile).getReferenceHexPositionPair().position;

                    UnityMapHex referenceHex = hexPositionGrid[referencePosition.x, referencePosition.y];
                    HexMapPosition position = nextTile.getPosition();
                    nextTile = new EnqueuedPlacementTile(
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
    }

    static HexMapPosition getCenterHexPosition(int mapWidth, int mapHeight) {
        return new HexMapPosition(mapWidth / 2, mapHeight / 2);
    }

    class HexPositionPair {
        public HexPositionPair(UnityMapHex hex, HexMapPosition position) {
            this.hex = hex;
            this.position = position;
        }

        public void setHex(UnityMapHex hex) {
            this.hex = hex;
        }

        public HexPositionPair(HexPositionPair original) : this(original.hex, original.position) { }
        public UnityMapHex hex { get; private set; }
        public HexMapPosition position { get; private set; }
    }

    // private const int NUM_HEXES = 1 + 6 + 12 + 18;
    private int calculateNumHexes() {
        return 1 + (numHexRingLevels * (numHexRingLevels + 1) / 2) * 6;
    }

    private void generatePlacementPlan() {
        int numHexes = calculateNumHexes();
        UnityMapHex startHex = new UnityMapHex(Instantiate(HexTilePrefab));

        HexMapPosition startingPosition = getCenterHexPosition(mapMaxDimensions.Item1, mapMaxDimensions.Item2);
        hexPositionGrid[startingPosition.x, startingPosition.y] = startHex;

        int hexCount = 1;

        if (randomizeColors) {
            startHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        List<HexEdgeEnum> placements = new List<HexEdgeEnum> {
            HexEdgeEnum.RIGHT,
            HexEdgeEnum.BOTTOM_RIGHT,
            HexEdgeEnum.BOTTOM_LEFT,
            HexEdgeEnum.LEFT,
            HexEdgeEnum.TOP_LEFT,
            HexEdgeEnum.TOP_RIGHT,
        };

        setOrderedEdges(placements);

        placementGrid = new EnqueuedPlacementTile[mapMaxDimensions.Item1, mapMaxDimensions.Item2];
        placementGrid[startingPosition.x, startingPosition.y] =
            new EnqueuedPlacementTileAnchor(new HexPositionPair(startHex, startingPosition));

        HexPositionPair startHexPositionPair = new HexPositionPair(startHex, startingPosition);

        // subsequent hexes will be placed relative to these
        List<EnqueuedPlacementTile> nextHexes = new List<EnqueuedPlacementTile>();

        foreach (HexEdgeEnum placement in getOrderedEdges()) {
            EnqueuedPlacementTile placementTile = new EnqueuedPlacementTile(startHexPositionPair, placement);
            enqueuedTiles.Add(placementTile);
            nextHexes.Add(placementTile);

            // precalculate placement positions to simplify subsequent placements
            HexMapPosition placementPosition = UnityMapHex.getAdjacentIndex(startingPosition, placement);
            placementGrid[placementPosition.x, placementPosition.y] = placementTile;

            hexCount++;
        }

        while (hexCount < numHexes && nextHexes.Count > 0) {
            EnqueuedPlacementTile newReferenceHex = nextHexes[0];
            nextHexes.RemoveAt(0);
            rotateOrderedEdges();
            bool skippedAHexClockwise = false;
            List<EnqueuedPlacementTile> tilesToEnqueueFromFront = new List<EnqueuedPlacementTile>();
            List<EnqueuedPlacementTile> tilesToEnqueueFromEnd = new List<EnqueuedPlacementTile>();

            foreach (HexEdgeEnum placement in getOrderedEdges()) {
                HexMapPosition newPosition = UnityMapHex.getAdjacentIndex(newReferenceHex.getPosition(), placement);
                if (hexCount < numHexes && placementGrid[newPosition.x, newPosition.y] == null) {
                    EnqueuedPlacementTile placementTile = new EnqueuedPlacementTilePlaceholder(
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

    private Dictionary<HexEdgeEnum, UnityMapHex> getAdjacentHexes(HexMapPosition referencePosition, UnityMapHex[,] placements) {
        Dictionary<HexEdgeEnum, UnityMapHex> adjacentHexes = new Dictionary<HexEdgeEnum, UnityMapHex>();
        foreach (HexEdgeEnum edge in Enum.GetValues(typeof(HexEdgeEnum))) {
            HexMapPosition adjacentPosition = UnityMapHex.getAdjacentIndex(referencePosition, edge);
            adjacentHexes.Add(edge, placements[adjacentPosition.x, adjacentPosition.y]);
        }
        return adjacentHexes;
    }

    void GenerateHexMapExperimental(int numLevels) {

        intializeReferenceHexMap();

        mapMaxDimensions = new Tuple<int, int>(2 * numLevels + 1, 2 * numLevels + 1);

        hexPositionGrid = new UnityMapHex[mapMaxDimensions.Item1, mapMaxDimensions.Item2];

        generatePlacementPlan();

        AnimatableMapHex.calibrateCycleTiming(calculateNumHexes());

        readyToGenerateMap = true;      
    }
}