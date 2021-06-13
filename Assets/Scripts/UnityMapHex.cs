using UnityEngine;

public class UnityMapHex : MapHex {

    public UnityMapHex(GameObject gameObject) {
        this.gameObject = gameObject;
    }

    // TODO: move this math code to separate unity-level constants file (and possibly calculate these on startup)
    public static readonly float ROOT_3 = Mathf.Sqrt(3);
    public static readonly float ROOT_3_OVER_2 = Mathf.Sqrt(3) / 2.0f; // ratio of side length to (distance from center to edge)
    public static readonly float ROOT_3_OVER_4 = Mathf.Sqrt(3) / 4.0f; // above in terms of outer radius
    private readonly float hexTileOffset_X = 1.76f; // hex width (i.e. outer radius, i.e. twice side length)
    private readonly float hexTileOffset_Y = 1.52f;
    public Vector2 getHexTileOffset() {
        return new Vector2(hexTileOffset_X, hexTileOffset_Y);
    }
    public float getSideLength() {
        return getHexTileOffset().x / 2.0f;
    }

    public void changeMeshMaterial(Material material) {
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // used to determine the world-space position of a hex positioned next to this hex along the specified edge
    public Vector3 calculateAdajcentPostition(HexEdgeEnum edge) {
        Vector3 position = this.gameObject.transform.position;

        switch (edge) {
            case HexEdgeEnum.LEFT:
                return new Vector3(position.x - hexTileOffset_X, position.y, position.z);
            case HexEdgeEnum.RIGHT:
                return new Vector3(position.x + hexTileOffset_X, position.y, position.z);
            case HexEdgeEnum.TOP_LEFT:
                return new Vector3(position.x - getSideLength(), position.y + ROOT_3 * getSideLength(), position.z);
            case HexEdgeEnum.TOP_RIGHT:
                return new Vector3(position.x + getSideLength(), position.y + ROOT_3 * getSideLength(), position.z);
            case HexEdgeEnum.BOTTOM_LEFT:
                return new Vector3(position.x - getSideLength(), position.y - ROOT_3 * getSideLength(), position.z);
            case HexEdgeEnum.BOTTOM_RIGHT:
                return new Vector3(position.x + getSideLength(), position.y - ROOT_3 * getSideLength(), position.z);
            default:
                return new Vector3();
        }
    }

    public GameObject gameObject { get; set; }
}
