using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// 手撸的轻量化作物数据库，可以用来存储和管理游戏中的作物数据。
/// </summary>

public class CropDatabase : MonoBehaviour {
    public static CropDatabase Instance;

    [Header("配置")]
    public List<CropData> allCrops;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }

        // 自动收集所有作物配置（仅在编辑器模式下）
        //FindAllCrops();
    }

    // 根据 ID 获取作物配置
    public static CropData GetCropData(string id) {
        if (Instance == null) return null;
        return Instance.allCrops.FirstOrDefault(c => c.id == id);
    }

    // 核心功能：根据作物生长了多久，返回它当前应该处于哪个阶段的数据
    /// <param name="cropId">作物ID</param>
    /// <param name="currentAgeDays">当前已经生长了多少天</param>
    /// <returns>当前阶段的数据 (GrowStageData)</returns>
    public static GrowStageData GetCurrentStageData(string cropId, float currentAgeDays) {
        CropData data = GetCropData(cropId);
        if (data == null || data.growStages.Count == 0) return null;

        float accumulatedDays = 0;

        // 遍历所有阶段
        for (int i = 0; i < data.growStages.Count; i++) {
            float stageDuration = data.growStages[i].DurationDays;

            // 累加时间
            accumulatedDays += stageDuration;

            // 如果当前年龄小于累加时间，说明就在这个阶段
            if (currentAgeDays < accumulatedDays) {
                return data.growStages[i];
            }
        }

        // 如果超过了所有阶段的时间，通常返回最后一个阶段（成熟阶段）
        return data.growStages[data.growStages.Count - 1];
    }

    // 获取种子的预制体（默认是第0个阶段的正常模型）
    public GameObject GetSeedPrefab(string id) {
        CropData data = GetCropData(id);
        if (data != null && data.growStages.Count > 0) {
            return data.growStages[0].NormalPrefab;
        }
        return null;
    }

    public static int GetStageIndex(string id, int lastStageIndex, long currStageStartTimeTick) {
        CropData data = GetCropData(id);
        if (data == null) return -1;

        // 如果已经处于最后一个阶段，直接返回
        if (lastStageIndex + 1 >= data.growStages.Count) {
            return lastStageIndex;
        }

        var currStagePassedDays = GameTimeManager.Instance.GetGameDaysPassed(currStageStartTimeTick);
        //Debug.Log($"currStagePassedDays{currStagePassedDays}");

        if (currStagePassedDays >= data.growStages[lastStageIndex + 1].DurationDays) {
            return lastStageIndex + 1;
        } else {
            return lastStageIndex;
        }
    }

    public static GrowStageData GetStageData(string id, int index) {
        CropData data = GetCropData(id);
        if (data != null && index >= 0 && index < data.growStages.Count) {
            return data.growStages[index];
        }
        return null;
    }


#if UNITY_EDITOR
    [ContextMenu("快速添加Assets里面的所有CropsData")]
    private void FindAllCrops() {
        var crops = Resources.LoadAll<CropData>("Farming");
        Debug.Log($"找到 {crops.Length} 个作物配置：");
        foreach (var crop in crops) {
            if (allCrops.Contains(crop)) continue;
            allCrops.Add(crop);
        }
    }
#endif

}