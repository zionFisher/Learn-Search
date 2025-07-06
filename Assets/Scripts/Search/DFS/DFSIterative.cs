using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DFSIterative
{
    public static List<Vector2Int> Search(Vector2Int start, Vector2Int end, CellType[,] cells)
    {
        // 初始化数据结构
        HashSet<Vector2Int> visited = new();
        Stack<Vector2Int> stack = new();
        List<Vector2Int> path = new();
        bool found = false;

        // 将起点加入栈和已访问集合
        stack.Push(start);
        visited.Add(start);
        path.Add(start);

        while (stack.Count > 0 && !found)
        {
            Vector2Int current = stack.Peek(); // 只查看栈顶，不弹出

            // 如果到达目标位置，标记为找到
            if (current == end)
            {
                found = true;
                break;
            }

            // 获取当前位置的整数坐标
            int x = Mathf.RoundToInt(current.x);
            int y = Mathf.RoundToInt(current.y);

            bool hasUnvisitedNeighbor = false;

            // 遍历所有可能的移动方向（与递归版本保持相同顺序）
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
                    // 将新位置加入栈和已访问集合
                    stack.Push(next);
                    visited.Add(next);
                    path.Add(next);
                    hasUnvisitedNeighbor = true;
                    break; // 找到一个未访问的邻居就停止，模拟递归的深度优先
                }
            }

            // 如果没有未访问的邻居，回溯（弹出栈顶元素并从路径中移除）
            if (!hasUnvisitedNeighbor)
            {
                stack.Pop();
                if (path.Count > 0)
                {
                    path.RemoveAt(path.Count - 1);
                }
            }
        }

        // 如果找到路径，返回路径，否则返回空列表
        return found ? path : new List<Vector2Int>();
    }

    private static bool IsValidPosition(int x, int y, CellType[,] cells)
    {
        return x >= 0 && x < cells.GetLength(0) && y >= 0 && y < cells.GetLength(1);
    }
}
