using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISeedItem : MonoBehaviour {

    // 子UI控件
    public Image Bg; // 背景图，用于推算宽度
    public Image IconImage; // 种子图标
    public TextMeshProUGUI Seedname; // 种子名称
    public TextMeshProUGUI SeedAmountText; // 种子数量显示
    public Button SelectButton; // 选择按钮

    // 内部数据
    [HideInInspector] public BagObject bagObj; // 对应背包中的物品数据，方便增减数量

    // 设置种子项的数据
    public void Setup(BagObject bagItem) {
        // 设置参数
        bagObj = bagItem;
        var data = ItemRegistry.Get(bagItem.id);
        if (IconImage != null && data.icon != null) {
            IconImage.sprite = data.icon;
        }
        Seedname.text = data.objectName;
        SeedAmountText.text = bagItem.GetQuantity().ToString();

        // 绑定按钮点击事件
        if (SelectButton != null) {
            SelectButton.onClick.RemoveAllListeners(); // 先移除旧的监听器，防止重复添加
            SelectButton.onClick.AddListener(OnSelectSeed);
        }
    }

    // 选中种子时的回调
    private void OnSelectSeed() {
        var cropData = ItemRegistry.Get(bagObj.id).cropData;
        InteractionManager.Instance.SetselectedSeed(cropData.id, bagObj);
    }
}
