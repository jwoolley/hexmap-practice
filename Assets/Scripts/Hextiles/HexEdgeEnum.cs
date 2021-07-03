using System;
using System.Collections.Generic;

public enum HexEdgeEnum {
    LEFT,
    TOP_LEFT,
    TOP_RIGHT,
    RIGHT,
    BOTTOM_RIGHT,
    BOTTOM_LEFT,
}

public static class HexEdgeEnumExtensions {
    public static HexEdgeEnum getOppositeEdge(this HexEdgeEnum edge) {
        switch (edge) {
            case HexEdgeEnum.LEFT:
                return HexEdgeEnum.RIGHT;
            case HexEdgeEnum.TOP_LEFT:
                return HexEdgeEnum.BOTTOM_RIGHT;
            case HexEdgeEnum.TOP_RIGHT:
                return HexEdgeEnum.BOTTOM_LEFT;
            case HexEdgeEnum.RIGHT:
                return HexEdgeEnum.LEFT;
            case HexEdgeEnum.BOTTOM_RIGHT:
                return HexEdgeEnum.TOP_LEFT;
            case HexEdgeEnum.BOTTOM_LEFT:
            default:
                return HexEdgeEnum.TOP_RIGHT;
        }
    }

    public static List<HexEdgeEnum> getEnumValues() {
        if (enumValues == null) {
            enumValues = new List<HexEdgeEnum>();
            foreach (HexEdgeEnum edge in Enum.GetValues(typeof(HexEdgeEnum))) {
                enumValues.Add(edge);
            }
        }
        return enumValues;
    }

    private static List<HexEdgeEnum> enumValues;
}