using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Searcher
{
    private DFSRecursive dfsRecursive;
    private DFSIterative dfsIterative;

    public Searcher()
    {
        dfsRecursive = new DFSRecursive();
        dfsIterative = new DFSIterative();
    }

    // 这里的cells不包含外围一圈None格子
    public List<Vector2Int> Search(SearchType searchType, Vector2Int start, Vector2Int end, CellType[,] cells)
    {
        switch (searchType)
        {
            case SearchType.DFSRecursive:
                return dfsRecursive.Search(start, end, cells);
            case SearchType.DFSIterative:
                return dfsIterative.Search(start, end, cells);
            case SearchType.BFSRecursive:
                Debug.LogError("暂未实现");
                return new List<Vector2Int>();
            case SearchType.BFSIterative:
                Debug.LogError("暂未实现");
                return new List<Vector2Int>();
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
    BFSRecursive,
    BFSIterative,
    AStar,
}
