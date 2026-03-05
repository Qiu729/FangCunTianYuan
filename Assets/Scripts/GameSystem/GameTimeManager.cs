using UnityEngine;
using System;


[DefaultExecutionOrder(-10)]
public class GameTimeManager : MonoBehaviour {
    public static GameTimeManager Instance;

    [Header("中国标准时区配置")]
    public const int GAME_DAY_START_HOUR_CST = 6; // 游戏时间 6 点算一天

    public static TimeZoneInfo chinaTimeZone;

    [Header("游戏时间（只读）")]
    public WorldTime currentTime;

    [Header("调试时间加速（仅 Editor/Debug）")]
    [Tooltip("时间加速倍率（1 = 实时，60 = 1秒=1小时，1440 = 1秒=1天）")]
    [Range(0, 10000)]
    public float timeScale = 1f;

    [Tooltip("是否暂停游戏时间")]
    public bool isPaused = false;

    [Tooltip("手动前进 1 分钟（调试按钮）")]
    public bool stepOneMinute;

    [Tooltip("重置为当前真实时间")]
    public bool resetToRealTime;

    // 变量：内部状态 变量
    //private const string LAST_SAVED_GAME_DAY_KEY = "LastSavedGameDay_CST";
    private int lastSavedGameDay;
    private int lastFrameGameDay;
    private bool hasProcessedOfflineDays = false; // 标记是否已处理过离线天数

    private DateTime? debugOverrideTime; // 手动覆盖时间（用于测试）
    private float accumulatedTime;       // 加速时间累积（避免受帧率影响）

    // 公共属性
    public long CurrentGameTick => (debugOverrideTime ?? DateTime.UtcNow).Ticks;
    public int CurrentGameDay => GetCurrentGameDay(debugOverrideTime ?? DateTime.UtcNow);

    private void Awake() {
        chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() {
        UpdateTime();
        // 不在这里处理离线天数，等 GridManager 准备好后再处理
    }

    /// <summary>
    /// GridManager 初始化完成后调用此方法
    /// </summary>
    public void OnGridManagerReady() {
        if (!hasProcessedOfflineDays) {
            ProcessOfflineDays();
            hasProcessedOfflineDays = true;
        }
    }

    private void Update() {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            return;
        }

        HandleDebugInputs();
        UpdateTime();

        // 检测新游戏日（CST 06:00）
        if (lastFrameGameDay == 0) {
            lastFrameGameDay = currentTime.gameDay;
        }
        if (currentTime.gameDay != lastFrameGameDay) {
            OnNewGameDay(currentTime.gameDay);
            lastFrameGameDay = currentTime.gameDay;
            SaveGameDay();
        }
    }

    private void OnApplicationQuit() {
        SaveGameDay();
    }

    // 以下是调试功能（调试开关）以下是调试功能
    private void HandleDebugInputs() {
#if UNITY_EDITOR || DEBUG
        // 前进 1 分钟（按钮测试）
        if (stepOneMinute) {
            stepOneMinute = false;
            StepTime(TimeSpan.FromMinutes(1));
        }

        // 重置为真实时间
        if (resetToRealTime) {
            resetToRealTime = false;
            debugOverrideTime = null;
            accumulatedTime = 0f;
        }

        // 时间加速累积（固定更新避免受帧率影响）
        if (!isPaused && timeScale > 0 && debugOverrideTime.HasValue) {
            accumulatedTime += Time.unscaledDeltaTime * timeScale;
            if (accumulatedTime >= 1f) { // 每秒更新一次（可调整）
                debugOverrideTime = debugOverrideTime.Value.AddSeconds((int)accumulatedTime);
                accumulatedTime -= (int)accumulatedTime;
            }
        }
#endif
    }

    /// <summary>
    /// 手动推进时间（用于调试）
    /// </summary>
    public void StepTime(TimeSpan step) {
        debugOverrideTime = (debugOverrideTime ?? DateTime.UtcNow).Add(step);
    }

    /// <summary>
    /// 设置调试时间（例如：设为凌晨 05:59 测试新日）
    /// </summary>
    public void SetDebugTime(DateTime cstTime) {
        // 传入的是北京时间 需 转为 UTC 存储
        debugOverrideTime = TimeZoneInfo.ConvertTimeToUtc(cstTime, chinaTimeZone);
    }

