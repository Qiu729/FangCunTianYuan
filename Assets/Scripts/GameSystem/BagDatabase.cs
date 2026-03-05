using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

/// <summary>
/// 背包数据库
/// </summary>
[DefaultExecutionOrder(-30)] // 需要在GridManager之前完成初始化，因为GridManager要读取FarmUpgrade数据
public class BagDatabase : MonoBehaviour {
    public static BagDatabase Instance;

    [Header("升级配置")]
    public BagObjectData toolUpgradeLine;
    public BagObjectData toolUpgradeSquare;
    public BagObjectData farmUpgrade;

    public int Coins;

    // 玩家物品列表
    public List<BagObject> items = new List<BagObject>();

    // 购买次数记录 (用于限购逻辑)
    public Dictionary<string, int> purchaseCounts = new Dictionary<string, int>();

    // 事件
    public UnityEvent BagItemsChange;
    public UnityEvent FarmUpgrade;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        AutoLoadBagData();
    }

    public int GetItemAmount(string id) {
        return items.FirstOrDefault(i => i.id == id)?.GetQuantity() ?? 0;
    }

    /// <summary>
    /// 获取某物品已购买次数
    /// </summary>
    public int GetPurchaseCount(string id) {
        if (purchaseCounts.TryGetValue(id, out int count)) {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// 增加某物品购买次数记录
    /// </summary>
    public void AddPurchaseCount(string id, int amount = 1) {
        if (purchaseCounts.ContainsKey(id)) {
            purchaseCounts[id] += amount;
        } else {
            purchaseCounts[id] = amount;
        }
        AutoSaveBagData();
    }

    public void AddItem(string id, int amount) {
        // 先看是否可堆叠
        var bObj = items.FirstOrDefault(i => i.id == id);
        var bData = ItemRegistry.Get(id);
        if (bObj != null) {
            bObj.SetQuantity(amount + bObj.GetQuantity());
        } else {
            // 添加新物品
            BagObject bagObject = new BagObject(id, amount);
            items.Add(bagObject);
        }

        // 如果是升级类物品，触发升级事件
        if (bData.type == BagObjectType.Upgrade) {
            FarmUpgrade?.Invoke();
        }

        BagItemsChange?.Invoke();
        // 自动存档
        AutoSaveBagData();
    }

    public void DecreseItem(string id, int amount) {
        // 递减
        var bObj = items.FirstOrDefault(i => i.id == id);
        if (bObj != null) {
            bObj.SetQuantity(bObj.GetQuantity() - amount);
            // 判断是否需要从列表中删除（完全用完）
            if (bObj.GetQuantity() <= 0) {
                items.Remove(bObj);
            }
        }

        BagItemsChange?.Invoke();
        // 自动存档
        AutoSaveBagData();
    }

    /// <summary>
    /// 自动存档，在背包数据改变时调用
    /// </summary>
    public void AutoSaveBagData() {
        // 等待一帧是为了确保所有逻辑都在同一帧内执行完毕后再存档，
        // 避免因执行顺序问题导致数据不一致。
        StartCoroutine(ContinueAfterOneFrame());

        // 获取之前的存档
        var saveData = SaveManager.instance.GetCurrentSaveData();
        if (saveData == null) {
            saveData = new SaveData();
        }

        // 更新数据
        saveData.Coins = Coins;
        saveData.items = items;
        saveData.PreparePurchaseSaveData(purchaseCounts);

        // 存档
        SaveManager.instance.AutoSave();
    }

    public void AutoLoadBagData() {
        // 从 SaveManager 获取当前存档数据（而不是重新读取文件）
        var saveData = SaveManager.instance.GetCurrentSaveData();

        if (saveData == null) {
            Debug.Log($"[BagDatabase] 无法读取存档，使用默认值");
            Coins = 30;
            items.Clear();
            purchaseCounts.Clear();
            BagItemsChange?.Invoke();
            return;
        }

        // 更新数据
        Coins = saveData.Coins;
        items = saveData.items;
        saveData.PrepareRuntimeData();
        purchaseCounts = saveData.PurchaseCounts ?? new Dictionary<string, int>();

        // 清理数量为0的物品
        for (int i = items.Count - 1; i >= 0; i--) {
            var item = items[i];
            if (item.GetQuantity() <= 0) {
                items.RemoveAt(i);
            }
        }

        Debug.Log($"[BagDatabase] 加载存档数据 - 金币: {Coins}, 物品数: {items.Count}");
        
        // 触发事件，通知UI进行初始化刷新
        BagItemsChange?.Invoke();
    }

    IEnumerator ContinueAfterOneFrame() {
        yield return null; // 等一帧
    }
}
