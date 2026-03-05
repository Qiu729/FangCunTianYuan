using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIBagCtrl : MonoBehaviour {
    [Header("背包大小")]
    public int bagSize;
    public GameObject SlotPrefab;
    public GameObject DragItemPrefab;

    [Header("UI父控件")]
    public Transform SlotContainer;
    public Canvas DargCanvas;

    private void Start() {
        GenerateSlots();
        InitialItems();

        // 初始化之后先隐藏，避免无法拾取物品
        gameObject.SetActive(false);
    }

    public void UpdateBagItemsVisual() {
        foreach (Transform slot in SlotContainer.transform) {
            var item = slot.GetComponentInChildren<DraggableItem>();
            if (item != null && item.itemData != null) {
                item.UpdateVisual();
            }
        }
    }

    private void GenerateSlots() {
        for (int i = 0; i < bagSize; i++) {
            var slot = Instantiate(SlotPrefab, SlotContainer);
        }
    }

    public void InitialItems() {
        // 清理旧的物品，使用 GetComponentsInChildren 确保清理所有潜在的克隆体
        foreach (Transform slot in SlotContainer) {
            var items = slot.GetComponentsInChildren<DraggableItem>();
            foreach (var item in items) {
                Destroy(item.gameObject);
            }
        }

        // 清理正在拖拽层中的物品，防止 InitialItems 在拖拽过程中被调用时产生克隆
        if (DargCanvas != null) {
            var dragItems = DargCanvas.GetComponentsInChildren<DraggableItem>();
            foreach (var item in dragItems) {
                Destroy(item.gameObject);
            }
        }

        // 生成新的物品
        var itemList = BagDatabase.Instance.items;
        for (int i = 0; i < itemList.Count; i++) {
            var itemData = itemList[i];
            var slot = SlotContainer.GetChild(i);
            var itemObj = Instantiate(DragItemPrefab, slot.transform);
            itemObj.GetComponent<DraggableItem>().Setup(itemData);
        }
    }

    /// <summary>
    /// 根据当前UI中槽位的物品顺序同步 BagDatabase 中的数据列表
    /// </summary>
    public void SyncBagData() {
        List<BagObject> newItems = new List<BagObject>();
        foreach (Transform slot in SlotContainer) {
            var item = slot.GetComponentInChildren<DraggableItem>();
            if (item != null && item.itemData != null) {
                newItems.Add(item.itemData);
            }
        }
        BagDatabase.Instance.items = newItems;
        // 同步数据后自动存档
        BagDatabase.Instance.AutoSaveBagData();
    }

    /// <summary>
    /// 向背包UI中添加物品，如果成功则返回true，否则返回false
    /// </summary>
    /// <param name="itemData">要添加的物品数据</param>
    /// <param name="addAmount">要添加的数量</param>
    /// <returns></returns>
    public bool AddItem(BagObjectData itemData, int addAmount) {
        // 优先尝试堆叠
        for (int i = 0; i < SlotContainer.childCount; i++) {
            var slot = SlotContainer.GetChild(i).GetComponent<Slot>();
            if (!slot.IsEmpty) {
                var item = slot.GetComponentInChildren<DraggableItem>();
                // 添加空引用检查
                if (item == null || item.itemData == null) {
                    Debug.LogWarning($"槽位 {i} 不为空但没有有效的 DraggableItem 或 itemData");
                    continue;
                }
                
                if (item.itemData.id == itemData.id) {
                    var data = ItemRegistry.Get(item.itemData.id);
                    int currentAmount = item.itemData.GetQuantity();
                    int maxStack = data.maxStack;

                    if (currentAmount < maxStack) {
                        int spaceLeft = maxStack - currentAmount;
                        int amountToAdd = Mathf.Min(addAmount, spaceLeft);

                        item.itemData.SetQuantity(currentAmount + amountToAdd);
                        addAmount -= amountToAdd;

                        if (addAmount <= 0) {
                            return true; // 所有物品都已添加
                        }
                    }
                }
            }
        }

        // 如果还有剩余物品，则寻找空槽位
        if (addAmount > 0) {
            for (int i = 0; i < SlotContainer.childCount; i++) {
                var slot = SlotContainer.GetChild(i).GetComponent<Slot>();
                if (slot.IsEmpty) {
                    var itemObj = Instantiate(DragItemPrefab, slot.transform);
                    itemObj.GetComponent<DraggableItem>().Setup(new BagObject(itemData.id, addAmount));
                    return true; // 物品已放入新槽位
                }
            }
        }

        return false; // 背包已满
    }
}
