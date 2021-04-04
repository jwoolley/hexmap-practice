using System;
using System.Collections.Generic;
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

    public MapHex() {
        adjacentHexes = new Dictionary<HexEdgeEnum, MapHex>();

        foreach(HexEdgeEnum edge in Enum.GetValues(typeof(HexEdgeEnum))) {
            adjacentHexes.Add(edge, PLACEHOLDER_HEX);
        }
    }

    public MapHex getAdjacentHex(HexEdgeEnum edge) {
        return adjacentHexes[edge];
    }

    public Boolean hasAdajacentHex(HexEdgeEnum edge) {
        return getAdjacentHex(edge) != PLACEHOLDER_HEX;
    }

    public virtual void setAdjacentHex(HexEdgeEnum edge, MapHex hex) {
        if (hex == this) {
            throw new Exception("A map hex can't be set adjacent to itself.");
        }
    }

    private Dictionary<HexEdgeEnum, MapHex> adjacentHexes;
}
