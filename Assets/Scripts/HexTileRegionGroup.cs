﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexTileRegionGroup  {
    public HexTileRegionGroup(MapHex[,] placementArray, MapHex originHex, HexTileColor color) {
        regions = new HashSet<HexTileRegion>();
        this.placementArray = placementArray;
        addNewRegion(originHex, color);
    }

    public void addHex(MapHex referenceHex, MapHex newHex, HexMapPosition referencePosition,
        HexEdgeEnum edge, HexTileColor newHexColor) {

        if (!this.containsHex(referenceHex)) {
            throw new Exception("Can't find reference hex in region group");
        }
        referenceHex.setAdjacentHex(edge, newHex, this.placementArray);

        HexMapPosition newHexPosition = UnityMapHex.getAdjacentIndex(referencePosition, edge);

        List<MapHex> adjacentHexes = new List<MapHex>(newHex.getAdjacentHexes(newHexPosition, this.placementArray).Values);

        HexTileRegion adoptingRegion = regions.FirstOrDefault(region => {
            return adjacentHexes.Any(adjacentHex => {
                return region.containsHex(adjacentHex) && region.color == newHexColor;
            });
        });

        if (adoptingRegion == null) {
            addNewRegion(newHex, newHexColor);
        } else {
            adoptingRegion.addHex(newHex);
        }
    }

    private Dictionary<MapHex, HexTileRegion> hexRegionDict = new Dictionary<MapHex, HexTileRegion>();
    public HexTileRegion getRegionContainingHex(MapHex hex) {
        if (!hexRegionDict.ContainsKey(hex)) {
            HexTileRegion region = regions.FirstOrDefault(_region => {
                return _region.containsHex(hex);
            });
            if (region == null) {
                Debug.LogWarning($"Unable to find region for hex {hex.hexId}");
                return null;
            } else {
                hexRegionDict[hex] = region;
            }
        }
        return hexRegionDict[hex];
    }

    private void addNewRegion(MapHex hex, HexTileColor color) {
        regions.Add(new HexTileRegion(hex, color));
    }

    private bool containsHex(MapHex hex) {
        return regions.Any(region => region.containsHex(hex));
    } 

    private HashSet<HexTileRegion> regions;
    private MapHex[,] placementArray;
}
