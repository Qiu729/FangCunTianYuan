using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    // UI显示
    [SerializeField] private Image Icon;
    [SerializeField] private TextMeshProUGUI Amount; // 外部可直接访问，但数据应由itemData驱动
    
    [Header("缩放设置")]
    [SerializeField] private float normalScale = 1.7f;
    [SerializeField] private float draggingScale = 1f;

    // 数据
    public BagObject itemData;

    private Transform originalParent;

    public void Setup(BagObject bagObject) {
        var data = ItemRegistry.Get(bagObject.id);
        Icon.sprite = data.icon;
        Amount.text = bagObject.GetQuantity().ToString();
        itemData = bagObject;
        // 使用属性设置初始缩放
        transform.localScale = Vector3.one * normalScale;
    }

    public void UpdateVisual() {
        if (itemData.GetQuantity() <= 0) {
            Destroy(this.gameObject);
        } else {
            Amount.text = itemData.GetQuantity().ToString();
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        var DragCanvas = GameUI.instance.UIBagCtrl.DargCanvas;
        originalParent = transform.parent;
        // 使用拖拽缩放属性
        transform.localScale = Vector3.one * draggingScale;
        // 移动到最上层canvas
        if (DragCanvas != null) {
            transform.SetParent(DragCanvas.transform, false);
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData) {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData) {
        // 恢复视觉效果，回到初始缩放属性值
        transform.localScale = Vector3.one * normalScale;

        // 使用事件系统检测鼠标下的UI元素
        Slot originalSlot = originalParent.GetComponent<Slot>();
        Slot targetSlot = null;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results) {
            // 检查是否放置在Slot上
            if (result.gameObject.TryGetComponent<Slot>(out Slot slot)) {
                targetSlot = slot;
                break;
            }
        }

        // 如果没有放置在有效的Slot上，或者放置在原Slot上，则返回原位
        if (targetSlot == null || targetSlot == originalSlot) {
            transform.SetParent(originalParent);
        } else {
            // 如果目标Slot是空的
            if (targetSlot.IsEmpty) {
                transform.SetParent(targetSlot.transform);
                // IsEmpty 状态会自动更新，无需手动设置
            } else { 
                // 如果目标Slot有物品，则交换。
                // 修复 bug：应查找 DraggableItem 组件而非直接获取第0个子对象（第0个子对象通常是背景图）
                DraggableItem otherItem = targetSlot.GetComponentInChildren<DraggableItem>();
                if (otherItem != null) {
                    otherItem.transform.SetParent(originalParent);
                    otherItem.transform.localPosition = Vector3.zero;
                }

                transform.SetParent(targetSlot.transform);
            }
            
            // 成功移动或交换后，同步背包数据
            GameUI.instance.UIBagCtrl.SyncBagData();
        }

        transform.localPosition = Vector3.zero; // 回到中心位置
    }
}
