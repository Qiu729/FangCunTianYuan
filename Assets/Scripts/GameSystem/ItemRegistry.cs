using System.Collections.Generic;
using UnityEngine;

public static class ItemRegistry {
    private static readonly Dictionary<string, BagObjectData> _items = new();

    // 启动时自动初始化（最晚在第一个场景加载前完成）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize() {
        // 方式1：Resources（简单，适合小型项目）
        var items = Resources.LoadAll<BagObjectData>("Inventory");

        // 方式2：Addressables（推荐中大型项目，异步加载）
        //Addressables.LoadAssetsAsync<BagObjectData>("Inventory", OnItemLoaded);

        foreach (var item in items) {
            if (string.IsNullOrEmpty(item.id)) {
                Debug.LogError($"物品 '{item.name}' 的 id 为空！");
                continue;
            }
            if (_items.ContainsKey(item.id)) {
                Debug.LogError($"物品 ID 冲突: {item.id}（{item.name} 与 {_items[item.id].name}）");
                continue;
            }
            _items[item.id] = item;
        }

        //Debug.Log($"物品数据库加载完成：{_items.Count} 个物品");
    }

    public static BagObjectData Get(string id) => _items.GetValueOrDefault(id);
    public static bool Contains(string id) => _items.ContainsKey(id);
    public static IReadOnlyDictionary<string, BagObjectData> All => _items;
}