using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour {
    public static SaveManager instance;

    private bool _isSaving;

    private SaveData currentSaveData;  // 运行时的缓存，避免频繁读写文件，还能避免不同逻辑间的数据不一致问题

    #region UnityLifeCycle
    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject); // 销毁重复的实例，而不是原始实例
        }
    }

    private void Start() {
        InitializeSaveData();
    }

    #endregion

    #region SaveSystem

    // 初始化存档（游戏启动时调用）
    public void InitializeSaveData() {
        currentSaveData = AutoLoad();
        if (currentSaveData == null) {
            currentSaveData = new SaveData();
            // 初始化默认值
            if (GameTimeManager.Instance != null) {
                currentSaveData.startGameDay = GameTimeManager.Instance.currentTime.gameDay;
            } else {
                currentSaveData.startGameDay = 1;
            }
            currentSaveData.gameDay = currentSaveData.startGameDay;
            currentSaveData.Coins = 0;
            // ... 其他初始化 ...
        }

        // 关键：准备成就系统的运行时数据
        currentSaveData.PrepareRuntimeData();
    }

    // 获取当前运行时数据（供其他系统使用）
    public SaveData GetCurrentSaveData() => currentSaveData;

    private string GetSavePath(string slotName) {
        string path = Path.Combine(Application.persistentDataPath, slotName + ".json");
        // Debug.Log($"[SaveManager] 存档路径: {path}");
        return path;
    }

    // 自动存档
    public void AutoSave() {
        if (_isSaving) return; // 如果正在存档，则忽略新的请求
        StartCoroutine(SaveCoroutine("autosave"));
    }

    private IEnumerator SaveCoroutine(string slotName) {
        _isSaving = true;
        GameUI.instance.ToggleSaveStageUIActive(true);

        currentSaveData.saveTime = System.DateTime.Now.ToString();
        SaveToFile(currentSaveData, slotName);
        // Debug.Log($"[SaveManager] 自动存档完成，存档路径: {GetSavePath(slotName)}");

        yield return new WaitForSeconds(0.5f); // 让UI显示0.5秒

        GameUI.instance.ToggleSaveStageUIActive(false);
        _isSaving = false;
    }

    /// <summary>
    /// 读取存档数据，如果没有存档则返回null
    /// </summary>
    /// <returns></returns>
    public SaveData AutoLoad() {
        SaveData data = LoadFromFile("autosave");
        //Debug.Log($"[SaveManager] 自动加载存档: {(data != null ? "成功" : "未找到存档")}");
        return data;
    }

    /// <summary>
    /// 检查是否存在存档
    /// </summary>
    public bool HasSaveFile() {
        string path = GetSavePath("autosave");
        bool exists = File.Exists(path);
        //Debug.Log($"[SaveManager] 检查存档是否存在: {exists}");
        return exists;
    }

    /// <summary>
    /// 开启新游戏：重置内存数据并删除物理文件
    /// </summary>
    public void NewGame() {
        string path = GetSavePath("autosave");
        if (File.Exists(path)) {
            File.Delete(path);
            Debug.Log($"[SaveManager] 已删除旧存档: {path}");
        }

        // 重新初始化一份干净的数据
        currentSaveData = new SaveData();
        if (GameTimeManager.Instance != null) {
            currentSaveData.startGameDay = GameTimeManager.Instance.currentTime.gameDay;
        } else {
            currentSaveData.startGameDay = 1;
        }
        currentSaveData.gameDay = currentSaveData.startGameDay;
        currentSaveData.Coins = 30;

        currentSaveData.PrepareRuntimeData();
        
        // 通知成就系统重置
        if (AchievementManager.Instance != null) {
            AchievementManager.Instance.ResetAchievements();
        }

        Debug.Log($"[SaveManager] 已创建新游戏存档");
    }

    private void SaveToFile(SaveData data, string slotName) {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(slotName), json);
    }

    private SaveData LoadFromFile(string slotName) {
        string path = GetSavePath(slotName);
        if (File.Exists(path)) {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<SaveData>(json);
            return data;
        }
        return null;
    }


    #endregion
}
