using System;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {
    [SerializeField]
    GameObject HexTilePrefab;

    [SerializeField]
    int mapHexWidth;

    [SerializeField]
    int mapHexHeight;

    [SerializeField]
    bool randomizeColors;

    [SerializeField]
    bool useExperimentalPlacement;

    readonly float hexTileOffset_X = 1.76f;
    readonly float hexTileOffset_Y = 1.52f;

    void Start() {
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
            GenerateHexMapExperimental();
        } else {
            GenerateHexMapSimple();
        }
    }

    void GenerateHexMapSimple() {
        float initialOffsetX = (1 - mapHexWidth) / 2.0f;
        float initialOffsetY = (1 - mapHexHeight) / 2.0f;

        intializeReferenceHexMap();

        for (int i = 0; i < mapHexHeight; i++) {
            float y = i + initialOffsetY;
            for (int j = 0; j < mapHexWidth; j++) {
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

    readonly TimeTransformer timeTransformer = new TimeTransformerBase();
    float startZOffset = 2.0f;
    private void animateHex(UnityMapHex hex) {
        Vector3 startPos = hex.gameObject.transform.position;
        hex.gameObject.transform.position = new Vector3(startPos.x, startPos.y, startPos.z + startZOffset);
        AnimatableMapHex animatableHex = new AnimatableMapHex(hex, timeTransformer);
        animatableHex.register(StaticAnimationManager.instance);
    }

    void GenerateHexMapExperimental() {
        intializeReferenceHexMap();

        UnityMapHex startHex = new UnityMapHex(Instantiate(HexTilePrefab));

        if (randomizeColors) {
            startHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        List<HexEdgeEnum> placements = new List<HexEdgeEnum>{
            HexEdgeEnum.RIGHT,
            HexEdgeEnum.BOTTOM_RIGHT,
            HexEdgeEnum.BOTTOM_LEFT,
            HexEdgeEnum.LEFT,
            HexEdgeEnum.TOP_LEFT,
            HexEdgeEnum.TOP_RIGHT,
        };

        foreach (HexEdgeEnum placement in placements) {
            UnityMapHex hex = placeNewHex(startHex, placement, randomizeColors
                ? getRandomHexColorMaterial()
                : startHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial);

            animateHex(hex);
        }

        // TODO: set edges on these (and any new edges that have been "met" upon placement ...?)

        /*
        UnityMapHex nextHex = new UnityMapHex(Instantiate(HexTilePrefab));
        HexEdgeEnum edge = HexEdgeEnum.LEFT;
        nextHex.gameObject.transform.position = startHex.calculateAdajcentPostition(edge);
        if (randomizeColors) {
            nextHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        nextHex = new UnityMapHex(Instantiate(HexTilePrefab));
        edge = HexEdgeEnum.RIGHT;
        nextHex.gameObject.transform.position = startHex.calculateAdajcentPostition(edge);
        if (randomizeColors) {
            nextHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        nextHex = new UnityMapHex(Instantiate(HexTilePrefab));
        edge = HexEdgeEnum.TOP_LEFT;
        nextHex.gameObject.transform.position = startHex.calculateAdajcentPostition(edge);
        if (randomizeColors) {
            nextHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        nextHex = new UnityMapHex(Instantiate(HexTilePrefab));
        edge = HexEdgeEnum.TOP_RIGHT;
        nextHex.gameObject.transform.position = startHex.calculateAdajcentPostition(edge);
        if (randomizeColors) {
            nextHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        nextHex = new UnityMapHex(Instantiate(HexTilePrefab));
        edge = HexEdgeEnum.BOTTOM_LEFT;
        nextHex.gameObject.transform.position = startHex.calculateAdajcentPostition(edge);
        if (randomizeColors) {
            nextHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }

        nextHex = new UnityMapHex(Instantiate(HexTilePrefab));
        edge = HexEdgeEnum.BOTTOM_RIGHT;
        nextHex.gameObject.transform.position = startHex.calculateAdajcentPostition(edge);
        if (randomizeColors) {
            nextHex.gameObject.GetComponent<MeshRenderer>().sharedMaterial = getRandomHexColorMaterial();
        }
        */
    }
}