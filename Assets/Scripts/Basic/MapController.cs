using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellType
{
    None = -1,
    Floor,
    Block,
    Start,
    End,
    Path1,
    Path2
}

public class MapController
{
    public Action onMapSizeChange;

    private MapGrid mapGrid;
    private Vector2Int mapSize;

    private CellType[,] actualCells;

    public CellType[,] Cells => GetCellsArray();
    public CellType[,] ActualCells => actualCells;

    public Vector2Int MapSize => mapSize;
    public Vector2Int ActualSize => new Vector2Int(mapSize.x + 2, mapSize.y + 2); // 包含外围格子的实际大小

    public MapController(int x, int y, MapGrid grid)
    {
        if (x <= 0 || y <= 0)
            throw new ArgumentException("地图尺寸必须大于0");

        if (grid == null)
            throw new ArgumentNullException(nameof(grid), "MapGrid 不能为空");

        mapGrid = grid;
        ResizeMap(x, y);
    }

    // 获取内部单元格数组（不包含外围None格子）
    private CellType[,] GetCellsArray()
    {
        if (actualCells == null) return null;

        var result = new CellType[mapSize.x, mapSize.y];
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                result[i, j] = actualCells[i + 1, j + 1];
            }
        }
        return result;
    }

    public CellType GetCell(int row, int col)
    {
        ValidateIndex(row + 1, col + 1);
        return actualCells[row + 1, col + 1];
    }

    public void ValidateIndex(int row, int col)
    {
        if (row < 0 || row >= ActualSize.x)
            throw new IndexOutOfRangeException($"行索引 {row} 超出范围 [0, {ActualSize.x})");

        if (col < 0 || col >= ActualSize.y)
            throw new IndexOutOfRangeException($"列索引 {col} 超出范围 [0, {ActualSize.y})");
    }

    public bool CheckInMap(int row, int col)
    {
        if (row < 1 || row > mapSize.x)
            return false;
        if (col < 1 || col > mapSize.y)
            return false;

        return true;
    }

    public void ResizeMap(int x, int y)
    {
        if (x <= 0 || y <= 0)
            throw new ArgumentException("地图尺寸必须大于0");

        // 如果尺寸相同，直接返回
        if (actualCells != null && x == mapSize.x && y == mapSize.y)
            return;

        // 创建新数组（包含外围格子）
        var newCells = new CellType[x + 2, y + 2];

        // 初始化外围格子为None
        for (int i = 0; i < x + 2; i++)
        {
            newCells[i, 0] = CellType.None;
            newCells[i, y + 1] = CellType.None;
        }
        for (int j = 0; j < y + 2; j++)
        {
            newCells[0, j] = CellType.None;
            newCells[x + 1, j] = CellType.None;
        }

        // 如果有现有数据，复制重叠部分（不包括外围格子）
        if (actualCells != null)
        {
            int copyX = Mathf.Min(x, mapSize.x);
            int copyY = Mathf.Min(y, mapSize.y);

            for (int i = 0; i < copyX; i++)
            {
                for (int j = 0; j < copyY; j++)
                {
                    newCells[i + 1, j + 1] = actualCells[i + 1, j + 1];
                }
            }
        }

        // 更新引用和尺寸
        actualCells = newCells;
        mapSize = new Vector2Int(x, y);

        // 更新网格显示
        UpdateGrid();

        onMapSizeChange?.Invoke();
    }

    // 批量设置单元格
    public void SetCells(int centerRow, int centerCol, int brushSize, CellType value)
    {
        centerRow--;
        centerCol--;
        ValidateIndex(centerRow, centerCol);

        bool needUpdate = false;
        int halfSize = brushSize / 2;

        for (int i = -halfSize; i <= halfSize; i++)
        {
            for (int j = -halfSize; j <= halfSize; j++)
            {
                int targetRow = centerRow + i;
                int targetCol = centerCol + j;

                if (targetRow >= 0 && targetRow < mapSize.x &&
                    targetCol >= 0 && targetCol < mapSize.y)
                {
                    // 转换为实际数组索引
                    int actualRow = targetRow + 1;
                    int actualCol = targetCol + 1;

                    // 只有当值不同时才更新
                    if (actualCells[actualRow, actualCol] != value)
                    {
                        actualCells[actualRow, actualCol] = value;
                        needUpdate = true;
                    }
                }
            }
        }

        if (needUpdate)
        {
            UpdateGrid();
        }
    }

    // 获取高亮数据
    public int[] GetHighlightData(int centerRow, int centerCol, int brushSize)
    {
        centerRow--;
        centerCol--;
        ValidateIndex(centerRow, centerCol);

        Vector2Int actualSize = ActualSize;
        int[] highlightData = new int[actualSize.x * actualSize.y];

        if (centerRow < 0 || centerCol < 0)
            return highlightData;

        int halfSize = brushSize / 2;

        for (int i = -halfSize; i <= halfSize; i++)
        {
            for (int j = -halfSize; j <= halfSize; j++)
            {
                int targetRow = centerRow + i;
                int targetCol = centerCol + j;

                if (targetRow >= 0 && targetRow < mapSize.x &&
                    targetCol >= 0 && targetCol < mapSize.y)
                {
                    // 转换为实际数组索引
                    int actualRow = targetRow + 1;
                    int actualCol = targetCol + 1;
                    highlightData[actualRow * actualSize.y + actualCol] = 1;
                }
            }
        }

        return highlightData;
    }

    public void ClearCellsOfType(CellType cellType, CellType targetType)
    {
        bool needUpdate = false;

        // 只遍历实际的游戏区域（不包括外围格子）
        for (int i = 1; i <= mapSize.x; i++)
        {
            for (int j = 1; j <= mapSize.y; j++)
            {
                if (actualCells[i, j] == cellType)
                {
                    actualCells[i, j] = targetType;
                    needUpdate = true;
                }
            }
        }

        if (needUpdate)
        {
            UpdateGrid();
        }
    }

    // 查找指定类型的单元格位置
    public Vector2Int? FindCellOfType(CellType cellType)
    {
        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                if (GetCell(i, j) == cellType)
                {
                    return new Vector2Int(i, j);
                }
            }
        }
        return null;
    }

    public void SetCell(int row, int col, CellType value)
    {
        // 转换为实际数组索引（考虑外围格子）
        row++;
        col++;
        ValidateIndex(row, col);

        // 如果值相同，不进行更新
        if (actualCells[row, col] == value)
            return;

        actualCells[row, col] = value;
        UpdateGrid();
    }

    public void SetCells(List<(int row, int col, CellType type)> cellChanges)
    {
        bool needUpdate = false;

        foreach (var change in cellChanges)
        {
            int actualRow = change.row + 1;
            int actualCol = change.col + 1;

            try
            {
                ValidateIndex(actualRow, actualCol);

                if (actualCells[actualRow, actualCol] != change.type)
                {
                    actualCells[actualRow, actualCol] = change.type;
                    needUpdate = true;
                }
            }
            catch (IndexOutOfRangeException)
            {
                // 忽略超出范围的索引
                continue;
            }
        }

        if (needUpdate)
        {
            UpdateGrid();
        }
    }

    private void UpdateGrid()
    {
        if (mapGrid != null)
        {
            mapGrid.UpdateGrid(actualCells, ActualSize);
        }
    }
}
