using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GuidanceBlockType { Text, Image }

/// <summary>
/// 单个指南内容块（文字或图片）
/// </summary>
[System.Serializable]
public class GuidanceBlock {
    public GuidanceBlockType type;
    [TextArea(3, 10)]
    public string text;
    public Texture texture;
}

/// <summary>
/// 指南的一个章节配置
/// </summary>
[System.Serializable]
public class GuidanceSection {
    public string title;
    public List<GuidanceBlock> blocks = new List<GuidanceBlock>();
}

/// <summary>
/// 玩家指南 UI 控制器
/// 支持图文混排、高度自适应及滚动条同步
/// </summary>
public class PlayerGuidance : MonoBehaviour {
    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private RectTransform contentContainer; // ScrollRect 的 Content
    [SerializeField] private Transform indexContainer;
    [SerializeField] private GameObject indexButtonPrefab;

    [Header("块预制体")]
    [SerializeField] private GameObject textBlockPrefab;
    [SerializeField] private GameObject imageBlockPrefab;

    [Header("内容配置")]
    [SerializeField] private List<GuidanceSection> guidanceSections = new List<GuidanceSection>();

    private Dictionary<int, GameObject> sectionPages = new Dictionary<int, GameObject>();
    private int currentSectionIndex = -1;

    private void Start() {
        InitializeGuidance();
    }

    /// <summary>
    /// 初始化指南，预生成所有页面以消除切换时的布局跳动
    /// </summary>
    public void InitializeGuidance() {
        // 1. 清理
        ClearContent();
        foreach (Transform child in indexContainer) Destroy(child.gameObject);

        // 2. 禁用容器自动布局（我们将手动同步 Page 高度到 Content，以获得最稳定的滚动条表现）
        var vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;
        var fitter = contentContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        // 3. 预生成页面与索引按钮
        for (int i = 0; i < guidanceSections.Count; i++) {
            CreateSectionPage(i);
            CreateLinkButton(i);
        }

        // 4. 默认显示
        if (guidanceSections.Count > 0) {
            ShowContent(0);
        } else {
            if (titleText != null) titleText.text = "暂无指南";
        }
    }

    /// <summary>
    /// 为特定章节生成静态页面
    /// </summary>
    private void CreateSectionPage(int index) {
        var section = guidanceSections[index];
        GameObject pageObj = new GameObject($"Page_{index}", typeof(RectTransform));
        pageObj.transform.SetParent(contentContainer, false);
        
        RectTransform rt = pageObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(0, rt.offsetMin.y); // Left = 0
        rt.offsetMax = new Vector2(0, rt.offsetMax.y); // Right = 0
        
        // 设置页面内部布局
        var vlg = pageObj.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 15;
        vlg.padding = new RectOffset(10, 10, 10, 20);

        var csf = pageObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 填充块
        foreach (var block in section.blocks) {
            if (block.type == GuidanceBlockType.Text) {
                CreateTextBlock(pageObj.transform, block.text);
            } else {
                CreateImageBlock(pageObj.transform, block.texture);
            }
        }

        pageObj.SetActive(false);
        sectionPages[index] = pageObj;
    }

    private void CreateTextBlock(Transform parent, string content) {
        if (textBlockPrefab == null) return;
        GameObject go = Instantiate(textBlockPrefab, parent);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = content;
    }

    private void CreateImageBlock(Transform parent, Texture tex) {
        if (imageBlockPrefab == null || tex == null) return;
        GameObject go = Instantiate(imageBlockPrefab, parent);
        var rawImg = go.GetComponentInChildren<RawImage>();
        if (rawImg != null) {
            rawImg.texture = tex;
            
            // 动态计算适配高度
            Canvas.ForceUpdateCanvases();
            float width = GetContainerWidth();
            float aspectRatio = (float)tex.height / tex.width;
            float height = width * aspectRatio;

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            rawImg.rectTransform.sizeDelta = new Vector2(width, height);
        }
    }

    private void CreateLinkButton(int index) {
        if (indexButtonPrefab == null) return;
        GameObject btnObj = Instantiate(indexButtonPrefab, indexContainer);
        var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = guidanceSections[index].title;

        var btn = btnObj.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => ShowContent(index));
    }

    private float GetContainerWidth() {
        float w = contentContainer.rect.width;
        Transform curr = contentContainer;
        while (w <= 1 && curr.parent != null) {
            curr = curr.parent;
            w = (curr as RectTransform).rect.width;
        }
        return w > 1 ? w : 600f;
    }

    /// <summary>
    /// 切换显示指定的指南内容
    /// </summary>
    public void ShowContent(int index) {
        if (!sectionPages.ContainsKey(index)) return;

        if (currentSectionIndex != -1) sectionPages[currentSectionIndex].SetActive(false);
        
        currentSectionIndex = index;
        titleText.text = guidanceSections[index].title;
        sectionPages[index].SetActive(true);

        StopAllCoroutines();
        StartCoroutine(SyncLayoutRoutine());
    }

    /// <summary>
    /// 同步子页面高度到 ScrollRect Content，并重置滚动位置
    /// </summary>
    private IEnumerator SyncLayoutRoutine() {
        yield return null; // 等待 Active 状态生效
        
        if (currentSectionIndex != -1 && sectionPages.TryGetValue(currentSectionIndex, out GameObject page)) {
            RectTransform pageRT = page.GetComponent<RectTransform>();
            
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(pageRT);

            // 关键同步：父容器高度 = 页面首选高度
            float targetHeight = LayoutUtility.GetPreferredHeight(pageRT);
            contentContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            
            yield return null;
            var scroll = GetComponentInChildren<ScrollRect>();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void ClearContent() {
        foreach (var page in sectionPages.Values) if (page != null) Destroy(page);
        sectionPages.Clear();
    }
}
