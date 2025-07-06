using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Searcher
{
    // 这里的cells不包含外围一圈None格子
    public List<Vector2Int> Search(SearchType searchType, Vector2Int start, Vector2Int end, CellType[,] cells)
    {
        switch (searchType)
        {
            case SearchType.DFSRecursive:
                return DFSRecursive.Search(start, end, cells);
            case SearchType.DFSIterative:
                return DFSIterative.Search(start, end, cells);
            case SearchType.BFS:
                return BFS.Search(start, end, cells);
            case SearchType.AStar:
                Debug.LogError("暂未实现");
                return new List<Vector2Int>();
            default:
                return new List<Vector2Int>();
        }
    }
}

public enum SearchType
{
    DFSRecursive,
    DFSIterative,
    BFS,
    AStar,
}