    /// <summary>
    /// 获取「当前用于计算的时间」（支持调试覆盖）
    /// </summary>
    private DateTime GetCurrentCalculationTime() {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            return debugOverrideTime ?? DateTime.UtcNow;
        }

#if UNITY_EDITOR || DEBUG
        if (isPaused) {
            return debugOverrideTime ?? DateTime.UtcNow;
        }

        if (debugOverrideTime.HasValue && timeScale > 0) {
            // 返回已累积的调试时间（在 Update 中累积逻辑）
            return debugOverrideTime.Value;
        }
#endif
        return DateTime.UtcNow;
    }

    // 以下是核心逻辑（更新逻辑）以下是核心逻辑
    private void UpdateTime() {
        currentTime = CalculateWorldTime(GetCurrentCalculationTime());
    }

    private void SaveGameDay() {
        var saveData = SaveManager.instance.GetCurrentSaveData();
        if (saveData == null) {
            Debug.LogError("无法获取存档");
            saveData = new SaveData();
        }

        saveData.gameDay = currentTime.gameDay;
        //Debug.Log($"保存数据：{saveData.gameDay}");
        SaveManager.instance.AutoSave();
    }

    private void LoadLastSavedGameDay() {
        var saveData = SaveManager.instance.AutoLoad();
        if (saveData == null) {
            //Debug.Log($"{name}无法获取存档,使用当前日期");
            lastSavedGameDay = currentTime.gameDay;
            return;
        }
        //Debug.Log($"读取到存档的gameday:{saveData.gameDay}");
        lastSavedGameDay = saveData.gameDay;
    }

    private void ProcessOfflineDays() {
        LoadLastSavedGameDay();

        // 如果使用调试时间（时间覆盖），离线时间不处理
        if (debugOverrideTime.HasValue) return;

        if (currentTime.gameDay <= lastSavedGameDay) return;
        for (int day = lastSavedGameDay + 1; day <= currentTime.gameDay; day++) {
            OnNewGameDay(day);
        }

        //lastSavedGameDay = currentTime.gameDay;
        SaveGameDay();
    }

    /// <summary>
    /// 游戏时间计算（支持 CST 06:00 起点 + 调试加速）
    /// </summary>
    public WorldTime CalculateWorldTime(DateTime utcNow) {
        // CST 时间
        DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, chinaTimeZone);

        // === 游戏日计算（起点：CST 06:00）===
        DateTime epochCST = new DateTime(2025, 1, 1, 6, 0, 0, DateTimeKind.Unspecified);
        DateTime epochUTC = TimeZoneInfo.ConvertTimeToUtc(epochCST, chinaTimeZone);

        double secondsSinceEpoch = (utcNow - epochUTC).TotalSeconds;
        int gameDay = Mathf.Max(0, (int)Math.Floor(secondsSinceEpoch / 86400.0));

        // 时间进度
        float progress = 0f + cstTime.Hour / 24f + cstTime.Minute / 1440f + cstTime.Second / 86400f;

        // 昼夜（CST）
        bool isNight = cstTime.Hour < 6 || cstTime.Hour >= 20;

        return new WorldTime {
            utcTime = utcNow,
            cstTime = cstTime,

            gameDay = gameDay,
            utcHour = utcNow.Hour,
            utcMinute = utcNow.Minute,
            cstHour = cstTime.Hour,
            cstMinute = cstTime.Minute,

            progress = progress,
            isNight = isNight,

            // 调试信息
            isDebugMode = debugOverrideTime.HasValue,
            timeScale = timeScale,
            isPaused = isPaused
        };
    }

    private int GetCurrentGameDay(DateTime utcNow) {
        DateTime epochUTC = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2025, 1, 1, 6, 0, 0, DateTimeKind.Unspecified),
            chinaTimeZone
        );
        return (int)Math.Floor((utcNow - epochUTC).TotalDays);
    }

    public float GetGameDaysPassed(long startTick) {
        DateTime start = new DateTime(startTick, DateTimeKind.Utc);
        DateTime now = GetCurrentCalculationTime();

        DateTime epochUTC = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2025, 1, 1, 6, 0, 0, DateTimeKind.Unspecified),
            chinaTimeZone
        );

        return (float)((now - epochUTC).TotalSeconds - (start - epochUTC).TotalSeconds) / 86400f;
    }

    private void OnNewGameDay(int newGameDay) {
        string timeStr = debugOverrideTime?.ToString("yyyy-MM-dd HH:mm:ss UTC")
                        ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        Debug.Log($"[GameTime] 新游戏日开始！第 {newGameDay} 天（服务器时间: {timeStr}）");

        GridManager.Instance?.ResetAllGridDry();
    }
}

