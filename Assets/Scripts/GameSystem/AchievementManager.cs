using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

public class AchievementManager : MonoBehaviour {
    public static AchievementManager Instance;

    [Header("配置")]
    public AchievementConfig config;

    [Header("界面")]
    [Tooltip("成就解锁时的预设体（需要有AchievementPopup组件）")]
    public GameObject achievementPopupPrefab;

    [Header("事件")]
    public UnityEvent<string> onAchievementUnlocked;
    public UnityEvent<string, int, int> onProgressUpdated;

    // 运行时数据
    private Dictionary<string, int> progress = new Dictionary<string, int>();
    private HashSet<string> unlocked = new HashSet<string>();
    private bool isInitialized = false;
    private Canvas uiCanvas; // 自动寻找Canvas

    // 调试
    [Header("调试信息")]
    [SerializeField]
    private string[] _debugProgressDisplay;
    public List<string> unlockedList = new List<string>();

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaultProgress();
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        LoadSaveData();
    }

    private void Update() {
        unlockedList = unlocked.ToList();

        _debugProgressDisplay = progress
    .Select(kvp => $"{kvp.Key}: {kvp.Value}")
    .ToArray();
    }

    /// <summary>
    /// 从存档系统加载时调用（初始化完成后）
    /// </summary>
    public void LoadFromArchive(Dictionary<string, int> savedProgress, HashSet<string> savedUnlocked) {
        progress = savedProgress ?? new Dictionary<string, int>();
        unlocked = savedUnlocked ?? new HashSet<string>();

        // 验证/安全检查
        if (config != null) {
            foreach (var ach in config.achievements) {
                if (!progress.ContainsKey(ach.id))
                    progress[ach.id] = 0;

                if (unlocked.Contains(ach.id) && progress[ach.id] < ach.targetValue)
                    progress[ach.id] = ach.targetValue;
            }
        }

        isInitialized = true;
        Debug.Log($"[AchievementManager] 从存档加载完成 - 已解锁 {unlocked.Count} 个成就，isInitialized={isInitialized}");
    }
    
    /// <summary>
    /// 重置成就系统（新游戏时调用）
    /// </summary>
    public void ResetAchievements() {
        Debug.Log("[AchievementManager] 重置成就系统");
        progress.Clear();
        unlocked.Clear();
        isInitialized = false;
        
        // 重新初始化默认进度
        if (config != null) {
            foreach (var ach in config.achievements) {
                progress[ach.id] = 0;
            }
        }
        
        isInitialized = true;
        Debug.Log($"[AchievementManager] 成就系统重置完成 - isInitialized={isInitialized}");
    }

    public (Dictionary<string, int> progress, HashSet<string> unlocked) GetArchiveData() {
        return (new Dictionary<string, int>(progress), new HashSet<string>(unlocked));
    }

    // ====== 核心接口 ======
    public void ReportProgress(string achievementId, int increment = 1) {
        if (!CanOperate()) {
            Debug.LogWarning($"[AchievementManager] 无法上报进度 - 未初始化: isInitialized={isInitialized}, config={config != null}");
            return;
        }
        if (unlocked.Contains(achievementId)) {
            Debug.Log($"[AchievementManager] 成就 {achievementId} 已解锁，跳过进度上报");
            return;
        }

        int oldProgress = progress.GetValueOrDefault(achievementId, 0);
        int target = GetTargetValue(achievementId);
        
        if (target == 0) {
            Debug.LogWarning($"[AchievementManager] 成就 {achievementId} 的目标值为0，可能配置中不存在该成就");
            return;
        }
        
        progress[achievementId] = Mathf.Min(
            oldProgress + increment,
            target
        );
        
        int newProgress = progress[achievementId];
        Debug.Log($"[AchievementManager] 成就进度更新 - ID: {achievementId}, 进度: {oldProgress} -> {newProgress}/{target}");

        if (newProgress >= target) {
            UnlockAchievement(achievementId);
        } else {
            onProgressUpdated?.Invoke(achievementId, newProgress, target);
            RequestSave();
        }
    }

    public void ForceUnlock(string achievementId) {
        if (!CanOperate() || string.IsNullOrEmpty(achievementId)) return;
        if (unlocked.Contains(achievementId)) return;

        progress[achievementId] = GetTargetValue(achievementId);
        UnlockAchievement(achievementId);
    }

    public int GetProgress(string achievementId) =>
        CanOperate() ? progress.GetValueOrDefault(achievementId, 0) : 0;

    public bool IsUnlocked(string achievementId) =>
        CanOperate() && unlocked.Contains(achievementId);

    // ====== 内部实现 ======
    private void InitializeDefaultProgress() {
        progress.Clear();
        unlocked.Clear();
        isInitialized = false;

        if (config != null) {
            Debug.Log($"[AchievementManager] 初始化成就系统 - 配置中有 {config.achievements.Length} 个成就");
            foreach (var ach in config.achievements) {
                progress[ach.id] = 0;
                Debug.Log($"[AchievementManager] 注册成就: {ach.id} (目标: {ach.targetValue})");
            }
        } else {
            Debug.LogError("[AchievementManager] 成就配置为空！请在 Inspector 中分配 AchievementConfig");
        }
    }

    private void UnlockAchievement(string achievementId) {
        Debug.Log($"{achievementId} 已解锁！");

        unlocked.Add(achievementId);

        // 显示弹窗
        ShowAchievementPopup(achievementId);

        onAchievementUnlocked?.Invoke(achievementId);
        RequestSave();
    }

    private void ShowAchievementPopup(string achievementId) {
        // 安全检查
        if (achievementPopupPrefab == null) {
            Debug.LogWarning("Achievement popup prefab not assigned!");
            return;
        }

        // 获取Canvas（运行时使用）- 只查找激活的 Canvas
        if (uiCanvas == null || !uiCanvas.gameObject.activeInHierarchy) {
            // 获取所有Canvas，找到激活的那个
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            uiCanvas = null;
            foreach (var canvas in allCanvases) {
                if (canvas.gameObject.activeInHierarchy) {
                    uiCanvas = canvas;
                    break;
                }
            }
            
            if (uiCanvas == null) {
                Debug.LogError("No active Canvas found in scene! Popup requires an active Canvas.");
                return;
            }
        }

        // 实例化弹窗
        GameObject popupObj = Instantiate(achievementPopupPrefab, uiCanvas.transform);
        var popup = popupObj.GetComponent<AchievementPopup>();

        if (popup != null) {
            popup.ShowPopup(achievementId); // 直接调用显示方法
        } else {
            Debug.LogError("Popup prefab missing AchievementPopup component!");
            Destroy(popupObj);
        }
    }

    private int GetTargetValue(string achievementId) {
        return config?.achievements.FirstOrDefault(a => a.id == achievementId)?.targetValue ?? 0;
    }

    private bool CanOperate() => isInitialized && config != null;

    // ====== 存档系统相关 ======
    private void RequestSave() {

        var saveData = SaveManager.instance.GetCurrentSaveData();
        if (saveData != null) {
            var (progressData, unlockedData) = GetArchiveData();   // 获取当前进度和解锁状态
            saveData.PrepareAchievementSaveData(progressData, unlockedData);  // 转换为序列化形式
            SaveManager.instance.AutoSave();               // 保存数据
        } else {
            Debug.LogError("[AchievementSystem] Save requested but no save data found!");
        }

        //Debug.Log($"[AchievementSystem] Save requested for {unlocked.Count} achievements");
    }

    private void LoadSaveData() {
        var saveData = SaveManager.instance.AutoLoad();
        if (saveData != null) {
            saveData.PrepareRuntimeData();
            LoadFromArchive(saveData.AchievementProgress, saveData.UnlockedAchievements);
        } else {
            //Debug.Log("[AchievementSystem] No save data found on load attempt. Using default values.");
        }
    }
}