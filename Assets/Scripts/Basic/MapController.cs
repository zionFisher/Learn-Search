using System;
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

    private CellType[,] cells;
    private Vector2Int mapSize;
    private MapGrid mapGrid;

    public CellType[,] Cells => cells;
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

    public CellType GetCell(int row, int col)
    {
        // 转换为实际数组索引（考虑外围格子）
        row++;
        col++;
        ValidateIndex(row, col);
        return cells[row, col];
    }

    public void SetCell(int row, int col, CellType value)
    {
        // 转换为实际数组索引（考虑外围格子）
        row++;
        col++;
        ValidateIndex(row, col);

        // 如果值相同，不进行更新
        if (cells[row, col] == value)
            return;

        cells[row, col] = value;
        UpdateGrid();
    }

    private void ValidateIndex(int row, int col)
    {
        if (row < 0 || row >= ActualSize.x)
            throw new IndexOutOfRangeException($"行索引 {row} 超出范围 [0, {ActualSize.x})");

        if (col < 0 || col >= ActualSize.y)
            throw new IndexOutOfRangeException($"列索引 {col} 超出范围 [0, {ActualSize.y})");
    }

    public void ResizeMap(int x, int y)
    {
        if (x <= 0 || y <= 0)
            throw new ArgumentException("地图尺寸必须大于0");

        // 如果尺寸相同，直接返回
        if (cells != null && x == mapSize.x && y == mapSize.y)
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
        if (cells != null)
        {
            int copyX = Mathf.Min(x, mapSize.x);
            int copyY = Mathf.Min(y, mapSize.y);

            for (int i = 0; i < copyX; i++)
            {
                for (int j = 0; j < copyY; j++)
                {
                    newCells[i + 1, j + 1] = cells[i + 1, j + 1];
                }
            }
        }

        // 更新引用和尺寸
        cells = newCells;
        mapSize = new Vector2Int(x, y);

        // 更新网格显示
        UpdateGrid();

        onMapSizeChange?.Invoke();
    }

    // 获取一维数组形式的单元格数据，用于传输到Shader
    public int[] GetCellsAsArray()
    {
        Vector2Int actualSize = ActualSize;
        int[] result = new int[actualSize.x * actualSize.y];

        for (int i = 0; i < actualSize.x; i++)
        {
            for (int j = 0; j < actualSize.y; j++)
            {
                result[i * actualSize.y + j] = (int)cells[i, j];
            }
        }

        return result;
    }

    // 批量设置单元格
    public void SetCells(int centerRow, int centerCol, int brushSize, CellType value)
    {
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
                    if (cells[actualRow, actualCol] != value)
                    {
                        cells[actualRow, actualCol] = value;
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

    private void UpdateGrid()
    {
        if (mapGrid != null)
        {
            mapGrid.UpdateGrid(cells, ActualSize);
        }
    }
}
