using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyItemGrid : MonoBehaviour {
    [Header("UI子控件")]
    [SerializeField] private Image Icon;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI Price;
    [SerializeField] private Button BuyButton;

    // 内部数据
    private BagObjectData goods;

    public void Setup(BagObjectData data) {
        goods = data;
        Icon.sprite = data.icon;
        Name.text = data.objectName;
        Price.text = data.buyPrice.ToString();
    }

    public void OnBuyButtonClick() {
        // 获取价格
        var price = goods.buyPrice;
        var playerCoins = BagDatabase.Instance.Coins;

        // 检查玩家金币是否足够
        if (playerCoins < price) {
            Debug.Log("玩家金币不足，无法购买");
            return;
        }

        // 扣除金币并添加物品
        BagDatabase.Instance.Coins -= price;
        BagDatabase.Instance.AddItem(goods.id, 1);

        // 如果是限购商品，记录购买次数
        if (goods.isPurchaseLimited) {
            BagDatabase.Instance.AddPurchaseCount(goods.id, 1);
        }

        // 上报升级类成就进度（根据物品ID判断，不依赖类型）
        ReportUpgradeAchievement(goods.id);

        // 更新UI
        var UIShopCtrl = GameUI.instance.UIShopCtrl;
        UIShopCtrl.SetPlayerCoins(BagDatabase.Instance.Coins);

        // 如果达到限购上限，刷新商店面板以隐藏该物品
        if (goods.isPurchaseLimited && BagDatabase.Instance.GetPurchaseCount(goods.id) >= goods.purchaseLimitAmount) {
            UIShopCtrl.RefreshBuyPanel();
        }

        var UIBagCtrl = GameUI.instance.UIBagCtrl;
        UIBagCtrl.InitialItems();

        var UISeedSelectCtrl = GameUI.instance.UISeedSelectCtrl;
        UISeedSelectCtrl.InitSeedList();
    }

    /// <summary>
    /// 上报升级类成就进度（根据物品ID直接判断，不依赖类型）
    /// </summary>
    private void ReportUpgradeAchievement(string itemId) {
        if (AchievementManager.Instance == null) return;

        var db = BagDatabase.Instance;
        
        Debug.Log($"[BuyItemGrid] 购买物品 - ID: {itemId}");
        Debug.Log($"[BuyItemGrid] 工具升级线条ID: {db.toolUpgradeLine?.id ?? "未配置"}");
        Debug.Log($"[BuyItemGrid] 工具升级方块ID: {db.toolUpgradeSquare?.id ?? "未配置"}");
        Debug.Log($"[BuyItemGrid] 农场升级ID: {db.farmUpgrade?.id ?? "未配置"}");

        // 工具升级成就
        if (itemId == (db.toolUpgradeLine?.id ?? "TOOL_UPGRADE_LINE")) {
            Debug.Log($"[BuyItemGrid] 上报成就进度: LINE_UPGRADE");
            AchievementManager.Instance.ReportProgress("LINE_UPGRADE", 1);
        } else if (itemId == (db.toolUpgradeSquare?.id ?? "TOOL_UPGRADE_SQUARE")) {
            Debug.Log($"[BuyItemGrid] 上报成就进度: SQUARE_UPGRADE");
            AchievementManager.Instance.ReportProgress("SQUARE_UPGRADE", 1);
        }
        // 农场升级成就
        else if (itemId == (db.farmUpgrade?.id ?? "FARMUPGRADE")) {
            Debug.Log($"[BuyItemGrid] 上报成就进度: FARM_UPGRADE1 和 FARM_UPGRADE2");
            AchievementManager.Instance.ReportProgress("FARM_UPGRADE1", 1);
            AchievementManager.Instance.ReportProgress("FARM_UPGRADE2", 1);
        }
    }
}
