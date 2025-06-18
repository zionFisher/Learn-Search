using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DFSSearcher : ISearchable
{
    private bool[,] visited;
    private Dictionary<Vector2, Vector2> parent;
    private readonly Vector2[] directions = new Vector2[]
    {
        new Vector2(0, 1),  // 上
        new Vector2(1, 0),  // 右
        new Vector2(0, -1), // 下
        new Vector2(-1, 0)  // 左
    };

    public List<Vector2> Search(Vector2 start, Vector2 end, CellType[,] cells)
    {
        // 初始化访问标记和父节点记录
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        visited = new bool[width, height];
        parent = new Dictionary<Vector2, Vector2>();

        // 开始递归搜索
        if (DFS_Recursive(start, end, cells))
        {
            // 如果找到路径，构建路径列表
            return BuildPath(start, end);
        }

        // 没找到路径，返回空列表
        return new List<Vector2>();
    }

    private bool DFS_Recursive(Vector2 current, Vector2 end, CellType[,] cells)
    {
        // 检查是否到达目标
        if (current == end)
        {
            return true;
        }

        // 标记当前节点为已访问
        visited[(int)current.x, (int)current.y] = true;

        // 遍历四个方向
        foreach (Vector2 dir in directions)
        {
            Vector2 next = current + dir;
            int nextX = (int)next.x;
            int nextY = (int)next.y;

            // 检查边界
            if (nextX < 0 || nextX >= cells.GetLength(0) ||
                nextY < 0 || nextY >= cells.GetLength(1))
            {
                continue;
            }

            // 检查是否是可通过的格子且未访问过
            if (cells[nextX, nextY] != CellType.Block && !visited[nextX, nextY])
            {
                // 记录父节点
                parent[next] = current;

                // 递归搜索
                if (DFS_Recursive(next, end, cells))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private List<Vector2> BuildPath(Vector2 start, Vector2 end)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 current = end;

        // 从终点回溯到起点
        while (current != start)
        {
            path.Add(current);
            current = parent[current];
        }
        path.Add(start);

        // 反转路径，使其从起点指向终点
        path.Reverse();
        return path;
    }

    private List<Vector2> DFS_Iterative(Vector2 start, Vector2 end, CellType[,] cells)
    {
        return new List<Vector2>();
    }
}
