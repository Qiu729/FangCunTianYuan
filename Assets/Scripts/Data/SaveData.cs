using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// 自定义存档所需的数据结构
/// </summary>

[System.Serializable]
public class SaveData {

    // 存档时间
    public string saveTime;

    // 地块状态（包括农田作物状态）
    public List<TileData> tileData = new List<TileData>();

    // 背包库
    public int Coins;
    public List<BagObject> items = new List<BagObject>();

    // 存档系统使用的总天数
    public int gameDay;

    // 存档开始时的日期（用于显示相对天数）
    public int startGameDay;

    // 成就系统
    // 使用自定义结构支持序列化
    [SerializeField] private List<AchievementProgressEntry> _achievementProgress = new List<AchievementProgressEntry>();
    [SerializeField] private List<string> _unlockedAchievements = new List<string>();

    // 购买限制记录
    [SerializeField] private List<PurchaseCountEntry> _purchaseCounts = new List<PurchaseCountEntry>();

    // 临时缓存（运行时使用）
    private Dictionary<string, int> _achievementProgressDict;
    private HashSet<string> _unlockedAchievementsSet;
    private Dictionary<string, int> _purchaseCountsDict;

    // ====== 提供给 AchievementManager 和 BagDatabase 的接口 ======

    /// <summary>
    /// 从存档读取后使用，列表转为字典/哈希集合
    /// </summary>
    public void PrepareRuntimeData() {
        _achievementProgressDict = _achievementProgress
            .ToDictionary(entry => entry.achievementId, entry => entry.progress);

        _unlockedAchievementsSet = new HashSet<string>(_unlockedAchievements);

        _purchaseCountsDict = _purchaseCounts
            .ToDictionary(entry => entry.itemId, entry => entry.count);
    }

    /// <summary>
    /// 保存前使用，字典/哈希集合转为列表
    /// </summary>
    public void PrepareAchievementSaveData(Dictionary<string, int> progress, HashSet<string> unlocked) {
        _achievementProgress = progress
            .Select(kvp => new AchievementProgressEntry(kvp.Key, kvp.Value))
            .ToList();

        _unlockedAchievements = unlocked.ToList();
    }

    /// <summary>
    /// 保存前使用，购买记录字典转为列表
    /// </summary>
    public void PreparePurchaseSaveData(Dictionary<string, int> purchaseCounts) {
        _purchaseCounts = purchaseCounts
            .Select(kvp => new PurchaseCountEntry(kvp.Key, kvp.Value))
            .ToList();
    }

    // ====== 数据访问属性 ======
    public Dictionary<string, int> AchievementProgress => _achievementProgressDict;
    public HashSet<string> UnlockedAchievements => _unlockedAchievementsSet;
    public Dictionary<string, int> PurchaseCounts => _purchaseCountsDict;
}

// 自定义结构，需要为 Serializable
[System.Serializable]
public class AchievementProgressEntry {
    public string achievementId;
    public int progress;

    public AchievementProgressEntry(string id, int prog) {
        achievementId = id;
        progress = prog;
    }
}

[System.Serializable]
public class PurchaseCountEntry {
    public string itemId;
    public int count;

    public PurchaseCountEntry(string id, int c) {
        itemId = id;
        count = c;
    }
}