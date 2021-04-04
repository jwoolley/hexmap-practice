using System;
using System.Collections.Generic;

public class MapHex {
    private readonly MapHex PLACEHOLDER_HEX = new MapHex();

    public MapHex() {
        adjacentHexes = new Dictionary<HexEdgeEnum, MapHex>();

        foreach(HexEdgeEnum edge in Enum.GetValues(typeof(HexEdgeEnum))) {
            adjacentHexes.Add(edge, PLACEHOLDER_HEX);
        }
    }

    public MapHex getAdjacentHex(HexEdgeEnum edge) {
        return null;
    }

    public Boolean hasAdajacentHex(HexEdgeEnum edge) {
        return getAdjacentHex(edge) != PLACEHOLDER_HEX;
    }

    public void setAdjacentHex(HexEdgeEnum edge, MapHex hex) {
        if (hex == this) {
            throw new Exception("A map hex can't be set adjacent to itself.");
        }
    }

    private Dictionary<HexEdgeEnum, MapHex> adjacentHexes;
}
