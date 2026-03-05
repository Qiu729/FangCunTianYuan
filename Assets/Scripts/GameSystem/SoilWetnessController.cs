using UnityEngine;

public class SoilWetnessController : MonoBehaviour {
    [Range(0f, 1f)] public float wetness = 0f; // 0=干, 1=湿

    public Color dryColor = new Color(0.6f, 0.5f, 0.4f);   // 干燥颜色（泥土色）
    public Color wetColor = new Color(0.3f, 0.25f, 0.2f);  // 湿润颜色（深褐色）

    [Range(0f, 1f)] public float drySmoothness = 0.1f;     // 干燥时粗糙度
    [Range(0f, 1f)] public float wetSmoothness = 0.6f;     // 湿润时光滑度

    private Renderer soilRenderer;
    private Material originalMaterial;
    private Material wetnessMaterial; // 使用实例化的材质以便独立调整参数

    private Tile tileComp;

    void Start() {
        soilRenderer = GetComponent<Renderer>();
        if (soilRenderer == null) {
            Debug.LogError("SoilWetnessController: No Renderer component found.");
            return;
        }


        tileComp = transform.parent.GetComponentInChildren<Tile>();
        if (tileComp == null) {
            Debug.LogError("SoilWetnessController: No Tile component found in children.");
            return;
        }

        // 保存材质，以便修改原始资源
        originalMaterial = soilRenderer.sharedMaterial;
        wetnessMaterial = Instantiate(originalMaterial);
        soilRenderer.material = wetnessMaterial;

        ApplyWetness();
    }

    void Update() {
        // 只在Playing状态下更新湿度
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            return;
        }

        if (tileComp != null && tileComp.data != null) {
            if (tileComp.data.isWatered) {
                SetWetness(1f);
            } else {
                SetWetness(0f);
            }
        }
    }

    public void SetWetness(float value) {
        wetness = Mathf.Clamp01(value);
        ApplyWetness();
    }

    void ApplyWetness() {
        if (wetnessMaterial == null) return;

        wetnessMaterial.SetColor("_BaseColor", Color.Lerp(dryColor, wetColor, wetness));
        wetnessMaterial.SetFloat("_Smoothness", Mathf.Lerp(drySmoothness, wetSmoothness, wetness));
    }
}