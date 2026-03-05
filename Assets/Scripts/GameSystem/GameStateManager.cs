using System;
using UnityEngine;

public enum GameState {
    StartMenu,   // 开始菜单
    Playing,     // 正常游戏
    Paused,      // 游戏暂停
    GameOver     // 游戏结束
}

public class GameStateManager : MonoBehaviour {
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.StartMenu;

    // 状态改变事件
    public static event Action<GameState> OnStateChanged;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        // 初始化时应用一次状态逻辑并触发事件
        ApplyStateEffects(CurrentState);
        OnStateChanged?.Invoke(CurrentState);
    }

    public void ChangeState(GameState newState) {
        if (CurrentState == newState) return;

        CurrentState = newState;
        ApplyStateEffects(newState);
        OnStateChanged?.Invoke(newState);
        
        Debug.Log($"[GameState] 切换到: {newState}");
    }

    private void ApplyStateEffects(GameState state) {
        switch (state) {
            case GameState.StartMenu:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                // 如果有锁定鼠标的逻辑可以在这里恢复
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }
    }

    /// <summary>
    /// 快捷进入游戏的方法
    /// </summary>
    public void StartGame() {
        ChangeState(GameState.Playing);
    }
}
