using System;
using UnityEngine;

public class UnityMapHex : MapHex {

    public UnityMapHex(GameObject gameObject) {
        this.gameObject = gameObject;
    }

    private static MeshRenderer referenceMesh;
    public static void initializeReferenceMesh(MeshRenderer mesh) {
        referenceMesh = mesh;

        if (referenceMesh != null) {
            Bounds meshBounds = referenceMesh.bounds;
            Vector3 size = meshBounds.size;
            prefabScale = size.x / defaultMeshXSize;
        }
    }

    // TODO: move this math code to separate unity-level constants file (and possibly calculate these on startup)
    public static readonly float ROOT_3 = Mathf.Sqrt(3);
    public static readonly float ROOT_3_OVER_2 = Mathf.Sqrt(3) / 2.0f; // ratio of side length to (distance from center to edge)
    public static readonly float ROOT_3_OVER_4 = Mathf.Sqrt(3) / 4.0f; // above in terms of outer radius
    private static float hexTileOffset_X = 1.76f; // hex width (i.e. outer radius, i.e. twice side length)
    private static float hexTileOffset_Y = 1.52f;
    private static float defaultMeshXSize = 1.73f;
    private static float prefabScale = 1.0f;

    public static Vector2 getHexTileOffset() {
        return new Vector2(hexTileOffset_X * prefabScale, hexTileOffset_Y * prefabScale);
    }
    public static float getSideLength() {
        return  getHexTileOffset().x / 2.0f;
    }

    public void changeMeshMaterial(Material material) {
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // used to determine the world-space position of a hex positioned next to this hex along the specified edge
    public Vector3 calculateAdajcentPostition(HexEdgeEnum edge) {
        Vector3 position = this.gameObject.transform.position;

        switch (edge) {
            case HexEdgeEnum.LEFT:
                return new Vector3(position.x - getHexTileOffset().x, position.y, position.z);
            case HexEdgeEnum.RIGHT:
                return new Vector3(position.x + getHexTileOffset().x, position.y, position.z);
            case HexEdgeEnum.TOP_LEFT:
                return new Vector3(position.x - getSideLength(), position.y + getMinorDiameter(), position.z);
            case HexEdgeEnum.TOP_RIGHT:
                return new Vector3(position.x + getSideLength(), position.y + getMinorDiameter(), position.z);
            case HexEdgeEnum.BOTTOM_LEFT:
                return new Vector3(position.x - getSideLength(), position.y - getMinorDiameter(), position.z);
            case HexEdgeEnum.BOTTOM_RIGHT:
                return new Vector3(position.x + getSideLength(), position.y - getMinorDiameter(), position.z);
            default:
                return new Vector3();
        }
    }

    // vertex-to-vertex, aka "apothem"
    private static float getMajorDiameter() {
        return 2.0f * getSideLength();
    }

    // edge-to-edge
    private static float getMinorDiameter() {
        return ROOT_3 * getSideLength();
    }

    // TODO: test w/ origin tile at center of map, then implement w/ origin tile at 0,0
    // (so origin tile argument should no longer require an index position)
    public static Vector3 calculateTilePosition(HexPositionPair originTile, HexMapPosition indexPosition) {
        float dx = originTile.position.x - indexPosition.x;
        float dy = originTile.position.y - indexPosition.y - (indexPosition.y % 2 == 0 ? 0.5f : 0f); // this assumes originTile.y is even; should this be generalized?

        float x = originTile.hex.gameObject.transform.position.x + (dx * getMinorDiameter());
        float y = originTile.hex.gameObject.transform.position.y + (dy * getMinorDiameter());

        return new Vector3(x, y, originTile.hex.gameObject.transform.position.z);
    }

    public GameObject gameObject { get; set; }
}
