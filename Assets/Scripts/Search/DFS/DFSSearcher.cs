using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DFSSearcher : ISearchable
{
    public List<Vector2> Search(Vector2 start, Vector2 end, CellType[,] cells)
    {
        return null;
    }

    private bool DFS_Recursive(Vector2 current, Vector2 end, CellType[,] cells)
    {
        return false;
    }

    private List<Vector2> BuildPath(Vector2 start, Vector2 end)
    {
        return null;
    }

    private List<Vector2> DFS_Iterative(Vector2 start, Vector2 end, CellType[,] cells)
    {
        return new List<Vector2>();
    }
}
