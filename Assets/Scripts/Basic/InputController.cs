using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputController : MonoBehaviour
{
    public static MapController mapController;

    [SerializeField] private MapGrid mapGrid;

    [Header("默认尺寸")]
    [SerializeField] private int defaultWidth = 10;
    [SerializeField] private int defaultHeight = 8;

    [Header("相机设置")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraPadding = 1f; // 地图边缘的额外空间，防止视线太拥挤
    [SerializeField] private float minCameraSize = 5f; // 最小视野大小，防止视野太小

    [Header("操作控件")]
    [SerializeField] private GameObject actions;
    [SerializeField] private Button hideButton;
    [SerializeField] private Button showButton;
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private Button resizeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Slider brushSizeSlider;
    [SerializeField] private TMP_Text brushSizeText;
    [SerializeField] private Toggle floorToggle;
    [SerializeField] private Toggle blockToggle;
    [SerializeField] private Toggle startToggle;
    [SerializeField] private Toggle endToggle;
    [SerializeField] private GameObject reachability;

    [Header("搜索控件")]
    [SerializeField] private TMP_Text mapSizeText;
    [SerializeField] private TMP_Text searcherText;
    [SerializeField] private TMP_Text searchTimeText;
    [SerializeField] private TMP_Text searchMemoryText;
    [SerializeField] private TMP_Dropdown searcherDropdown;
    [SerializeField] private Button searchButton;

    [Header("文本渲染")]
    [SerializeField] private GridTextRenderer gridTextRenderer;

    private bool isDragging = false;
    private Vector3 lastMousePosition; // 防止重复鼠标事件
    private Plane groundPlane; // 用于检测鼠标射线是否击中地面

    void Awake()
    {
        if (mapGrid == null)
        {
            Debug.LogError("MapGrid 未设置！");
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("未找到主摄像机！");
                return;
            }
        }

        // 如果没有设置GridTextRenderer，尝试自动获取
        if (gridTextRenderer == null)
        {
            gridTextRenderer = GetComponent<GridTextRenderer>();
            if (gridTextRenderer == null)
            {
                gridTextRenderer = FindFirstObjectByType<GridTextRenderer>();
            }
        }

        // 设置Action显示与否
        hideButton.onClick.AddListener(OnHideButtonClick);
        showButton.onClick.AddListener(OnShowButtonClick);

        // 设置cell type
        floorToggle.onValueChanged.AddListener(OnSelectedCellTypeChanged);
        blockToggle.onValueChanged.AddListener(OnSelectedCellTypeChanged);
        startToggle.onValueChanged.AddListener(OnSelectedCellTypeChanged);
        endToggle.onValueChanged.AddListener(OnSelectedCellTypeChanged);

        // 设置画刷
        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = 1;
            brushSizeSlider.maxValue = 5; // 最大5层网格
            brushSizeSlider.wholeNumbers = true; // 只允许整数值
            brushSizeSlider.onValueChanged.AddListener(OnBrushSizeSliderValueChanged);
        }

        // 初始化reachability
        Transform toggles = reachability.transform.GetChild(1);
        foreach (Transform child in toggles)
        {
            child.GetComponent<Toggle>().onValueChanged.AddListener(OnReachabilityToggleValueChanged);
        }
        OnReachabilityToggleValueChanged(true);

        // 创建 MapController
        mapController = new MapController(defaultWidth, defaultHeight, mapGrid);

        // 订阅事件
        mapController.onMapSizeChange += UpdateCamera;
        mapController.onMapSizeChange += UpdateMapSizeText;
        mapController.onMapSizeChange();

        // 设置按钮监听
        resetButton.onClick.AddListener(ResetToDefault);
        resizeButton.onClick.AddListener(ResizeMap);
        searchButton.onClick.AddListener(OnSearchButtonClick);
        clearButton.onClick.AddListener(OnClearButtonClick);

        // 初始化搜索算法下拉框
        InitializeSearcherDropdown();

        // 手动触发一次相机更新
        UpdateCamera();

        // 初始化地面平面
        groundPlane = new Plane(Vector3.up, Vector3.zero);
    }

    private void OnDestroy()
    {
        if (mapController != null)
        {
            mapController.onMapSizeChange -= UpdateCamera;
        }
    }

    private void UpdateCamera()
    {
        if (mainCamera == null) return;

        Vector2Int actualSize = mapController.ActualSize;

        // 计算地图的中心点（考虑外围格子）
        Vector3 center = new Vector3(actualSize.x * 0.5f, 0, actualSize.y * 0.5f);

        // 设置相机位置
        mainCamera.transform.position = new Vector3(center.x, 10, center.z);
        mainCamera.transform.eulerAngles = new Vector3(90, 0, 0);

        // 计算所需的相机大小，使用最长边
        float targetSize = Mathf.Max(actualSize.x, actualSize.y) * 0.5f + cameraPadding;

        // 限制相机大小在合理范围内
        targetSize = Mathf.Max(targetSize, minCameraSize);

        // 设置正交相机的Size
        mainCamera.orthographicSize = targetSize;
    }

    private void ResizeMap()
    {
        ClearPaths();

        if (!int.TryParse(widthInput.text, out int x) || !int.TryParse(heightInput.text, out int y))
        {
            Debug.LogError("输入值必须是有效的整数");
            return;
        }

        if (x <= 0 || y <= 0)
        {
            Debug.LogError("地图尺寸必须大于0");
            return;
        }

        if (x > 100 || y > 100)
        {
            Debug.LogError("地图尺寸不能大于100");
            return;
        }

        mapController.ResizeMap(x, y);

        // 清空文本显示
        if (gridTextRenderer != null)
        {
            gridTextRenderer.ClearGridTexts();
        }
    }

    private void ResetToDefault()
    {
        // 清除路径
        ClearPaths();

        widthInput.text = defaultWidth.ToString();
        heightInput.text = defaultHeight.ToString();
        mapController.ResizeMap(defaultWidth, defaultHeight);

        // 清空文本显示
        if (gridTextRenderer != null)
        {
            gridTextRenderer.ClearGridTexts();
        }
    }

    void Update()
    {
        // 获取当前选中的工具类型
        CellType? selectedType = GetSelectedCellType();

        // 如果没有选中工具，不处理输入
        if (!selectedType.HasValue && !Input.GetMouseButton(1))
        {
            if (isDragging)
            {
                isDragging = false;
                // 清除高亮
                mapGrid.UpdateHighlight(new int[mapController.ActualSize.x * mapController.ActualSize.y], CellType.Floor);
            }
            return;
        }

        // 获取鼠标射线
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        float enter;

        // 如果射线击中地面
        if (groundPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            // 计算网格坐标（考虑外围格子的偏移）
            int gridX = Mathf.FloorToInt(hitPoint.x);
            int gridY = Mathf.FloorToInt(hitPoint.z);
            if (!mapController.CheckInMap(gridX, gridY))
            {
                // 清空原有的高光格子
                mapGrid.UpdateHighlight(new int[mapController.ActualSize.x * mapController.ActualSize.y], CellType.Floor);
                return;
            }

            // 获取画刷大小（层数）
            int layers = Mathf.RoundToInt(brushSizeSlider.value);
            // 计算实际的画刷大小（边长）
            int brushSize = layers * 2 - 1;

            // 更新高亮
            CellType highlightType = Input.GetMouseButton(1) ? CellType.Floor : selectedType.Value;
            mapGrid.UpdateHighlight(mapController.GetHighlightData(gridX, gridY, brushSize), highlightType);

            // 处理鼠标按下
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                // 检查是否是新的位置
                Vector3 currentMousePosition = new Vector3(gridX, 0, gridY);
                if (!isDragging || currentMousePosition != lastMousePosition)
                {
                    isDragging = true;
                    lastMousePosition = currentMousePosition;

                    // 清除路径
                    ClearPaths();

                    // 设置单元格（需要调整坐标以匹配实际的游戏区域）
                    CellType targetType = Input.GetMouseButton(1) ? CellType.Floor : selectedType.Value;

                    // 如果设置的是Start或End类型，先清除所有同类型的格子
                    if (targetType == CellType.Start || targetType == CellType.End)
                    {
                        mapController.ClearCellsOfType(targetType, CellType.Floor);
                    }

                    mapController.SetCells(gridX, gridY, brushSize, targetType);

                    // 清空文本显示
                    if (gridTextRenderer != null)
                    {
                        gridTextRenderer.ClearGridTexts();
                    }
                }
            }
            else
            {
                isDragging = false;
            }
        }
    }

    private CellType? GetSelectedCellType()
    {
        if (floorToggle.isOn) return CellType.Floor;
        if (blockToggle.isOn) return CellType.Block;
        if (startToggle.isOn) return CellType.Start;
        if (endToggle.isOn) return CellType.End;
        return null;
    }

    private void UpdateMapSizeText()
    {
        mapSizeText.text = $"{mapController.MapSize.x}x{mapController.MapSize.y}";
    }

    private void OnHideButtonClick()
    {
        actions.SetActive(false);
        hideButton.gameObject.SetActive(false);
        showButton.gameObject.SetActive(true);
        enabled = false;
    }

    private void OnShowButtonClick()
    {
        actions.SetActive(true);
        hideButton.gameObject.SetActive(true);
        showButton.gameObject.SetActive(false);
        enabled = true;
    }

    private void InitializeSearcherDropdown()
    {
        searcherDropdown.ClearOptions();
        var options = new List<string>();

        foreach (SearchType searchType in System.Enum.GetValues(typeof(SearchType)))
        {
            options.Add(searchType.ToString());
        }

        searcherDropdown.AddOptions(options);
        searcherDropdown.value = 0; // 默认选择第一个选项
    }

    private Searcher searcher;

    private void OnSearchButtonClick()
    {
        // 获取选中的搜索算法
        SearchType selectedSearchType = (SearchType)searcherDropdown.value;

        // 查找起点和终点
        Vector2Int? startPoint = mapController.FindCellOfType(CellType.Start);
        Vector2Int? endPoint = mapController.FindCellOfType(CellType.End);

        if (!startPoint.HasValue || !endPoint.HasValue)
        {
            Debug.LogError("请先设置起点和终点！");
            return;
        }

        // 确保searcher已初始化
        if (searcher == null)
        {
            searcher = new Searcher();
        }

        // 记录开始时间
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 执行搜索
        List<Vector2Int> path = searcher.Search(selectedSearchType, startPoint.Value, endPoint.Value, mapController.Cells);

        // 记录结束时间
        stopwatch.Stop();

        // 更新UI显示
        searcherText.text = $"{selectedSearchType}";
        // 显示微秒精度的时间
        long microseconds = stopwatch.ElapsedTicks / (System.Diagnostics.Stopwatch.Frequency / 1000000);
        searchTimeText.text = $"{microseconds}μs";
        searchMemoryText.text = $"Not Implemented!";

        // 更新地图显示路径
        UpdatePathOnMap(path);

        // 显示搜索结果文本
        DisplaySearchResultText(path);
    }

    private void UpdatePathOnMap(List<Vector2Int> path)
    {
        var cellChanges = new List<(int row, int col, CellType type)>();

        // 清除之前的路径
        for (int i = 0; i < mapController.MapSize.x; i++)
        {
            for (int j = 0; j < mapController.MapSize.y; j++)
            {
                CellType currentType = mapController.GetCell(i, j);
                if (currentType == CellType.Path1 || currentType == CellType.Path2)
                {
                    cellChanges.Add((i, j, CellType.Floor));
                }
            }
        }

        // 绘制新路径
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int point = path[i];
            int x = Mathf.RoundToInt(point.x);
            int y = Mathf.RoundToInt(point.y);

            // 跳过起点和终点
            CellType currentType = mapController.GetCell(x, y);
            if (currentType == CellType.Start || currentType == CellType.End)
                continue;

            // 交替使用Path1和Path2来显示路径
            CellType pathType = CellType.Path1;
            cellChanges.Add((x, y, pathType));
        }

        // 批量更新地图
        mapController.SetCells(cellChanges);
    }

    /// <summary>
    /// 显示搜索结果文本
    /// </summary>
    /// <param name="path">搜索路径</param>
    private void DisplaySearchResultText(List<Vector2Int> path)
    {
        gridTextRenderer.ClearGridTexts();
        if (gridTextRenderer == null || path == null || path.Count == 0) return;

        Vector2Int gridSize = mapController.MapSize;
        int[] textArray = new int[gridSize.x * gridSize.y];

        // 初始化数组为-1（表示不显示文本）
        for (int i = 0; i < textArray.Length; i++)
        {
            textArray[i] = -1;
        }

        // 在路径上显示步骤编号
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int point = path[i];
            int index = point.x * gridSize.y + point.y;
            if (index >= 0 && index < textArray.Length)
            {
                textArray[index] = i; // 显示步骤编号
            }
        }

        // 渲染文本
        gridTextRenderer.RenderGridTexts(textArray, gridSize);
    }

    private void OnSelectedCellTypeChanged(bool isOn)
    {
        if (isOn)
        {
            RestrictBrushSizeIfNeeded();
        }
    }

    private void OnBrushSizeSliderValueChanged(float value)
    {
        RestrictBrushSizeIfNeeded();
        brushSizeText.text = $"Brush Size: {brushSizeSlider.value}";
    }

    private void RestrictBrushSizeIfNeeded()
    {
        CellType? selectedType = GetSelectedCellType();
        if (selectedType == CellType.Start || selectedType == CellType.End)
        {
            brushSizeSlider.value = 1;
        }
    }

    private void OnReachabilityToggleValueChanged(bool isOn)
    {
        Reachability.reachableCells = new List<Vector2Int>();
        Transform toggles = reachability.transform.GetChild(1);
        for (int i = 0; i < toggles.childCount; i++)
        {
            Toggle toggle = toggles.GetChild(i).GetComponent<Toggle>();
            if (toggle.isOn && i != 4)
            {
                Reachability.reachableCells.Add(Reachability.directions[i]);
            }
        }
    }

    private void ClearPaths()
    {
        mapController.ClearCellsOfType(CellType.Path1, CellType.Floor);
        mapController.ClearCellsOfType(CellType.Path2, CellType.Floor);
    }

    private void OnClearButtonClick()
    {
        // 清除所有非None类型的格子为Floor
        mapController.ClearCellsOfType(CellType.Block, CellType.Floor);
        mapController.ClearCellsOfType(CellType.Start, CellType.Floor);
        mapController.ClearCellsOfType(CellType.End, CellType.Floor);
        mapController.ClearCellsOfType(CellType.Path1, CellType.Floor);
        mapController.ClearCellsOfType(CellType.Path2, CellType.Floor);

        // 清空文本显示
        if (gridTextRenderer != null)
        {
            gridTextRenderer.ClearGridTexts();
        }
    }
}
