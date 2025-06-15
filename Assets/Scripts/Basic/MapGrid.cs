using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapGrid : MonoBehaviour
{
    [SerializeField] private Material gridMaterial;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private ComputeBuffer cellTypeBuffer;
    private ComputeBuffer highlightBuffer;
    private Vector2Int gridSize;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (gridMaterial == null)
        {
            gridMaterial = new Material(Shader.Find("Custom/Grids"));
        }
        meshRenderer.material = gridMaterial;
    }

    private void OnDestroy()
    {
        // 释放ComputeBuffer
        if (cellTypeBuffer != null)
        {
            cellTypeBuffer.Release();
            cellTypeBuffer = null;
        }

        if (highlightBuffer != null)
        {
            highlightBuffer.Release();
            highlightBuffer = null;
        }
    }

    public void UpdateGrid(CellType[,] cells, Vector2Int size)
    {
        if (cells == null || size.x <= 0 || size.y <= 0)
            return;

        gridSize = size;

        // 创建网格
        CreateMesh();

        // 更新单元格类型数据
        UpdateCellTypes(cells);
    }

    public void UpdateHighlight(int[] highlightData, CellType highlightType)
    {
        if (highlightData == null || highlightData.Length != gridSize.x * gridSize.y)
            return;

        // 创建或更新高亮buffer
        if (highlightBuffer == null)
        {
            highlightBuffer = new ComputeBuffer(highlightData.Length, sizeof(int));
        }

        // 更新buffer数据
        highlightBuffer.SetData(highlightData);

        // 更新shader中的buffer
        gridMaterial.SetBuffer("_HighlightCells", highlightBuffer);
        gridMaterial.SetInt("_HighlightType", (int)highlightType);
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "GridMesh";

        // 创建顶点
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(gridSize.x, 0, 0);
        vertices[2] = new Vector3(0, 0, gridSize.y);
        vertices[3] = new Vector3(gridSize.x, 0, gridSize.y);

        // 创建UV
        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        // 创建三角形
        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private void UpdateCellTypes(CellType[,] cells)
    {
        // 释放旧的buffer
        if (cellTypeBuffer != null)
        {
            cellTypeBuffer.Release();
        }

        // 创建新的buffer
        int totalCells = gridSize.x * gridSize.y;
        cellTypeBuffer = new ComputeBuffer(totalCells, sizeof(int));

        // 将数据展平为一维数组
        int[] cellTypes = new int[totalCells];
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                cellTypes[i * gridSize.y + j] = (int)cells[i, j];
            }
        }

        // 更新buffer数据
        cellTypeBuffer.SetData(cellTypes);

        // 更新shader中的buffer
        gridMaterial.SetBuffer("_CellTypes", cellTypeBuffer);
        gridMaterial.SetInt("_GridWidth", gridSize.x);
        gridMaterial.SetInt("_GridHeight", gridSize.y);

        // 同时创建高亮buffer
        if (highlightBuffer != null)
        {
            highlightBuffer.Release();
        }
        highlightBuffer = new ComputeBuffer(totalCells, sizeof(int));
        int[] emptyHighlight = new int[totalCells];
        highlightBuffer.SetData(emptyHighlight);
        gridMaterial.SetBuffer("_HighlightCells", highlightBuffer);
        gridMaterial.SetInt("_HighlightType", (int)CellType.Floor);
    }
}
