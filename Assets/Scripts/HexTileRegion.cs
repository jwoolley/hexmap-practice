using System;
using System.Collections.Generic;
using UnityEngine;

public class HexTileRegion {
    static int regionIdCounter = 1;
    private static int assignNewRegionId() {
        return regionIdCounter++;
    }


    public HexTileRegion(MapHex startHex, HexTileColor color) {
        this.color = color;
        this.hexes = new HashSet<MapHex>();
        this.regionId = assignNewRegionId();
        this.addHex(startHex);
    }

    public bool containsHex(MapHex hex) {
        return this.hexes.Contains(hex);
    }

    public void addHex(MapHex hex) {
        this.hexes.Add(hex);
        if (this.getSize() == 1) {
            Debug.Log($"Created new hex region Region_{regionId} of color {color}");
        } else {
            Debug.Log($"Added hex to Region_{regionId}[size={this.getSize()}, color={color}]");
        }
    }

    public int getSize() {
        return this.hexes.Count;
    }

    public HexTileColor color { get; private set; }
    private HashSet<MapHex> hexes;
    private int regionId;
}
