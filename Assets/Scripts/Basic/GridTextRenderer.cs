using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(MapGrid))]
public class GridTextRenderer : MonoBehaviour
{
    [Header("文本设置")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float textSize = 5f;
    [SerializeField] private Vector3 textOffset = new Vector3(0, 0.1f, 0);

    [Header("显示设置")]
    [SerializeField] private bool showGridIndices = true;
    [SerializeField] private bool showCoordinates = false; // 显示坐标 (x,y) 格式

    private MapGrid mapGrid;
    private MapController mapController;
    private List<TextMeshPro> gridTexts;
    private Vector2Int currentGridSize;

    private void Awake()
    {
        mapGrid = GetComponent<MapGrid>();
        gridTexts = new List<TextMeshPro>();

        // 如果没有设置文本预制体，创建一个默认的
        if (textPrefab == null)
        {
            CreateDefaultTextPrefab();
        }
    }

    private void Start()
    {
        // 获取MapController引用（通过InputController的静态实例）
        if (InputController.mapController != null)
        {
            mapController = InputController.mapController;
        }
        else
        {
            Debug.LogWarning("MapController 未找到，GridTextRenderer 将无法正常工作");
        }
    }

    private void OnDestroy()
    {
        // 清理所有文本对象
        ClearAllTexts();
    }

    private void CreateDefaultTextPrefab()
    {
        // 创建一个简单的文本预制体
        GameObject defaultPrefab = new GameObject("GridTextPrefab");
        TextMeshPro tmp = defaultPrefab.AddComponent<TextMeshPro>();

        // 设置默认文本属性
        tmp.fontSize = textSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        tmp.fontStyle = FontStyles.Normal;

        // 添加黑色描边
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        // 设置文本网格属性
        tmp.text = "0";
        tmp.ForceMeshUpdate();

        // 隐藏预制体
        defaultPrefab.SetActive(false);

        textPrefab = defaultPrefab;
    }

    /// <summary>
    /// 根据网格数组渲染数字文本
    /// </summary>
    /// <param name="gridArray">网格数组，数组下标对应网格位置</param>
    /// <param name="gridSize">网格尺寸</param>
    public void RenderGridTexts(int[] gridArray, Vector2Int gridSize)
    {
        if (gridArray == null || gridArray.Length == 0)
        {
            Debug.LogWarning("网格数组为空，无法渲染文本");
            return;
        }

        if (gridArray.Length != gridSize.x * gridSize.y)
        {
            Debug.LogError($"网格数组长度 ({gridArray.Length}) 与网格尺寸 ({gridSize.x * gridSize.y}) 不匹配");
            return;
        }

        // 清理旧的文本
        ClearAllTexts();

        // 更新当前尺寸
        currentGridSize = gridSize;

        // 创建新的文本
        CreateGridTextsFromArray(gridArray, gridSize);
    }

    /// <summary>
    /// 清空所有文本
    /// </summary>
    public void ClearGridTexts()
    {
        ClearAllTexts();
    }

    private void CreateGridTextsFromArray(int[] gridArray, Vector2Int gridSize)
    {
        if (!showGridIndices) return;

        for (int row = 0; row < gridSize.x; row++)
        {
            for (int col = 0; col < gridSize.y; col++)
            {
                int index = row * gridSize.y + col;
                if (index < gridArray.Length)
                {
                    // 只有当值不为-1时才创建文本（-1表示不显示文本）
                    if (gridArray[index] != -1)
                    {
                        CreateTextForCell(row, col, gridArray[index]);
                    }
                }
            }
        }
    }

    private void CreateTextForCell(int row, int col, int value)
    {
        if (textPrefab == null) return;

        // 计算文本位置（考虑MapController的实际索引偏移）
        // MapController使用从1开始的索引，所以我们需要调整位置
        // 修复：row对应X轴，col对应Z轴
        Vector3 worldPosition = transform.position + new Vector3(row + 1.5f, 0, col + 1.5f) + textOffset;

        // 实例化文本对象
        GameObject textObj = Instantiate(textPrefab, worldPosition, Quaternion.identity, transform);
        textObj.SetActive(true);

        // 确保文本面向相机
        textObj.transform.Rotate(90, 0, 0); // 翻转文本使其正确显示

        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            // 设置文本内容
            string textContent = GetTextContent(row, col, value);
            tmp.text = textContent;

            // 设置文本属性
            tmp.fontSize = textSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            // 确保黑色描边效果
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;

            // 添加到列表
            gridTexts.Add(tmp);
        }
    }

    private string GetTextContent(int row, int col, int value)
    {
        if (showCoordinates)
        {
            return $"({row},{col})";
        }
        else
        {
            // 显示传入的数值
            return value.ToString();
        }
    }

    private void ClearAllTexts()
    {
        foreach (var text in gridTexts)
        {
            if (text != null)
            {
                DestroyImmediate(text.gameObject);
            }
        }
        gridTexts.Clear();
    }

    // 公共方法：切换显示模式
    public void ToggleShowIndices()
    {
        showGridIndices = !showGridIndices;
        if (!showGridIndices)
        {
            ClearAllTexts();
        }
    }

    public void ToggleShowCoordinates()
    {
        showCoordinates = !showCoordinates;
        // 注意：切换坐标显示模式需要重新调用RenderGridTexts
    }

    // 公共方法：设置文本颜色
    public void SetTextColor(Color color)
    {
        textColor = color;
        foreach (var text in gridTexts)
        {
            if (text != null)
            {
                text.color = color;
            }
        }
    }

    // 公共方法：设置文本大小
    public void SetTextSize(float size)
    {
        textSize = size;
        foreach (var text in gridTexts)
        {
            if (text != null)
            {
                text.fontSize = size;
            }
        }
    }

    // 公共方法：设置文本偏移
    public void SetTextOffset(Vector3 offset)
    {
        textOffset = offset;
        // 注意：更改偏移需要重新调用RenderGridTexts
    }

    // 公共方法：更新所有文本的旋转（当相机移动时调用）
    public void UpdateTextRotations()
    {
        if (Camera.main == null) return;

        foreach (var text in gridTexts)
        {
            if (text != null)
            {
                text.transform.LookAt(Camera.main.transform);
                text.transform.Rotate(0, 180, 0);
            }
        }
    }
}
