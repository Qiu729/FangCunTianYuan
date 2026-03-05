using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementItemUI : MonoBehaviour {
    [Header("UI 引用")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image bgImage;
    public TextMeshProUGUI stateText;

    [Header("视觉效果配置")]
    public Color unlockedBGColor = Color.white;
    public Color lockedBGColor = Color.gray;
    public string unlockedStateStr = "已解锁";
    public string lockedStateStr = "未解锁";
    public Color unlockedStateColor = Color.green;
    public Color lockedStateColor = Color.red;

    /// <summary>
    /// 设置成就项显示内容
    /// </summary>
    /// <param name="achievement">成就配置数据</param>
    /// <param name="isUnlocked">是否已解锁</param>
    public void Setup(AchievementConfig.Achievement achievement, bool isUnlocked) {
        if (achievement == null) return;

        // 设置基础信息
        titleText.text = achievement.title;
        descriptionText.text = achievement.description;
        
        if (achievement.icon != null) {
            iconImage.sprite = achievement.icon;
        }

        // 根据解锁状态更新视觉效果
        if (isUnlocked) {
            bgImage.color = unlockedBGColor;
            stateText.text = unlockedStateStr;
            stateText.color = unlockedStateColor;
            // 如果是秘密成就且已解锁，显示原图标（可选：也可以在Locked状态置灰）
            iconImage.color = Color.white;
        } else {
            bgImage.color = lockedBGColor;
            stateText.text = lockedStateStr;
            stateText.color = lockedStateColor;
            
            // 如果是秘密成就且未解锁，可以隐藏内容
            if (achievement.isSecret) {
                titleText.text = "???";
                descriptionText.text = "这是一个秘密成就，继续探索以解锁。";
                iconImage.color = Color.black; // 或者设为半透明/问号
            } else {
                iconImage.color = new Color(1, 1, 1, 0.5f); // 未解锁置灰/半透明
            }
        }
    }
}
