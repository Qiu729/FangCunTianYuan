using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// 背包物品基础数据
/// </summary>

[CreateAssetMenu(fileName = "NewBagObject", menuName = "Inventory/Bag Object"), Serializable]
public class BagObjectData : ScriptableObject {

    [Header("基础信息")]
    public string objectName; // 物品名称
    [ShowWhen("showID", true)] public string id; // 物品唯一标识符
    public BagObjectType type; // 物品类型
    public Sprite icon; // 物品图标
    public int maxStack; // 最大堆叠数量

    [Header("交易信息")]
    public bool canBeBuy; // 是否可被购买
    [ShowWhen("canBeBuy", true)] public GoodsType goodsType; // 商品类型
    [ShowWhen("canBeBuy", true)] public int buyPrice; // 购买价格

    public bool canBeSold; // 是否可出售
    [ShowWhen("canBeSold", true)] public int sellPrice; // 出售价格

    [Header("购买限制")]
    public bool isPurchaseLimited; // 是否限制购买数量
    [ShowWhen("isPurchaseLimited", true)] public int purchaseLimitAmount; // 购买数量限制

    // 种子相关数据
    [Header("种子数据")]
    [ShowWhen("type", BagObjectType.Seed)] public CropData cropData; // 对应的作物数据

    // 调试辅助
    [Header("调试")]
    public bool showID = false; // 是否显示 ID 字段

#if UNITY_EDITOR
    // 在编辑器中自动同步 ID 为资源文件名称的大写形式
    private void OnValidate() {
        string targetId = name.ToUpper();
        if (id != targetId) {
            // 如果资源文件名为 "Item_Potion"，ID 自动设为 "ITEM_POTION"
            id = targetId;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    // 自动根据逻辑条件隐藏或显示特定字段的绘制器
    [CustomPropertyDrawer(typeof(ShowWhenAttribute))]
    public class ShowWhenDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var showWhen = attribute as ShowWhenAttribute;
            var shouldShow = ShouldShow(property, showWhen);

            return shouldShow ? EditorGUI.GetPropertyHeight(property) : -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var showWhen = attribute as ShowWhenAttribute;
            var shouldShow = ShouldShow(property, showWhen);

            if (shouldShow) {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private bool ShouldShow(SerializedProperty property, ShowWhenAttribute showWhen) {
            var path = property.propertyPath.Contains(".")
                ? property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'))
                : "";

            var conditionProperty = string.IsNullOrEmpty(path)
                ? property.serializedObject.FindProperty(showWhen.propertyName)
                : property.serializedObject.FindProperty(path + "." + showWhen.propertyName);

            if (conditionProperty == null) return true;

            switch (conditionProperty.propertyType) {
                case SerializedPropertyType.Enum:
                    return conditionProperty.enumValueIndex == (int)showWhen.compareValue;
                case SerializedPropertyType.Boolean:
                    return conditionProperty.boolValue == (bool)showWhen.compareValue;
                case SerializedPropertyType.Integer:
                    return conditionProperty.intValue == (int)showWhen.compareValue;
                case SerializedPropertyType.Float:
                    return Mathf.Approximately(conditionProperty.floatValue, (float)showWhen.compareValue);
                case SerializedPropertyType.String:
                    return conditionProperty.stringValue == (string)showWhen.compareValue;
                default:
                    return true;
            }
        }
    }
#endif
}

/// <summary>
/// 当满足特定条件时显示此字段的特性
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class ShowWhenAttribute : PropertyAttribute {
    public string propertyName;
    public object compareValue;

    public ShowWhenAttribute(string propertyName, object compareValue) {
        this.propertyName = propertyName;
        this.compareValue = compareValue;
    }
}

public enum BagObjectType {
    Seed, // 种子
    Fruit, // 果实
    Tool, // 工具
    Upgrade, // 农具升级
    Other // 其他
}

/// <summary>
/// 商店物品类型
/// </summary>
public enum GoodsType {
    Seed, // 种子
    Tools, // 工具
    Upgrade, // 农具升级
    Others, // 其他
}

/// <summary>
/// 背包中的物品实例，包含数据引用和数量
/// </summary>
[System.Serializable]
public class BagObject {
    public string id; // 物品id
    [SerializeField] private int quantity; // 物品数量
    public BagObject(string id, int quantity) {
        this.id = id;
        this.quantity = quantity;
    }

    public int GetQuantity() { return quantity; }

    public void SetQuantity(int quantity) {
        this.quantity = quantity;
        // 自动存档
        BagDatabase.Instance.AutoSaveBagData();
    }
}
