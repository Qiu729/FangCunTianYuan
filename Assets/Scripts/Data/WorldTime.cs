using System;

[System.Serializable]
public struct WorldTime {
    public DateTime utcTime;
    public DateTime cstTime;

    public int gameDay;
    public float progress;
    public bool isNight;

    public int utcHour, utcMinute;
    public int cstHour, cstMinute;

    // === 新增：调试状态 ===
    public bool isDebugMode;
    public float timeScale;
    public bool isPaused;

    // 显示
    public string GetCSTTimeString() => $"{cstHour:D2}:{cstMinute:D2}";
    public string GetGameDayString() => $"day-{gameDay}";
    public string GetDisplayString() =>
        $"{GetGameDayString()} · {GetCSTTimeString()} · {(isNight ? "Night" : "Day")}" +
        (isDebugMode ? $" [DEBUG ×{timeScale:F0}]" : "");

    // 倒计时：距离下次 06:00 CST 还剩多久
    public string GetNextDayCountdown() {
        // 下一次游戏日开始：CST 明天 06:00
        DateTime nextDayCST = cstTime.Date.AddDays(1).AddHours(6);
        if (cstTime.Hour >= 6) {
            // 今天还没到 06:00？不可能（因 gameDay 已切换），但保险起见：
            nextDayCST = cstTime.Date.AddDays(cstTime.Hour < 6 ? 0 : 1).AddHours(6);
        }

        DateTime nextDayUTC = TimeZoneInfo.ConvertTimeToUtc(nextDayCST, GameTimeManager.chinaTimeZone);
        TimeSpan left = nextDayUTC - utcTime;

        if (left.TotalSeconds < 0) left = TimeSpan.Zero;

        return $"{(int)left.TotalHours:D2}:{left.Minutes:D2}:{left.Seconds:D2}";
    }
}