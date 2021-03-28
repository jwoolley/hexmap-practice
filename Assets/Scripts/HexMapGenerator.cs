using UnityEngine;

public class HexMapGenerator : MonoBehaviour {
    [SerializeField]
    GameObject HexTilePrefab;

    [SerializeField]
    int mapHexWidth = 1;

    [SerializeField]
    int mapHexHeight = 4;

    readonly float hexTileOffset_X = 1.76f;
    readonly float hexTileOffset_Y = 1.52f;

    void Start() {
        GenerateHexMap();
    }

    void GenerateHexMap() {
        float initialOffsetX = (1 - mapHexWidth) / 2.0f;
        float initialOffsetY = (1 - mapHexHeight) / 2.0f;

        for (int i = 0; i < mapHexHeight; i++) {
            float y = i + initialOffsetY;
            for (int j = 0; j < mapHexWidth; j++) {
                float x = j + initialOffsetX;
                GameObject tempGameObject = Instantiate(HexTilePrefab);

                if (i % 2 == 0) {
                    tempGameObject.transform.position = new Vector3(x * hexTileOffset_X, y * hexTileOffset_Y, 0);
                } else {
                    tempGameObject.transform.position = new Vector3(x * hexTileOffset_X + hexTileOffset_X / 2.0f, y * hexTileOffset_Y, 0);
                }
            }
        }
    }
}