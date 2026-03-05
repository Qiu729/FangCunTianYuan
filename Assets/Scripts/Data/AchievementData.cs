using UnityEngine;

/// <summary>
/// 成就配置数据脚本对象
/// </summary>
[CreateAssetMenu(fileName = "NewAchievements", menuName = "Achievements/Config")]
public class AchievementConfig : ScriptableObject {
    [System.Serializable]
    public class Achievement {
        public string title = "新成就";            // 显示标题
        public string id = "NEW_ACHIEVEMENT";      // 唯一ID
        public string description = "描述内容";      // 详细描述
        public int targetValue = 1;                // 达成目标值
        public bool isSecret = false;              // 是否为秘密成就
        public Sprite icon;                        // 成就图标
        [HideInInspector] public bool isUnlocked;  // 用于在编辑器预览
    }

    public Achievement[] achievements;

#if UNITY_EDITOR
    // 已取消 ID 自动同步逻辑，支持中文标题和手动配置 ID
#endif
}
