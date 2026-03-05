using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour {
    public static GameUI instance;

    [Header("时间显示文本")]
    [SerializeField] private TMPro.TextMeshProUGUI timeText;

    [Header("种子选择UI控件")]
    public GameObject SeedSelectCtrl;
    public UISeedSelectCtrl UISeedSelectCtrl;

    [Header("商店UI")]
    [SerializeField] private GameObject ShopCanvas;
    public UIShopCtrl UIShopCtrl; // 供其他UI控件使用

    [Header("背包UI")]
    [SerializeField] private GameObject BagCanvas;
    public UIBagCtrl UIBagCtrl; // 供其他UI控件使用

    [Header("存档时显示UI")]
    [SerializeField] private GameObject SaveStageCanvas;

    [Header("开始菜单UI")]
    [SerializeField] private GameObject StartMenuCanvas;
    [SerializeField] private UnityEngine.UI.Button newGameButton;
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [SerializeField] private GameObject PlayerGuidanceUI;
    [SerializeField] private GameObject AchievementListUI;
    [SerializeField] private GameObject ConfirmNewGameUI;

    [Header("暂停菜单UI")]
    [SerializeField] private GameObject PauseMenuCanvas;

    [Header("成就列表配置")]
    [SerializeField] private AchievementItemUI achievementItemPrefab;
    [SerializeField] private Transform achievementListContainer;

    [Header("工具等级选择")]
    [SerializeField] private TMPro.TMP_Dropdown ToolLevelChooseButton;
    private List<int> _dropdownLevelMap = new List<int>();

    [Header("工具等级按钮")]
    [SerializeField] private UnityEngine.UI.Button toolLevel1Button;
    [SerializeField] private UnityEngine.UI.Button toolLevel2Button;
    [SerializeField] private UnityEngine.UI.Button toolLevel3Button;
    private Color TOOL_BUTTON_SELECT_COLOR = new Color(0.7f, 1f, 0.7f);
    private Color TOOL_BUTTON_NORMAL_COLOR = Color.white;
    private Color TOOL_BUTTON_DISABLED_COLOR = new Color(0.5f, 0.5f, 0.5f);

    private void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this.gameObject);
        }
    }

    private void Start() {
        // 订阅状态改变
        GameStateManager.OnStateChanged += OnGameStateChanged;

        // 订阅背包物品变化事件
        if (BagDatabase.Instance != null) {
            BagDatabase.Instance.BagItemsChange.AddListener(OnBagItemsChanged);
        }

        SeedSelectCtrl.SetActive(false);
        BagCanvas.SetActive(true); // 确保背包在启动时初始化

        // 初始化工具等级下拉框
        UpdateToolLevelDropdown();

        // 初始化工具等级按钮
        InitializeToolLevelButtons();

        // 初始同步状态
        if (GameStateManager.Instance != null) {
            OnGameStateChanged(GameStateManager.Instance.CurrentState);
        }
    }

    private void OnDestroy() {
        // 取消订阅
        GameStateManager.OnStateChanged -= OnGameStateChanged;

        // 取消订阅，防止内存泄漏
        if (BagDatabase.Instance != null) {
            BagDatabase.Instance.BagItemsChange.RemoveListener(OnBagItemsChanged);
        }
    }

    private void Update() {
        UpdateTimeText();

        // 处理暂停菜单输入（只在Playing状态下响应）
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Playing) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                TogglePauseMenu();
            }
        }

        // 显示或隐藏种子选择UI
        var InteractionMgr = InteractionManager.Instance;
        if (InteractionMgr != null) {
            SeedSelectCtrl.SetActive(InteractionMgr.currInteractionMode == InteractionMode.Planting);
        }
    }

    /// <summary>
    /// 当背包物品发生变化时，更新所有相关的UI
    /// </summary>
    private void OnBagItemsChanged() {
        if (UISeedSelectCtrl != null) {
            UISeedSelectCtrl.InitSeedList();
        }
        if (UIBagCtrl != null) {
            UIBagCtrl.InitialItems();
        }
        // 如果商店是打开的，也刷新出售面板
        if (UIShopCtrl != null && UIShopCtrl.gameObject.activeInHierarchy) {
            UIShopCtrl.InitShellPanelItemList();
        }

        // 刷新工具等级选择
        UpdateToolLevelDropdown();

        // 刷新工具等级按钮状态
        UpdateToolLevelButtonsAvailability();
    }

    private void UpdateTimeText() {
        var wt = GameTimeManager.Instance.currentTime;
        var saveData = SaveManager.instance.GetCurrentSaveData();
        int displayDay = wt.gameDay;

        // 如果有存档数据，计算相对于存档开始的天数
        if (saveData != null && saveData.startGameDay > 0) {
            displayDay = wt.gameDay - saveData.startGameDay;
        }

        // 构造显示字符串：第X天 | HH:mm | 日/夜
        string dayStr = $"第{displayDay + 1}天";
        string timeStr = wt.GetCSTTimeString();
        string periodStr = wt.isNight ? "夜" : "日";
        string debugStr = wt.isDebugMode ? $" [DEBUG x{wt.timeScale:F0}]" : "";

        timeText.text = $"{dayStr} | {timeStr} | {periodStr}{debugStr}";
    }

    public void OnShopOpenButtonClick() {
        if (ShopCanvas != null) {
            ShopCanvas.SetActive(true);
        }
    }

    public void OnBagOpenButtonClick() {
        if (BagCanvas != null) {
            BagCanvas.SetActive(true);
        }
    }

    public void OnShopCloseButtonClick() {
        if (ShopCanvas != null) {
            ShopCanvas.SetActive(false);
        }
    }

    public void OnBagCloaseButtonClick() {
        if (BagCanvas != null) {
            BagCanvas.SetActive(false);
        }
    }

    public void ToggleSaveStageUIActive(bool active) {
        SaveStageCanvas.SetActive(active);
    }

    #region 额外UI显示隐藏
    public void OpenPlayerGuidance() {
        if (PlayerGuidanceUI != null) PlayerGuidanceUI.SetActive(true);
    }

    public void ClosePlayerGuidance() {
        if (PlayerGuidanceUI != null) PlayerGuidanceUI.SetActive(false);
    }

    public void OpenAchievementList() {
        if (AchievementListUI != null) {
            AchievementListUI.SetActive(true);
            RefreshAchievementList();
        }
    }

    public void CloseAchievementList() {
        if (AchievementListUI != null) AchievementListUI.SetActive(false);
    }

    /// <summary>
    /// 刷新成就列表显示
    /// </summary>
    public void RefreshAchievementList() {
        if (achievementListContainer == null || achievementItemPrefab == null) return;

        // 清空现有项
        foreach (Transform child in achievementListContainer) {
            Destroy(child.gameObject);
        }

        // 获取成就数据
        if (AchievementManager.Instance == null || AchievementManager.Instance.config == null) return;

        var achievements = AchievementManager.Instance.config.achievements;
        foreach (var ach in achievements) {
            AchievementItemUI item = Instantiate(achievementItemPrefab, achievementListContainer);
            bool isUnlocked = AchievementManager.Instance.IsUnlocked(ach.id);
            item.Setup(ach, isUnlocked);
        }
    }
    #endregion

    private void OnGameStateChanged(GameState newState) {
        if (StartMenuCanvas != null) {
            bool isStartMenu = newState == GameState.StartMenu;
            StartMenuCanvas.SetActive(isStartMenu);

            // 如果是回到开始菜单，检测存档状态
            if (isStartMenu && continueButton != null) {
                bool hasSave = SaveManager.instance != null && SaveManager.instance.HasSaveFile();
                continueButton.interactable = hasSave;
            }
        }

        // 暂停菜单只在Paused状态显示
        if (PauseMenuCanvas != null) {
            PauseMenuCanvas.SetActive(newState == GameState.Paused);
        }
    }

    public void OnNewGameButtonClick() {
        // 检查是否已经有存档
        bool hasSave = SaveManager.instance != null && SaveManager.instance.HasSaveFile();
        
        if (hasSave) {
            // 如果有存档，显示确认界面
            if (ConfirmNewGameUI != null) {
                ConfirmNewGameUI.SetActive(true);
            }
        } else {
            // 如果没有存档，直接新建游戏
            ConfirmNewGame();
        }
    }

    public void OnContinueButtonClick() {
        // Continue 模式下，确保数据是最新的
        if (SaveManager.instance != null) {
            SaveManager.instance.InitializeSaveData();
        }
        if (BagDatabase.Instance != null) {
            BagDatabase.Instance.AutoLoadBagData();
        }
        if (GridManager.Instance != null) {
            GridManager.Instance.GenerateGrid();
        }

        if (GameStateManager.Instance != null) {
            GameStateManager.Instance.StartGame();
        }
    }

    public void OnStartGameButtonClick() {
        if (GameStateManager.Instance != null) {
            GameStateManager.Instance.StartGame();
        }
    }

    #region 新建存档确认逻辑

    /// <summary>
    /// 取消新建存档，关闭确认界面
    /// </summary>
    public void OnCancelNewGameButtonClick() {
        if (ConfirmNewGameUI != null) {
            ConfirmNewGameUI.SetActive(false);
        }
    }

    /// <summary>
    /// 确认新建存档，执行新建游戏逻辑
    /// </summary>
    public void OnConfirmNewGameButtonClick() {
        // 关闭确认界面
        if (ConfirmNewGameUI != null) {
            ConfirmNewGameUI.SetActive(false);
        }

        // 执行新建游戏
        ConfirmNewGame();
    }

    /// <summary>
    /// 核心新建游戏逻辑
    /// </summary>
    private void ConfirmNewGame() {
        if (SaveManager.instance != null) {
            SaveManager.instance.NewGame();
        }

        // 强制重新初始化各个系统的数据
        if (BagDatabase.Instance != null) {
            BagDatabase.Instance.AutoLoadBagData();
        }
        if (GridManager.Instance != null) {
            GridManager.Instance.GenerateGrid();
        }

        if (GameStateManager.Instance != null) {
            GameStateManager.Instance.StartGame();
        }
    }

    #endregion

    #region 暂停菜单逻辑

    /// <summary>
    /// 切换暂停菜单显示/隐藏
    /// </summary>
    private void TogglePauseMenu() {
        if (GameStateManager.Instance == null) return;

        if (GameStateManager.Instance.CurrentState == GameState.Playing) {
            // 从游戏中进入暂停
            GameStateManager.Instance.ChangeState(GameState.Paused);
        } else if (GameStateManager.Instance.CurrentState == GameState.Paused) {
            // 从暂停返回游戏
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// 继续游戏（关闭暂停菜单）
    /// </summary>
    public void OnResumeGameButtonClick() {
        if (GameStateManager.Instance != null) {
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// 返回主菜单（从暂停菜单）
    /// </summary>
    public void OnReturnToMainMenuButtonClick() {
        if (GameStateManager.Instance != null) {
            GameStateManager.Instance.ChangeState(GameState.StartMenu);
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void OnQuitGameButtonClick() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region 工具等级选择逻辑

    /// <summary>
    /// 根据背包中的升级道具动态更新下拉框选项
    /// </summary>
    public void UpdateToolLevelDropdown() {
        if (ToolLevelChooseButton == null) return;

        // 记录当前选择的等级，以便刷新后恢复（如果依然可用）
        int currentLevel = 0;
        if (_dropdownLevelMap.Count > ToolLevelChooseButton.value && ToolLevelChooseButton.value >= 0) {
            currentLevel = _dropdownLevelMap[ToolLevelChooseButton.value];
        }

        ToolLevelChooseButton.ClearOptions();
        _dropdownLevelMap.Clear();

        List<TMPro.TMP_Dropdown.OptionData> options = new List<TMPro.TMP_Dropdown.OptionData>();

        // 等级 1: 始终显示
        options.Add(new TMPro.TMP_Dropdown.OptionData("等级 1"));
        _dropdownLevelMap.Add(0);

        // 等级 2: 需要线条升级道具
        string lineUpgradeId = BagDatabase.Instance.toolUpgradeLine?.id ?? "TOOL_UPGRADE_LINE";
        if (BagDatabase.Instance.GetItemAmount(lineUpgradeId) > 0) {
            options.Add(new TMPro.TMP_Dropdown.OptionData("等级 2"));
            _dropdownLevelMap.Add(1);
        }

        // 等级 3: 需要方块升级道具
        string squareUpgradeId = BagDatabase.Instance.toolUpgradeSquare?.id ?? "TOOL_UPGRADE_SQUARE";
        if (BagDatabase.Instance.GetItemAmount(squareUpgradeId) > 0) {
            options.Add(new TMPro.TMP_Dropdown.OptionData("等级 3"));
            _dropdownLevelMap.Add(2);
        }

        ToolLevelChooseButton.AddOptions(options);

        // 尝试恢复之前的选择
        int newIndex = _dropdownLevelMap.IndexOf(currentLevel);
        if (newIndex != -1) {
            ToolLevelChooseButton.SetValueWithoutNotify(newIndex);
        } else {
            // 如果之前的等级不再可用，重置为等级 1
            ToolLevelChooseButton.SetValueWithoutNotify(0);
            if (InteractionManager.Instance != null) {
                InteractionManager.Instance.OnToolLevelChanged(0);
            }
        }

        ToolLevelChooseButton.RefreshShownValue();
    }

    /// <summary>
    /// 供 Dropdown 的 On Value Changed (Int32) 绑定
    /// </summary>
    /// <param name="uiIndex">UI中的索引</param>
    public void OnToolLevelDropdownChanged(int uiIndex) {
        if (InteractionManager.Instance == null) return;

        if (uiIndex >= 0 && uiIndex < _dropdownLevelMap.Count) {
            int targetLevel = _dropdownLevelMap[uiIndex];
            InteractionManager.Instance.OnToolLevelChanged(targetLevel);
        }
    }

    #endregion

    #region 工具等级按钮逻辑

    /// <summary>
    /// 初始化工具等级按钮状态
    /// </summary>
    private void InitializeToolLevelButtons() {
        // 初始化默认为等级 1
        UpdateToolLevelButtonsVisual(0);
        UpdateToolLevelButtonsAvailability();
    }

    /// <summary>
    /// 更新工具等级按钮的可用性（根据背包中的升级道具）
    /// </summary>
    private void UpdateToolLevelButtonsAvailability() {
        if (toolLevel1Button == null || toolLevel2Button == null || toolLevel3Button == null) return;

        // 等级 1 始终可用
        toolLevel1Button.interactable = true;

        // 等级 2 需要线条升级道具
        string lineUpgradeId = BagDatabase.Instance.toolUpgradeLine?.id ?? "TOOL_UPGRADE_LINE";
        bool hasLine = BagDatabase.Instance.GetItemAmount(lineUpgradeId) > 0;
        toolLevel2Button.interactable = hasLine;
        if (!hasLine) {
            toolLevel2Button.image.color = TOOL_BUTTON_DISABLED_COLOR;
        }

        // 等级 3 需要方块升级道具
        string squareUpgradeId = BagDatabase.Instance.toolUpgradeSquare?.id ?? "TOOL_UPGRADE_SQUARE";
        bool hasSquare = BagDatabase.Instance.GetItemAmount(squareUpgradeId) > 0;
        toolLevel3Button.interactable = hasSquare;
        if (!hasSquare) {
            toolLevel3Button.image.color = TOOL_BUTTON_DISABLED_COLOR;
        }
    }

    /// <summary>
    /// 更新工具等级按钮的视觉状态（选中/未选中）
    /// 由 InteractionManager 调用
    /// </summary>
    /// <param name="selectedLevel">当前选中的等级: 0, 1, 2</param>
    public void UpdateToolLevelButtonsVisual(int selectedLevel) {
        if (toolLevel1Button == null || toolLevel2Button == null || toolLevel3Button == null) return;

        // 等级 1
        if (selectedLevel == 0) {
            toolLevel1Button.image.color = TOOL_BUTTON_SELECT_COLOR;
        } else {
            toolLevel1Button.image.color = TOOL_BUTTON_NORMAL_COLOR;
        }

        // 等级 2
        if (selectedLevel == 1) {
            toolLevel2Button.image.color = TOOL_BUTTON_SELECT_COLOR;
        } else {
            string lineUpgradeId = BagDatabase.Instance.toolUpgradeLine?.id ?? "TOOL_UPGRADE_LINE";
            bool hasLine = BagDatabase.Instance.GetItemAmount(lineUpgradeId) > 0;
            toolLevel2Button.image.color = hasLine ? TOOL_BUTTON_NORMAL_COLOR : TOOL_BUTTON_DISABLED_COLOR;
        }

        // 等级 3
        if (selectedLevel == 2) {
            toolLevel3Button.image.color = TOOL_BUTTON_SELECT_COLOR;
        } else {
            string squareUpgradeId = BagDatabase.Instance.toolUpgradeSquare?.id ?? "TOOL_UPGRADE_SQUARE";
            bool hasSquare = BagDatabase.Instance.GetItemAmount(squareUpgradeId) > 0;
            toolLevel3Button.image.color = hasSquare ? TOOL_BUTTON_NORMAL_COLOR : TOOL_BUTTON_DISABLED_COLOR;
        }
    }

    #endregion
}
