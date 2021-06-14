using System;
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
        referenceHex.setAdjacentHex(edge, newHex);

        HexMapPosition newHexPosition = UnityMapHex.getAdjacentIndex(referencePosition, edge);

        HexMapGenerator.DEBUG_HEX_LABEL_LIST.Clear();

        List<MapHex> adjacentHexes = new List<MapHex>(newHex.getAdjacentHexes(newHexPosition, this.placementArray).Values);

        adjacentHexes
            .Where(hex => hex != null)
            .ToList()
            .ForEach(hex => {
                HexMapGenerator.DEBUG_HEX_LABEL_LIST[(UnityMapHex)hex] = $"** ({hex.hexId}) **";
            });

        List<HexTileRegion> matchingNeighborRegions = regions.ToList().Where(region => {
            return adjacentHexes.Any(adjacentHex => {
                return region.containsHex(adjacentHex) && region.color == newHexColor;
            });
        }).ToList();

        if (matchingNeighborRegions.Count == 0) {
            addNewRegion(newHex, newHexColor);
        } else {
            HexTileRegion adoptingRegion = matchingNeighborRegions.OrderBy(region => region.regionId).First();
            adoptingRegion.addHex(newHex);
            matchingNeighborRegions.GetRange(1, matchingNeighborRegions.Count - 1)
                .ForEach(region => {
                    adoptingRegion.absorbRegion(region);
                    region.getHexes().ForEach(hex => {
                        HexMapGenerator.assignHexMaterialFromRegion(adoptingRegion, (UnityMapHex)hex);
                    });
                    this.regions.Remove(region);
                });
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
        HexTileRegion newRegion = new HexTileRegion(hex, color);
        regions.Add(newRegion);
        HexMapGenerator.assignRandomRegionMaterial(newRegion);
    }

    private bool containsHex(MapHex hex) {
        return regions.Any(region => region.containsHex(hex));
    } 

    private HashSet<HexTileRegion> regions;
    private MapHex[,] placementArray;
}
