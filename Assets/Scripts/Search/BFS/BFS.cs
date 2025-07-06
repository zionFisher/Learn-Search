using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BFS
{
    public static List<Vector2Int> Search(Vector2Int start, Vector2Int end, CellType[,] cells)
    {
        // 初始化数据结构
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        bool found = false;

        // 将起点加入队列和已访问集合
        queue.Enqueue(start);
        visited.Add(start);
        parent[start] = start; // 起点的父节点是自己

        while (queue.Count > 0 && !found)
        {
            Vector2Int current = queue.Dequeue();

            // 如果到达目标位置，标记为找到
            if (current == end)
            {
                found = true;
                break;
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
                if (IsValidPosition(newX, newY, cells) &&
                    cells[newX, newY] != CellType.Block &&
                    cells[newX, newY] != CellType.None &&
                    !visited.Contains(next))
                {
                    // 将新位置加入队列和已访问集合
                    queue.Enqueue(next);
                    visited.Add(next);
                    parent[next] = current; // 记录父节点用于重建路径
                }
            }
        }

        // 如果找到路径，重建路径
        if (found)
        {
            return ReconstructPath(start, end, parent);
        }

        // 否则返回空列表
        return new List<Vector2Int>();
    }

    private static List<Vector2Int> ReconstructPath(Vector2Int start, Vector2Int end, Dictionary<Vector2Int, Vector2Int> parent)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;

        // 从终点回溯到起点
        while (current != start)
        {
            path.Add(current);
            current = parent[current];
        }
        path.Add(start);

        // 反转路径，使其从起点到终点
        path.Reverse();
        return path;
    }

    private static bool IsValidPosition(int x, int y, CellType[,] cells)
    {
        return x >= 0 && x < cells.GetLength(0) && y >= 0 && y < cells.GetLength(1);
    }
}
