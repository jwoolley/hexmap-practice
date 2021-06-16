using System;
using System.Collections.Generic;
using System.Linq;
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
        // if adding more logic here, remember that absorbRegion adds hexes too
        this.hexes.Add(hex);

        //if (this.getSize() == 1) {
        //    Debug.Log($"Created new hex region Region_{regionId} of color {color}");
        //} else {
        //    Debug.Log($"Added hex to Region_{regionId}[size={this.getSize()}, color={color}]");
        //}
    }

    public void absorbRegion(HexTileRegion otherRegion) {
        if (otherRegion == this) {
            Debug.LogWarning($"Region is trying to absorb itself: " + regionId);
            return;
        }
        Debug.Log($"Merging region {otherRegion.regionId} [size={otherRegion.hexes.Count}] into {regionId}");
        otherRegion.hexes.ToList().ForEach(hex => this.hexes.Add(hex));
    }
     
    public int getSize() {
        return this.hexes.Count;
    }

    public List<MapHex> getHexes() {
        return new List<MapHex>(hexes);
    }

    public HexTileColor color { get; private set; }
    private HashSet<MapHex> hexes;
    public int regionId { get; private set; }
}
