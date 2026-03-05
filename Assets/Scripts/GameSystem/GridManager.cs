using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

[DefaultExecutionOrder(-20)]
public class GridManager : MonoBehaviour {
    public static GridManager Instance;

    public int initGridSize = 2;
    public int gridSize;
    public float tileSize = 1.8f;
    public float tileYPosition = 2.4f; // 地块Y轴位置
    public GameObject tilePrefab; // 地块的预制体

    // Hover 状态
    private Vector2Int _lastHoverTile = Vector2Int.one * -1;

    // _gridData 用于存档和读档
    public List<TileData> _gridData = new List<TileData>();
    // _gridDataDict 用于在运行时快速访问地块数据
    public Dictionary<Vector2Int, TileData> _gridDataDict = new Dictionary<Vector2Int, TileData>();

    [Header("存档事件")]
    public UnityEvent DataChange;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this.gameObject);
        }
    }

    void Start() {
        GenerateGrid();

        // 网格生成完毕后，通知 GameTimeManager 可以处理离线天数了
        if (GameTimeManager.Instance != null) {
            GameTimeManager.Instance.OnGridManagerReady();
        }
    }

    void Update() {
        // 只在Playing状态下处理Hover
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            return;
        }

        // 处理Hover
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            // 假设鼠标下方的物体上一定有一个 TileComponent 组件
            var tile = hit.collider.GetComponent<Tile>();
            if (tile != null && tile.data != null) {
                OnTileHover(tile.data.x, tile.data.y);
            }
        } else {
            OnTileHover(-1, -1); // 没有 Hover 任何地块
        }
    }

    public void ResetAllGridDry() {
        foreach (var tileData in _gridData) {
            if (tileData.IsEmpty == false) {
                tileData.consecutiveUnwateredDays += 1;
                var stageData = CropDatabase.GetStageData(tileData.cropId, tileData.currentStageIndex);
                if (stageData == null) {
                    Debug.LogError($"无法找到作物 {tileData.cropId} 的阶段数据，索引：{tileData.currentStageIndex}");
                    continue;
                }
                var mxUnwateredDays = stageData.MaxUnwateredDays;
                var willDie = stageData.willDie;
                if (willDie && tileData.consecutiveUnwateredDays > mxUnwateredDays) {
                    tileData.isDead = true;
                }
            }
            if (tileData.isWatered == false) continue;
            tileData.isWatered = false;
            tileData.tileMode = TileMode.Dry;
            if (tileData.worldObject != null) {
                UpdateGridVisual(tileData.worldObject);
            }
        }

        // 存档
        DataChange?.Invoke();
    }

    public void UpdateGridVisual(GameObject gridObj) {
        if (gridObj == null) {
            Debug.LogWarning("UpdateGridVisual: gridObj 为空，跳过更新");
            return;
        }

        var tile = gridObj.GetComponent<Tile>();
        if (tile == null || tile.data == null) {
            Debug.LogWarning("UpdateGridVisual: Tile 或 tile.data 为空，跳过更新");
            return;
        }

        if (tile.data.tileMode == TileMode.Watered) {
            gridObj.transform.localScale = Vector3.one;
            gridObj.GetComponent<Renderer>().material.color = Color.blue;
        } else {
            gridObj.transform.localScale = Vector3.one;
            gridObj.GetComponent<Renderer>().material.color = Color.white;
        }
    }

    private void OnTileHover(int x, int y) {
        //// 恢复上一个Hover的地块状态
        //if (_lastHoverTile != -1 * Vector2Int.one) {
        //    if (_gridDataDict.TryGetValue(_lastHoverTile, out TileData lastTileData)) {
        //        if (lastTileData.worldObject != null) {
        //            UpdateGridVisual(lastTileData.worldObject);
        //        }
        //    }
        //}

        //if (x == -1 || y == -1) {
        //    _lastHoverTile = Vector2Int.one * -1;
        //    return; // 没有 Hover 任何地块
        //}

        //_lastHoverTile = new Vector2Int(x, y);
        //// 设置当前 Hover 地块状态
        //if (_gridDataDict.TryGetValue(_lastHoverTile, out TileData currentTileData)) {
        //    if (currentTileData.worldObject != null) {
        //        currentTileData.worldObject.transform.localScale = Vector3.one * 1.15f;
        //        currentTileData.worldObject.GetComponent<Renderer>().material.color = Color.gray;
        //    }
        //}
    }

    /// <summary>
    /// 生成网格，优先读取存档数据，如果没有存档则创建新的数据
    /// </summary>
    public void GenerateGrid() {
        _gridData.Clear();
        _gridDataDict.Clear();
        // 先清空子物体
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // 从 SaveManager 获取当前存档数据（而不是重新读取文件）
        var saveDatas = SaveManager.instance.GetCurrentSaveData();
        bool hasSaveData = saveDatas != null && saveDatas.tileData != null && saveDatas.tileData.Count > 0;

        // 这里前提是背包系统已经初始化完毕，所以可能要添加一个初始化顺序的控制
        string farmUpgradeId = BagDatabase.Instance.farmUpgrade?.id ?? "FARMUPGRADE";
        var farmUpgrade = BagDatabase.Instance.GetItemAmount(farmUpgradeId);
        gridSize = initGridSize + farmUpgrade; // 重置为基准尺寸加上升级数量，而不是在当前尺寸上累加
        Debug.Log($"[GridManager] 生成网格 - 尺寸: {gridSize}x{gridSize}, 有存档数据: {hasSaveData}");

        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                // 使用本地坐标，这样移动 GridManager 时土块会跟随
                Vector3 localPos = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, this.transform);
                tileObj.transform.localPosition = localPos;
                tileObj.name = $"Tile_{x}_{y}";

                var tile = tileObj.GetComponentInChildren<Tile>();
                TileData tileData;

                if (hasSaveData) {
                    tileData = saveDatas.tileData.FirstOrDefault(i => i.x == x && i.y == y);
                    
                    // 如果存档中没有该地块数据（升级后的新地块），则创建新的
                    if (tileData == null) {
                        tileData = new TileData(x, y, tileObj);
                    } else {
                        tileData.worldObject = tileObj;
                    }
                    
                    if (tile != null) {
                        tile.data = tileData;
                        if (!string.IsNullOrEmpty(tileData.cropId)) {
                            tile.RePlant();
                        }
                    }
                } else {
                    tileData = new TileData(x, y, tileObj);
                    if (tile != null) {
                        tile.data = tileData;
                    }
                }

                _gridData.Add(tileData);
                _gridDataDict[new Vector2Int(x, y)] = tileData;
            }
        }
    }

    /// <summary>
    /// 自动保存地块和作物数据，当状态变化时（如浇水、生长阶段变化）调用
    /// </summary>
    public void AutoSaveTileCropData() {
        // 读取之前的存档
        var saveData = SaveManager.instance.GetCurrentSaveData();
        if (saveData == null) {
            saveData = new SaveData();
        }

        // 更新地块数据
        saveData.tileData = _gridData;

        // 存档
        SaveManager.instance.AutoSave();
    }

    private float _lastUpgradeTime = 0f; // 记录上次升级时间
    private const float UPGRADE_COOLDOWN = 0.5f; // 升级冷却时间，防止重复触发

    public void OnFramUpgrade() {
        // 检查是否在冷却时间内，防止短时间内重复触发
        if (Time.time - _lastUpgradeTime < UPGRADE_COOLDOWN) {
            return;
        }

        _lastUpgradeTime = Time.time;
        GenerateGrid();
    }
}
