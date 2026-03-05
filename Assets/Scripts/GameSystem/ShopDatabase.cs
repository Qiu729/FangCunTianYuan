using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopDatabase : MonoBehaviour {

    public static ShopDatabase instance;

    public Dictionary<GoodsType, List<BagObjectData>> goodsDicList = new Dictionary<GoodsType, List<BagObjectData>>(); // 货物列表，根据分类存储

    private void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        // 先建List
        foreach (GoodsType type in Enum.GetValues(typeof(GoodsType))) {
            goodsDicList[type] = new List<BagObjectData>();
        }

        // 从Resources里加载出全部可出售物品
        var bagObjDatas = Resources.LoadAll<BagObjectData>("Inventory");
        foreach (var bagObjData in bagObjDatas) {
            if (bagObjData.canBeBuy) {
                goodsDicList[bagObjData.goodsType].Add(bagObjData);
            }
        }
    }
}
