using System;

public enum HexTileColor {
    RED,
    BLUE,
    GREEN,
    PURPLE,
    ORANGE,
    YELLOW
};

public static class HexTileColorExtensions {
    public static HexTileColor getRandomColor() {
        return (HexTileColor)UnityEngine.Random.Range(0, Enum.GetNames(typeof(HexTileColor)).Length);
    }
}