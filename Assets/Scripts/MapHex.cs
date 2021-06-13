using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapHex {
    static private readonly MapHex PLACEHOLDER_HEX = new ImmutableMapHex();

    private class ImmutableMapHex : MapHex {
        public ImmutableMapHex() {
            adjacentHexes = new Dictionary<HexEdgeEnum, MapHex>();
        }

        public override void setAdjacentHex(HexEdgeEnum edge, MapHex hex) {
            Debug.LogWarning("Called setAdjacentHex for ImmutableMapHex");
        }
    }

    static private int hexIdCounter = 1;
    static private int assignHexId() {
        return hexIdCounter++;
    }

    public MapHex() {
        this.hexId = assignHexId();
        adjacentHexes = new Dictionary<HexEdgeEnum, MapHex>();

        HexEdgeEnumExtensions.getEnumValues().ForEach(edge => {
            adjacentHexes.Add(edge, PLACEHOLDER_HEX);
        });
    }

    public MapHex getAdjacentHex(HexEdgeEnum edge) {
        return adjacentHexes[edge];
    }

    public bool hasAdajacentHex(HexEdgeEnum edge) {
        return getAdjacentHex(edge) != PLACEHOLDER_HEX;
    }

    public virtual void setAdjacentHex(HexEdgeEnum edge, MapHex hex) {
        if (hex == null) {
            throw new Exception("A map hex can't be set adjacent to null.");
        }
        if (hex == this) {
            throw new Exception("A map hex can't be set adjacent to itself.");
        }

        MapHex adjacentHex = adjacentHexes[edge];
        if (adjacentHex != null && !adjacentHex.Equals(PLACEHOLDER_HEX) && !adjacentHex.Equals(hex)) {
            Debug.LogWarning($"setAdjacentHex called but hex exists at edge {edge}");
        }

        MapHex adjacentHexCorrespondingHex = hex.adjacentHexes[edge.getOppositeEdge()];
        if (adjacentHexCorrespondingHex != null
            && !adjacentHexCorrespondingHex.Equals(PLACEHOLDER_HEX)
            && !adjacentHexCorrespondingHex.Equals(this)) {
            Debug.LogWarning($"setAdjacentHex called other hex has existing hex at edge {edge.getOppositeEdge()}");
        }
        adjacentHexes[edge] = hex;
        hex.adjacentHexes[edge.getOppositeEdge()] = this;
    }

    public bool isAdjacent(MapHex otherHex) {
        return otherHex != null && adjacentHexes.Any(hex => otherHex.Equals(hex));
    }

    static public HexMapPosition getAdjacentIndex(HexMapPosition currentIndex, HexEdgeEnum edge) {
        int x = currentIndex.Item1;
        int y = currentIndex.Item2;
        switch (edge) {
            case HexEdgeEnum.LEFT:
                return new HexMapPosition(x - 1, y);
            case HexEdgeEnum.TOP_LEFT:
                return new HexMapPosition((y % 2 == 0) ? x : x - 1, y - 1);
            case HexEdgeEnum.TOP_RIGHT:
                return new HexMapPosition((y % 2 == 0) ? x + 1 : x, y - 1);
            case HexEdgeEnum.RIGHT:
                return new HexMapPosition(x + 1, y);
            case HexEdgeEnum.BOTTOM_LEFT:
                return new HexMapPosition((y % 2 == 0) ? x : x - 1, y + 1);
            case HexEdgeEnum.BOTTOM_RIGHT:
                return new HexMapPosition((y % 2 == 0) ? x + 1 : x, y + 1);
            default:
                return currentIndex;
        }
    }

    public Dictionary<HexEdgeEnum, MapHex> getAdjacentHexes(HexMapPosition referencePosition, MapHex[,] placements) {
        Dictionary<HexEdgeEnum, MapHex> adjacentHexes = new Dictionary<HexEdgeEnum, MapHex>();
        HexEdgeEnumExtensions.getEnumValues().ForEach(edge => {
            HexMapPosition adjacentPosition = MapHex.getAdjacentIndex(referencePosition, edge);
            adjacentHexes.Add(edge, placements[adjacentPosition.x, adjacentPosition.y]);
        });
        return adjacentHexes;
    }

    private Dictionary<HexEdgeEnum, MapHex> adjacentHexes;
    public int hexId { get; private set; }
}
