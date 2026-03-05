using UnityEngine;

public class DayNightCycle : MonoBehaviour {
    private Light sunLight;

    [Header("����")]
    public float sunRadius = 20f;

    // �򵥵���ɫ���ã�������ڱ༭�����ɫ
    public Color dayColor = new Color(1f, 0.95f, 0.9f); // ����ů��
    public Color duskColor = new Color(1f, 0.5f, 0.2f); // �ƻ�Ⱥ�
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f); //������ҹ��Ҳ��Ҫ���ڣ�������

    // ������ǿ��
    public float dayIntensity;
    public float nightIntensity;

    void Start() {
        sunLight = GetComponent<Light>();
    }

    void Update() {
        if (GameTimeManager.Instance == null) return;

        // 只在Playing状态下更新昼夜循环
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            return;
        }

        WorldTime time = GameTimeManager.Instance.currentTime;

        // 1. ��ת̫��
        // ��Ӧ React: const angle = (sunCycle * Math.PI * 2) - (Math.PI / 2);
        // �� Unity �У�����ֱ�Ӳ��� Transform Rotation
        // 0.0 (0��) -> 270��
        // 0.25 (6��) -> 0��
        // 0.5 (12��) -> 90��
        // 0.75 (18��) -> 180��
        float angle = (time.progress - 0.25f) * 360f;

        // �� X ����תģ����������
        transform.rotation = Quaternion.Euler(angle, 0f, 0f);

        // 2. �ı���ɫ��ǿ��
        UpdateLighting(time.progress);
    }

    void UpdateLighting(float dayProgress) {
        //Debug.Log($"Day Progress: {dayProgress}");
        // �򵥵Ĳ�ֵ�߼�����Ӧ React �е� color logic
        if (dayProgress >= 5 / 24f && dayProgress < 7 / 24f) {
            //Debug.Log("�ճ�");
            sunLight.color = Color.Lerp(nightColor, duskColor, 0.5f);
            sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, 0.5f);
        } else if (dayProgress >= 7 / 24f && dayProgress < 17 / 24f) {
            //Debug.Log("����");
            sunLight.color = Color.Lerp(sunLight.color, dayColor, Time.deltaTime);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, dayIntensity, Time.deltaTime);
        } else if (dayProgress >= 17 / 24f && dayProgress < 19 / 24f) {
            //Debug.Log("����");
            sunLight.color = Color.Lerp(dayColor, duskColor, Time.deltaTime);
            sunLight.intensity = 0.8f;
        } else {
            //Debug.Log("ҹ��");
            sunLight.color = Color.Lerp(duskColor, nightColor, Time.deltaTime);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, nightIntensity, Time.deltaTime);
        }
    }
}