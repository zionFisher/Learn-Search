using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DFSIterative
{
    public List<Vector2Int> Search(Vector2Int start, Vector2Int end, CellType[,] cells)
    {
        // 初始化数据结构
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        List<Vector2Int> path = new List<Vector2Int>();

        // 将起点加入栈和已访问集合
        stack.Push(start);
        visited.Add(start);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();

            // 如果到达目标位置，构建路径并返回
            if (current == end)
            {
                // 回溯构建路径
                Vector2Int temp = current;
                while (parent.ContainsKey(temp))
                {
                    path.Insert(0, temp);
                    temp = parent[temp];
                }
                path.Insert(0, start);
                return path;
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
                    // 将新位置加入栈和已访问集合
                    stack.Push(next);
                    visited.Add(next);
                    parent[next] = current;
                }
            }
        }

        // 如果没有找到路径，返回空列表
        return new List<Vector2Int>();
    }

    private bool IsValidPosition(int x, int y, CellType[,] cells)
    {
        return x >= 0 && x < cells.GetLength(0) && y >= 0 && y < cells.GetLength(1);
    }
}
