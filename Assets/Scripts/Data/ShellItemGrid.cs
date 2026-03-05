using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShellItemGrid : MonoBehaviour {

    [Header("UI子控件")]
    [SerializeField] private Image Icon;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI Description;
    public Button ShellButton; // 出售时需要添加监听
    [SerializeField] private Slider ShellSlider;
    [SerializeField] private TextMeshProUGUI ShellCount;
    [SerializeField] private TextMeshProUGUI ShellPriceText;

    // 内部数据
    private int ItemCount => _item.GetQuantity(); // 当前物品数量，对应数据库
    private BagObject _item;
    private int SelectedShellCount { // Slider选择的数量，逻辑上需要用到
        get => (int)ShellSlider.value;
        set {
            // 确保值在Slider的有效范围内
            int clampedValue = Mathf.Clamp(
                value,
                (int)ShellSlider.minValue,
                (int)ShellSlider.maxValue
            );
            ShellSlider.value = clampedValue;
        }
    }
    private int ShellPrice => ItemRegistry.Get(_item.id).sellPrice;

    // 事件

    /// <summary>
    /// 通知父节点调整高度
    /// </summary>
    public event UIEvents.LayoutChanged OnLayoutChanged;

    /// <summary>
    /// 通知父节点移除事件监听，从而取消订阅
    /// </summary>
    public event LifeCycleEvents.Destroyed OnDestroyed;

    /// <summary>
    /// 当物品被卖出时触发
    /// </summary>
    public event UnityAction OnItemSold;

    public void Setup(BagObject item) {
        _item = item;
        var itemData = ItemRegistry.Get(item.id);
        Icon.sprite = itemData.icon;
        Name.text = itemData.objectName;
        // Description还没做
        ShellPriceText.text = $"售价{ShellPrice}元";
        ShellSlider.minValue = 1;
        ShellSlider.maxValue = ItemCount;
        OnShellSliderValueChange();
    }

    public void OnShellSliderValueChange() {
        // 更新文本显示
        SelectedShellCount = (int)ShellSlider.value;
        ShellCount.text = $"{SelectedShellCount}/{ItemCount}";
    }

    public void OnShellButtonClick() {
        // 更新数据库
        BagDatabase.Instance.DecreseItem(_item.id, SelectedShellCount);
        BagDatabase.Instance.Coins += SelectedShellCount * ShellPrice;

        // 触发事件，通知UI更新
        OnItemSold?.Invoke();

        // 判断是否需要移除这一行
        if (ItemCount <= 0) {
            Destroy(this.gameObject);
            return;
        }

        // 更新UI
        ShellSlider.maxValue = ItemCount;
        OnShellSliderValueChange();
    }

    public void OnDestroy() {
        // 通知父节点调整高度
        OnLayoutChanged?.Invoke();

        // 通知取消订阅
        OnDestroyed?.Invoke();
    }
}
