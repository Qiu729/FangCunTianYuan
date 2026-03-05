using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ShopMode {
    Buy,
    Shell
}

public class UIShopCtrl : MonoBehaviour {
    [Header("UI子控件")]
    public Button BuyModeButton;
    public Button ShellModeButton;
    [SerializeField] private TextMeshProUGUI PlayerCoinsCount;
    public Transform ShellItemContainer;
    public Transform BuyItemContainer;

    public Button SeedTypeButton;
    public Button ToolsTypeButton;
    public Button UpgradeypeButton;
    public Button OthersTypeButton;

    public GameObject BuyPanel;
    public GameObject ShellPanel;

    [Header("预制体和配置")]
    public GameObject ShellItemGridPrefab;
    public GameObject BuyItemGridPrefab;
    public int buyItemsPerRow = 9; // 每行显示的购买物品数量

    // 内部数据
    private ShopMode currShopMode = ShopMode.Buy; // 默认商店
    private Color BUTTON_SELECT_COLOR = new Color(0.7f, 1f, 0.7f);
    private Color BUTTON_NORMAL_COLOR = Color.white;
    public int PlayerCoins {
        get => BagDatabase.Instance.Coins;
        private set => BagDatabase.Instance.Coins = value;
    }
    private GoodsType currBuyPanelType = GoodsType.Seed; // 当前购买面板的类型，默认是种子
    // 用于记录事件订阅
    private Dictionary<ShellItemGrid, (LifeCycleEvents.Destroyed destroySub, UnityAction itemSoldSub)> _gridSubscriptions =
        new Dictionary<ShellItemGrid, (LifeCycleEvents.Destroyed, UnityAction)>();


    private void Start() {
        UpdateShopModeVisual();
        UpdatePlayerCoinsVisual();
        InitShellPanelItemList();
        InitBuyPanelItemList();
        UpdateBuyTypeButtonsVisual();
    }
    
    private void OnEnable() {
        // 每次打开商店时更新金币显示
        UpdatePlayerCoinsVisual();
    }

    public void InitShellPanelItemList() {
        // 清理旧的列表项和订阅
        foreach (Transform child in ShellItemContainer) {
            Destroy(child.gameObject);
        }
        _gridSubscriptions.Clear();

        // 获取数据
        var items = BagDatabase.Instance.items;

        // 实例化可出售的物品
        foreach (var item in items) {
            if (item.GetQuantity() <= 0) continue;
            var itemData = ItemRegistry.Get(item.id);
            if (itemData.canBeSold) {
                var itemGrid = Instantiate(ShellItemGridPrefab, ShellItemContainer);
                var gridComp = itemGrid.GetComponent<ShellItemGrid>();
                gridComp.Setup(item);

                // 定义和订阅事件
                void OnDestroyed() => OnChildDestroyed(gridComp);
                gridComp.OnDestroyed += OnDestroyed;

                void OnItemSold() => UpdatePlayerCoinsVisual();
                gridComp.OnItemSold += OnItemSold;

                // 记录订阅，以便后续取消
                _gridSubscriptions[gridComp] = (OnDestroyed, OnItemSold);
            }
        }
    }

    /// <summary>
    /// 不同类型的商品在同一个panel，所以切换类型时需要清理并重新初始化
    /// </summary>
    private void InitBuyPanelItemList() {
        // 清理Container
        foreach (Transform child in BuyItemContainer) {
            Destroy(child.gameObject);
        }

        // 获取数据
        var DicitemList = ShopDatabase.instance.goodsDicList;
        var items = DicitemList[currBuyPanelType];

        // 实例化可购买的物品
        foreach (var item in items) {
            // 检查限购逻辑
            if (item.isPurchaseLimited) {
                int boughtCount = BagDatabase.Instance.GetPurchaseCount(item.id);
                if (boughtCount >= item.purchaseLimitAmount) {
                    continue; // 达到上限，不显示
                }
            }

            // 实例化Grid
            var gird = Instantiate(BuyItemGridPrefab, BuyItemContainer);
            // 设置Grid数据
            gird.GetComponent<BuyItemGrid>().Setup(item);
        }
    }

    public void RefreshBuyPanel() {
        InitBuyPanelItemList();
    }

    private void UpdatePlayerCoinsVisual() {
        PlayerCoinsCount.text = $"玩家金币数量：{PlayerCoins.ToString()}";
    }

    public void SetPlayerCoins(int amount) {
        PlayerCoins = amount;
        UpdatePlayerCoinsVisual();
    }


    /// <summary>
    /// 切换商店模式时，需要更新UI，特别是按钮的背景
    /// </summary>
    private void UpdateShopModeVisual() {
        BuyPanel.SetActive(currShopMode == ShopMode.Buy);
        ShellPanel.SetActive(currShopMode == ShopMode.Shell);

        BuyModeButton.image.color = (currShopMode == ShopMode.Buy) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        ShellModeButton.image.color = (currShopMode == ShopMode.Shell) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
    }

    private void OnChildDestroyed(ShellItemGrid destroyedGrid) {
        // 从字典获取并取消该Grid的订阅
        if (_gridSubscriptions.TryGetValue(destroyedGrid, out var subs)) {
            destroyedGrid.OnDestroyed -= subs.destroySub;
            destroyedGrid.OnItemSold -= subs.itemSoldSub;
            _gridSubscriptions.Remove(destroyedGrid);
        }
    }

    private void OnDestroy() {
        // 取消所有剩余的订阅，防止内存泄漏
        foreach (var kvp in _gridSubscriptions) {
            var grid = kvp.Key;
            var (destroySub, itemSoldSub) = kvp.Value;
            if (grid != null) {
                grid.OnDestroyed -= destroySub;
                grid.OnItemSold -= itemSoldSub;
            }
        }
        _gridSubscriptions.Clear();
    }

    #region ButtonClick
    public void OnBuyModeButtonClick() {
        currShopMode = ShopMode.Buy;
        UpdateShopModeVisual();
    }

    public void OnShellModeButtonClick() {
        currShopMode = ShopMode.Shell;
        UpdateShopModeVisual();
        InitShellPanelItemList();
    }



    private void UpdateBuyTypeButtonsVisual() {
        SeedTypeButton.image.color = (currBuyPanelType == GoodsType.Seed) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        ToolsTypeButton.image.color = (currBuyPanelType == GoodsType.Tools) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        UpgradeypeButton.image.color = (currBuyPanelType == GoodsType.Upgrade) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
        OthersTypeButton.image.color = (currBuyPanelType == GoodsType.Others) ? BUTTON_SELECT_COLOR : BUTTON_NORMAL_COLOR;
    }


    /// <summary>
    /// 切换购买面板的商品类型，在Unity按钮中绑定，seed=0,tools=1,upgrade=2,others=3
    /// </summary>
    /// <param name="typeIndex"></param>
    public void OnBuyTypeButtonClick(int typeIndex) {
        GoodsType newType = (GoodsType)typeIndex;
        if (currBuyPanelType == newType) return;

        currBuyPanelType = newType;
        InitBuyPanelItemList();
        UpdateBuyTypeButtonsVisual();
    }

    #endregion

}
