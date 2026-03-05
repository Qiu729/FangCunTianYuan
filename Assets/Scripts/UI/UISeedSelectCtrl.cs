using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GUI种植模式中，种子选择列表的控制器
/// </summary>
public class UISeedSelectCtrl : MonoBehaviour {
    [Header("子UI控件")]
    public GameObject SeedItemPrefab; // 种子项预制体
    public Transform ContentParent; // 内容父物体

    [Header("水平排列参数")]
    public float ItemSpacing = 10f; // 项之间的间距
    public float LeftPadding = 35f; // 起始X位置 
    public float RightPadding = 35f; // 右侧内边距

    public void InitSeedList() {
        // 清理旧内容
        for (int i = ContentParent.childCount - 1; i >= 0; i--) {
            var child = ContentParent.GetChild(i);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // 获取背包物品
        var bagItems = BagDatabase.Instance.items;
        float currentX = LeftPadding;

        foreach (var item in bagItems) {
            if (ItemRegistry.Get(item.id).type != BagObjectType.Seed) continue;
            if (item.GetQuantity() <= 0) continue;

            // 实例化种子项
            GameObject seedItemObj = Instantiate(SeedItemPrefab, ContentParent);
            UISeedItem seedItemComp = seedItemObj.GetComponent<UISeedItem>();
            if (seedItemComp != null) {
                seedItemComp.Setup(item);
            }

            if (seedItemObj.activeSelf == false) continue;

            // 手动摆放位置
            var pos = seedItemObj.transform.localPosition;
            pos.x = currentX;
            seedItemObj.transform.localPosition = pos;

            currentX += seedItemComp.Bg.GetComponent<RectTransform>().rect.width + ItemSpacing;
        }

        // 手动设置content宽度
        ContentParent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentX - LeftPadding + RightPadding);
    }
}
