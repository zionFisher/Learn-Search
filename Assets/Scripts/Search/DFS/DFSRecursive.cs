using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DFSRecursive
{
    private static HashSet<Vector2Int> visited;
    private static List<Vector2Int> path;
    private static CellType[,] cells;
    private static Vector2Int target;
    private static bool found;

    public static List<Vector2Int> Search(Vector2Int start, Vector2Int end, CellType[,] cells)
    {
        // 初始化静态变量
        DFSRecursive.cells = cells;
        target = end;
        visited = new HashSet<Vector2Int>();
        path = new List<Vector2Int>();
        found = false;

        // 开始递归搜索
        DFS(start);

        // 如果找到路径，返回路径，否则返回空列表
        return found ? path : new List<Vector2Int>();
    }

    private static void DFS(Vector2Int current)
    {
        // 如果已经找到路径或当前位置已访问，直接返回
        if (found || visited.Contains(current))
            return;

        // 将当前位置加入已访问集合和路径
        visited.Add(current);
        path.Add(current);

        // 如果到达目标位置，标记为找到并返回
        if (current == target)
        {
            found = true;
            return;
        }

        // 获取当前位置的整数坐标
        int x = Mathf.RoundToInt(current.x);
        int y = Mathf.RoundToInt(current.y);

        // 遍历所有可能的移动方向
        foreach (Vector2Int dir in Reachability.reachableCells)
        {
            // 计算新位置
            int newX = x + dir.x;
            int newY = y + dir.y;
            Vector2Int next = new Vector2Int(newX, newY);

            // 检查新位置是否有效且可通行
            if (IsValidPosition(newX, newY) &&
                cells[newX, newY] != CellType.Block &&
                cells[newX, newY] != CellType.None &&
                !visited.Contains(next))
            {
                DFS(next);

                // 如果找到路径，直接返回
                if (found)
                    return;
            }
        }

        // 如果所有方向都无法到达目标，回溯（从路径中移除当前位置）
        if (!found)
            path.RemoveAt(path.Count - 1);
    }

    private static bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < cells.GetLength(0) && y >= 0 && y < cells.GetLength(1);
    }
}
