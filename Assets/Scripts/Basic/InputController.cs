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

    [Header("控件")]
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private Button resizeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Slider brushSizeSlider;
    [SerializeField] private TextMeshProUGUI brushSizeText;
    [SerializeField] private Toggle floorToggle;
    [SerializeField] private Toggle blockToggle;
    [SerializeField] private Toggle startToggle;
    [SerializeField] private Toggle endToggle;
    [SerializeField] private GameObject reachability;

    [Header("相机设置")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraPadding = 1f; // 地图边缘的额外空间
    [SerializeField] private float minCameraSize = 5f; // 最小视野大小

    private bool isDragging = false;
    private Vector3 lastMousePosition;
    private Plane groundPlane;

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

        // 先创建 MapController
        mapController = new MapController(defaultWidth, defaultHeight, mapGrid);

        // 然后订阅事件
        mapController.onMapSizeChange += UpdateCamera;

        // 最后设置按钮监听
        resetButton.onClick.AddListener(ResetToDefault);
        resizeButton.onClick.AddListener(ResizeMap);

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
    }

    private void ResetToDefault()
    {
        widthInput.text = defaultWidth.ToString();
        heightInput.text = defaultHeight.ToString();
        mapController.ResizeMap(defaultWidth, defaultHeight);
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

            // 获取画刷大小（层数）
            int layers = Mathf.RoundToInt(brushSizeSlider.value);
            // 计算实际的画刷大小（边长）
            int brushSize = layers * 2 - 1;

            // 更新高亮（不需要调整坐标，因为GetHighlightData内部会处理）
            CellType highlightType = Input.GetMouseButton(1) ? CellType.Floor : selectedType.Value;
            mapGrid.UpdateHighlight(mapController.GetHighlightData(gridX - 1, gridY - 1, brushSize), highlightType);

            // 处理鼠标按下
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                // 检查是否是新的位置
                Vector3 currentMousePosition = new Vector3(gridX, 0, gridY);
                if (!isDragging || currentMousePosition != lastMousePosition)
                {
                    isDragging = true;
                    lastMousePosition = currentMousePosition;

                    // 设置单元格（需要调整坐标以匹配实际的游戏区域）
                    CellType targetType = Input.GetMouseButton(1) ? CellType.Floor : selectedType.Value;
                    mapController.SetCells(gridX - 1, gridY - 1, brushSize, targetType);
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
}
