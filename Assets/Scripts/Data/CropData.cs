using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 农作物的数据类
/// </summary>

[CreateAssetMenu(fileName = "NewCropData", menuName = "Farming/Crop Data")]
public class CropData : ScriptableObject {
    [Header("基本信息")]
    public string cropName; // 农作物名称
    public string id; // 农作物唯一标识符

    [Header("生长配置")]
    [Tooltip("这里是一个列表，你可以填入任意数量的阶段（种子->发芽->成熟）")]
    public List<GrowStageData> growStages = new List<GrowStageData>(); // 生长阶段数据列表

#if UNITY_EDITOR

    // 在编辑器中自动同步 ID 为文件名的大写形式
    private void OnValidate() {
        // 获取当前文件名作为 ID
        if (id != this.name) {
            // 比如文件名是 "Data_Corn"，ID 自动变成 "DATA_CORN"
            id = this.name.ToUpper();
            UnityEditor.EditorUtility.SetDirty(this);
        }
        if (growStages.Count <= 0) return;
        // 设置最后一阶段的标记
        foreach (var stage in growStages) {
            stage.isLastStage = false;
        }
        growStages[growStages.Count - 1].isLastStage = true;
    }
#endif

    // 获取作物成熟所需的总天数
    public float GetTotalGrowDays() {
        float total = 0;
        foreach (var stage in growStages) {
            total += stage.DurationDays;
        }
        return total;
    }
}

/// <summary>
/// 农作物生长阶段数据类
/// </summary>
[System.Serializable]
public class GrowStageData {
    public string Name; // 阶段名称
    [ShowWhen("isLastStage", false)]
    public int DurationDays; // 阶段持续时间（天数）
    public bool willDie; // 如果未浇水，是否会在这个阶段死亡
    [ShowWhen("willDie", true)]
    public int MaxUnwateredDays; // 最大允许未浇水天数
    [Tooltip("AutoSet by Scirpt")] public bool isLastStage; // 是否为最后阶段（成熟阶段），只有这个阶段可以收获
    [ShowWhen("isLastStage", true)]
    public BagObjectData fruitData; // 果实的物品数据
    [ShowWhen("isLastStage", true)]
    public int fruitCount; // 收获能拿到的果实数量
    public GameObject NormalPrefab; // 正常状态预制体
    [ShowWhen("willDie", true)]
    public GameObject DeadPrefab; // 死亡状态预制体
}
