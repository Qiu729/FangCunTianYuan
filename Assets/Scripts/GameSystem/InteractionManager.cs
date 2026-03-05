using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

public enum InteractionMode {
    None,
    Planting,
    Watering,
    Harvesting,
    UpRooting
}

public class InteractionManager : MonoBehaviour {
    public static InteractionManager Instance;

    [Header("层级")]
    public LayerMask groundLayer; // 设置为只检测 Ground 层
    public GridManager gridManager;

    [Header("幽灵提示")]
    public GameObject ghostPrefab; // 半透明的幽灵Prefab
    private List<GameObject> _ghostPool = new List<GameObject>();
    private int _activeGhostCount = 0;

    [Header("交互模式")]
    public Button PlantButton;
    public Button WaterButton;
    public Button HarvestButton;
    public Button UpRootButton;
    public InteractionMode currInteractionMode;
    private Color BUTTON_SELECT_COLOR = new Color(0.7f, 1f, 0.7f);
    private Color BUTTON_NORMAL_COLOR = Color.white;

    // 当前选择的种子ID (由UI设置)  public的
    public string selectedSeedId = "";
    public BagObject selectedBagObject;

    // 当数据依赖发生改变时，比如也需要一个事件来通知自动存档
    [Header("存档事件")]
    public UnityEvent DataChange;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this.gameObject);
        }
    }

    void Update() {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            HideGhost();
            return;
        }

        if (currInteractionMode == InteractionMode.None) {
            return;
        }

        HandleInteraction();
    }

    private void HandleInteraction() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f)) {

            //Debug.Log($"射线检测到物体: {hit.collider.name}");

            var tile = hit.collider.GetComponentInChildren<Tile>();

            //Debug.Log($"选择土壤{tile.name}");

            if (tile == null) {
                HideGhost();
                return;
            }

            switch (currInteractionMode) {
                case InteractionMode.Planting:
                    HandlePlantModeLogic(tile);
                    break;
                case InteractionMode.Watering:
                    HandleWaterModeLogic(tile);
                    break;
                case InteractionMode.Harvesting:
                    HandleHarvestModeLogic(tile);
                    break;
                case InteractionMode.UpRooting:
                    HandleUpRootModeLogic(tile);
                    break;
            }
        } else {
            HideGhost();
        }
    }

    // 处理种植模式的交互逻辑
    private void HandlePlantModeLogic(Tile tile) {
        if (!string.IsNullOrEmpty(selectedSeedId)) {
            var tiles = GetTilesInPattern(tile);
            ShowGhost(tiles);

            if (Input.GetMouseButtonDown(0)) {
                PlantCrop(tile, selectedSeedId);
            }
        } else {
            HideGhost();
        }
    }

    // 处理浇水模式的交互逻辑
    private void HandleWaterModeLogic(Tile tile) {
        var tiles = GetTilesInPattern(tile);
        ShowGhost(tiles);

        if (Input.GetMouseButtonDown(0)) {
            Watering(tile);
        }
    }

    // 处理收获模式的交互逻辑
    private void HandleHarvestModeLogic(Tile tile) {
        var tiles = GetTilesInPattern(tile);
        ShowGhost(tiles);

        if (Input.GetMouseButtonDown(0)) {
            bool harvestedAny = false;
            foreach (var t in tiles) {
                if (t.data.harvestAble) {
                    t.Harvest();
                    harvestedAny = true;
                }
            }
            if (harvestedAny) DataChange?.Invoke();
        }
    }

    // 处理铲除模式的交互逻辑
    private void HandleUpRootModeLogic(Tile tile) {
        var tiles = GetTilesInPattern(tile);
        ShowGhost(tiles);

        if (Input.GetMouseButtonDown(0)) {
            bool upRootedAny = false;
            foreach (var t in tiles) {
                if (!t.data.IsEmpty) {
                    t.UpRoot();
                    upRootedAny = true;
                }
            }
            if (upRootedAny) DataChange?.Invoke();
        }
    }


    // 给土地浇水
    private void Watering(Tile tile) {
        var tiles = GetTilesInPattern(tile);
        foreach (var t in tiles) {
            t.Water();
        }
        // 存档
        DataChange?.Invoke();
    }

    // 显示幽灵/预显示
    void ShowGhost(List<Tile> tiles) {
        HideGhost();

        for (int i = 0; i < tiles.Count; i++) {
            if (i >= _ghostPool.Count) {
                _ghostPool.Add(Instantiate(ghostPrefab));
            }

            GameObject ghost = _ghostPool[i];
            ghost.SetActive(true);
            // 关键点：幽灵要稍微高于地面，防止闪烁
            ghost.transform.position = tiles[i].PlantRoot.position;
        }
        _activeGhostCount = tiles.Count;
    }

    void HideGhost() {
        for (int i = 0; i < _ghostPool.Count; i++) {
            if (_ghostPool[i] != null) _ghostPool[i].SetActive(false);
        }
        _activeGhostCount = 0;
    }

    // 核心的种植逻辑
    void PlantCrop(Tile centerTile, string seedId) {
        var tiles = GetTilesInPattern(centerTile);
        bool plantedAny = false;

        foreach (var tile in tiles) {
            // 检查种子数量和地块是否为空
            if (selectedBagObject != null && selectedBagObject.GetQuantity() > 0 && tile.data.IsEmpty) {
                BagDatabase.Instance.DecreseItem(selectedBagObject.id, 1);
                tile.Plant(seedId);
                plantedAny = true;

                // 成就
                AchievementManager.Instance?.ReportProgress("PLANT_5_CROP");
            }

            if (selectedBagObject == null || selectedBagObject.GetQuantity() <= 0) {
                selectedBagObject = null;
                selectedSeedId = "";
                break;
            }
        }

        if (plantedAny) {
            // 存档
            DataChange?.Invoke();
        }
    }

    // UI调用的方法
    public void SelectSeed(string id) {
        selectedSeedId = id;
    }

    private void SetInteractionMode(InteractionMode mode) {
        // 如果当前模式和新模式相同，则切换到None模式，实现toggle效果
        if (currInteractionMode == mode) {
            currInteractionMode = InteractionMode.None;
        } else {
            currInteractionMode = mode;
        }
        UpdateButtons();
    }

    private void UpdateButtons() {
        PlantButton.image.color = (currInteractionMode == InteractionMode.Planting) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        WaterButton.image.color = (currInteractionMode == InteractionMode.Watering) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        HarvestButton.image.color = (currInteractionMode == InteractionMode.Harvesting) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        UpRootButton.image.color = (currInteractionMode == InteractionMode.UpRooting) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;

        if (currInteractionMode != InteractionMode.Planting) {
            HideGhost();
        }
    }

    /// <summary>
    /// 供Unity Button OnClick绑定使用（Inspector对enum参数支持不稳定），
    /// 因此使用int参数再转换为InteractionMode。
    /// None=0, Planting=1, Watering=2, Harvesting=3, UpRooting=4
    /// </summary>
    public void OnInteractionModeChangeButtonClick(int mode) {
        SetInteractionMode((InteractionMode)mode);
    }

    public void SetselectedSeed(string id, BagObject bagObject) {
        selectedSeedId = id;
        selectedBagObject = bagObject;
    }

    #region 工具升级与范围操作

    private int _manualToolLevel = 0; // 默认使用等级 0 (对应UI的选项0)

    /// <summary>
    /// 供UI组件（如Dropdown/选项按钮）绑定，手动选择工具作用等级。
    /// 对应 On Value Changed (Int32)
    /// </summary>
    /// <param name="level">UI传入的索引：0(单格), 1(线条), 2(方块)</param>
    public void OnToolLevelChanged(int level) {
        _manualToolLevel = level;
        Debug.Log($"[Interaction] Tool Level Switched to: {_manualToolLevel}");
    }

    /// <summary>
    /// 供按钮 OnClick 绑定，直接设置工具等级。
    /// 用于替代 Dropdown 的按钮组实现。
    /// </summary>
    /// <param name="level">目标等级：0(单格), 1(线条), 2(方块)</param>
    public void OnToolLevelButtonClick(int level) {
        _manualToolLevel = level;
        Debug.Log($"[Interaction] Tool Level Button Clicked: {_manualToolLevel}");
        
        // 通知 GameUI 更新按钮视觉状态
        if (GameUI.instance != null) {
            GameUI.instance.UpdateToolLevelButtonsVisual(_manualToolLevel);
        }
    }

    private int GetToolLevel() {
        // Shift 键保留作为强制单格的操作习惯
        if (Input.GetKey(KeyCode.LeftShift)) return 0;

        // 直接返回玩家手动选择的等级，不再进行背包自动判定
        return _manualToolLevel;
    }

    private List<Tile> GetTilesInPattern(Tile centerTile) {
        List<Tile> tiles = new List<Tile>();
        if (centerTile == null) return tiles;

        int level = GetToolLevel();
        Vector2Int centerPos = new Vector2Int(centerTile.data.x, centerTile.data.y);
        
        // Debug.Log($"[Interaction] Tool Level: {level}, Center: {centerPos}");

        if (level == 0) {
            tiles.Add(centerTile);
        } else if (level == 1) {
            float yaw = Camera.main.transform.eulerAngles.y;
            // 判定摄像机是主要看向南北(Z)还是东西(X)
            bool lookingNorthSouth = (yaw >= 315 || yaw < 45) || (yaw >= 135 && yaw < 225);

            for (int i = -1; i <= 1; i++) {
                // 如果看向南北，线条应该横向(X轴)；如果看向东西，线条应该纵向(Y轴/Z方向)
                Vector2Int pos = lookingNorthSouth ? new Vector2Int(centerPos.x + i, centerPos.y) : new Vector2Int(centerPos.x, centerPos.y + i);
                if (GridManager.Instance._gridDataDict.TryGetValue(pos, out var data)) {
                    var tile = data.worldObject.GetComponentInChildren<Tile>(); // 修正：应为 GetComponentInChildren
                    if (tile != null) tiles.Add(tile);
                }
            }
        } else if (level == 2) {
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    Vector2Int pos = new Vector2Int(centerPos.x + x, centerPos.y + y);
                    if (GridManager.Instance._gridDataDict.TryGetValue(pos, out var data)) {
                        var tile = data.worldObject.GetComponentInChildren<Tile>(); // 修正：应为 GetComponentInChildren
                        if (tile != null) tiles.Add(tile);
                    }
                }
            }
        }

        // if (tiles.Count == 0) Debug.LogWarning("[Interaction] No tiles found in pattern!");
        return tiles;
    }

    #endregion
}
