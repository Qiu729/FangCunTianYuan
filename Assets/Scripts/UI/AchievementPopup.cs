using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AchievementPopup : MonoBehaviour {
    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject secretCover;

    [Header("动画设置")]
    [SerializeField] private float popupDuration = 0.3f;
    [SerializeField] private float stayDuration = 2.5f;
    [SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Vector3 popScale = new Vector3(1.1f, 1.1f, 1.1f);

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private bool isInitialized = false;

    void Awake() {
        InitializeComponents();
        // 不在这里隐藏，让 GameObject 保持激活状态
        // 因为弹窗是动态实例化的，实例化后会立即调用 ShowPopup
    }

    private void InitializeComponents() {
        if (isInitialized) return;

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) {
            Debug.LogError("AchievementPopup: RectTransform component not found!");
            return;
        }

        originalScale = rectTransform.localScale;
        isInitialized = true;
    }

    void OnEnable() {
        if (AchievementManager.Instance != null) {
            AchievementManager.Instance.onAchievementUnlocked.AddListener(ShowPopup);
        }
    }

    void OnDisable() {
        if (AchievementManager.Instance != null) {
            AchievementManager.Instance.onAchievementUnlocked.RemoveListener(ShowPopup);
        }
        StopAllCoroutines();
    }

    public void ShowPopup(string achievementId) {
        // 确保组件已初始化
        if (!isInitialized) {
            InitializeComponents();
        }

        // 安全检查
        if (rectTransform == null) {
            Debug.LogError("AchievementPopup: Cannot show popup, RectTransform is null!");
            return;
        }

        if (AchievementManager.Instance == null || AchievementManager.Instance.config == null) {
            Debug.LogError("AchievementPopup: AchievementManager or config is null!");
            return;
        }

        var ach = System.Array.Find(
            AchievementManager.Instance.config.achievements,
            a => a.id == achievementId
        );

        if (ach == null) {
            Debug.LogWarning($"AchievementPopup: Achievement with ID '{achievementId}' not found!");
            return;
        }

        // 更新UI内容（添加空值检查）
        if (titleText != null) titleText.text = ach.title;
        if (descText != null) descText.text = ach.isSecret && !ach.isUnlocked ? "???" : ach.description;
        if (iconImage != null) iconImage.sprite = ach.icon;
        if (secretCover != null) secretCover.SetActive(ach.isSecret && !ach.isUnlocked);

        // 重置状态
        rectTransform.localScale = originalScale;
        
        // 确保 GameObject 在层级中是激活的
        if (!gameObject.activeInHierarchy) {
            Debug.LogError("AchievementPopup: GameObject is not active in hierarchy!");
            return;
        }

        // 停止之前的动画（如果有）
        StopAllCoroutines();
        
        // 播放动画
        StartCoroutine(PopupAnimation());
    }

    private IEnumerator PopupAnimation() {
        float timer = 0f;

        // 弹出动画
        while (timer < popupDuration) {
            float t = popupCurve.Evaluate(timer / popupDuration);
            rectTransform.localScale = Vector3.Lerp(originalScale, popScale, t);
            timer += Time.unscaledDeltaTime; // 不受时间缩放影响
            yield return null;
        }

        // 保持显示
        yield return new WaitForSecondsRealtime(stayDuration);

        // 收回动画
        timer = 0f;
        while (timer < popupDuration * 0.7f) {
            float t = 1 - popupCurve.Evaluate(timer / (popupDuration * 0.7f));
            rectTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 动画结束后销毁弹窗
        Destroy(gameObject);
    }
}
