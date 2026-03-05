using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileMode {
    Dry,    // 未浇水
    Watered, // 已浇水
}

[System.Serializable]
public class TileData {
    // ID
    public string Id => $"{x}_{y}";

    // 地块属性
    public int x;
    public int y;
    public GameObject worldObject; // 对应的世界实体，用于显示
    public TileMode tileMode;
    public bool isWatered; // 当前是否已浇水

    // 作物生长信息
    public string cropId;
    public long plantedTimeTick; // 使用 C# Ticks (System.DateTime.Ticks)
    public long currStageStartTimeTick; // 当前阶段开始时间
    public int consecutiveUnwateredDays;  // 连续未浇水天数
    public bool isDead;                   // 是否已经枯死
    public bool harvestAble;            // 是否可收获
    public int currentStageIndex;        // 当前阶段索引
    public bool IsEmpty => string.IsNullOrEmpty(cropId); // 当前地块是否为空

    public TileData(int x, int y, GameObject obj) {
        this.x = x;
        this.y = y;
        this.worldObject = obj;
    }

    // 移除作物时调用
    public void RemoveCrop() {
        cropId = null;
        plantedTimeTick = 0;
        currStageStartTimeTick = 0;
        consecutiveUnwateredDays = 0;
        isDead = false;
        harvestAble = false;
        currentStageIndex = 0;
        isWatered = false;
        tileMode = TileMode.Dry;
    }
}

public class Tile : MonoBehaviour {
    // 对应的地块数据
    public TileData data;
    public Transform PlantRoot; // 作物模型的根节点

    // 当前显示的作物模型
    private GameObject currentModel;
    // 记录上一次更新的阶段索引，避免每帧都 Destroy/Instantiate
    private int lastStageIndex = -1;
    private bool needReSpawns = false; // 是否需要重新加载游戏（需要重新生成模型）

    void Update() {
        if (data == null || data.IsEmpty) return;

        // 每隔一段时间检查一次，不需要每帧都跑，节省性能
        if (Time.frameCount % 60 == 0) {
            UpdateCropVisual();
        }
    }

    private void Start() {
        // 刷新渲染
        if (data != null && !data.IsEmpty) {
            UpdateCropVisual();
        }

        // 刷新颜色
        if(GridManager.Instance != null)
            GridManager.Instance.UpdateGridVisual(this.gameObject);
    }

    // 核心逻辑，根据时间更新作物阶段
    public void UpdateCropVisual() {
        // 判断死亡
        if (data.isDead) {
            GrowStageData deadStage = CropDatabase.GetStageData(data.cropId, data.currentStageIndex);
            SwitchModel(deadStage.DeadPrefab);
            return;
        }

        // 根据时间流逝计算应该在的阶段
        var TimeGoStageIndex = CropDatabase.GetStageIndex(data.cropId, lastStageIndex, data.currStageStartTimeTick);

        // 如果时间足够推进到下一阶段，并且已经浇水（或是刚种植），则切换模型
        if ((TimeGoStageIndex != lastStageIndex && (data.isWatered || TimeGoStageIndex == 0)) || needReSpawns) {
            needReSpawns = false;
            GrowStageData stageInfo = CropDatabase.GetStageData(data.cropId, TimeGoStageIndex);
            SwitchModel(stageInfo.NormalPrefab);
            data.currStageStartTimeTick = GameTimeManager.Instance.CurrentGameTick;
            lastStageIndex = data.currentStageIndex;
            data.currentStageIndex = TimeGoStageIndex;

            // 判断是否可收获
            if (stageInfo.isLastStage) {
                data.harvestAble = true;
            }
        }
    }

    private void SwitchModel(GameObject prefab) {
        if (currentModel != null) Destroy(currentModel);

        if (prefab != null) {
            currentModel = Instantiate(prefab, PlantRoot);
            currentModel.transform.localPosition = new Vector3(0, 0.3f, 0);
        }
    }

    /// <summary>
    /// 重新加载游戏时，读取存档后，调用此方法来恢复作物的显示
    /// </summary>
    public void RePlant() {
        needReSpawns = true;
        lastStageIndex = data.currentStageIndex;
        UpdateCropVisual();
    }

    // 种植时调用
    public void Plant(string id) {
        data.cropId = id;
        // 关键：记录当前时间为种植时间
        data.plantedTimeTick = GameTimeManager.Instance.CurrentGameTick;
        data.currentStageIndex = 0;

        lastStageIndex = -1; // 重置状态
        UpdateCropVisual(); // 立即刷新模型
    }

    // 浇水时调用
    public void Water() {
        // 如果已经死了就不能再浇水
        if (data.isDead) return;
        data.isWatered = true;
        data.consecutiveUnwateredDays = 0;

        // 刷新颜色
        data.tileMode = TileMode.Watered;
        GridManager.Instance.UpdateGridVisual(this.gameObject);
    }

    // 收获时调用
    public bool Harvest() {
        if (data.isDead) return false;

        // 获取产物
        var stageData = CropDatabase.GetCurrentStageData(data.cropId, data.currentStageIndex);
        var fruitData = stageData.fruitData;
        var count = stageData.fruitCount;
        // 尝试将产物添加到背包UI
        var UIBagCtrl = GameUI.instance.UIBagCtrl;
        var res = UIBagCtrl.AddItem(fruitData, count);
        if (res == false) {
            Debug.Log("背包已满，无法添加更多物品");
            return false;
        }
        // 添加果实到数据库
        BagDatabase.Instance.AddItem(fruitData.id, count);

        // 上报收获成就进度
        if (AchievementManager.Instance != null) {
            AchievementManager.Instance.ReportProgress("GOT_5_CROP", 1);
        }

        // 重置地块数据
        data.RemoveCrop();

        // 移除模型
        if (currentModel != null) {
            Destroy(currentModel);
            currentModel = null;
        }

        return true;
    }

    // 铲除时调用
    public void UpRoot() {
        // 重置地块数据
        data.RemoveCrop();
        // 移除模型
        if (currentModel != null) {
            Destroy(currentModel);
            currentModel = null;
        }
    }

}
