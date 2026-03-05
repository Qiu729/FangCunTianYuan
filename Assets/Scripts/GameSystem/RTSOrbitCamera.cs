using UnityEngine;

public class RTSOrbitCamera : MonoBehaviour {
    [Header("����Ŀ��")]
    public Vector3 targetPivot = Vector3.zero;

    [Header("�ƶ� (Panning)")]
    public float panSpeed = 20f;
    public float panSmoothTime = 0.1f;
    public LayerMask groundLayer;

    // --- �������������� ---
    [Header("�ƶ����� (Limits)")]
    public bool enablePanLimits = true;
    // ���� X �� Z ��������뾶 (���� X=50 ���������� -50 �� 50 ֮���ƶ�)
    public Vector2 panLimitArea = new Vector2(50, 50);
    // --------------------

    [Header("���� (Zooming)")]
    public float zoomStep = 5f;
    public float minZoom = 2f;
    public float maxZoom = 50f;
    public float zoomSmoothTime = 0.2f;

    [Header("��ת (Orbit)")]
    public float rotationSpeed = 5f;
    public float rotationSmoothTime = 0.1f;
    public float minVerticalAngle = 10f;
    public float maxVerticalAngle = 85f;

    // --- �ڲ����� ---
    private Camera cam;
    private Vector3 _smoothPivot;
    private float _smoothYaw;
    private float _smoothPitch;
    private float _smoothDistance;
    private float _smoothOrthoSize;

    private Vector3 _pivotVel;
    private float _yawVel, _pitchVel, _distVel, _orthoVel;

    private float _targetYaw;
    private float _targetPitch;
    private float _targetDistance;
    private float _targetOrthoSize;

    void Start() {
        cam = GetComponent<Camera>();

        // ��ʼ���Ƕ�
        Vector3 angles = transform.eulerAngles;
        _targetYaw = _smoothYaw = angles.y;
        _targetPitch = _smoothPitch = angles.x;

        // ��ʼ��ê��
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f, groundLayer)) {
            targetPivot = hit.point;
        } else {
            float t = -transform.position.y / transform.forward.y;
            if (t > 0) targetPivot = transform.position + transform.forward * t;
            else targetPivot = Vector3.zero;
        }

        // ��ʼʱҲӦ��һ�����ƣ���ֹ�����ڱ߽���
        ClampTargetPivot();

        _smoothPivot = targetPivot;

        if (cam.orthographic) {
            _targetOrthoSize = _smoothOrthoSize = cam.orthographicSize;
            _targetDistance = _smoothDistance = 50f;
        } else {
            _targetDistance = _smoothDistance = Vector3.Distance(transform.position, targetPivot);
        }
    }

    void LateUpdate() {
        HandleInput();
        CalculateCameraTransform();
    }

    void HandleInput() {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Playing) {
            return;
        }

        float dt = Time.deltaTime;

        // 1. ��ת
        if (Input.GetMouseButton(2) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))) {
            _targetYaw += Input.GetAxis("Mouse X") * rotationSpeed;
            _targetPitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            _targetPitch = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
        }

        // 2. ����
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f) {
            if (cam.orthographic) {
                _targetOrthoSize -= scroll * zoomStep;
                _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, minZoom, maxZoom);
            } else {
                _targetDistance -= scroll * zoomStep;
                _targetDistance = Mathf.Clamp(_targetDistance, minZoom, maxZoom);
            }
        }

        // 3. ƽ��
        if (Input.GetMouseButton(1)) {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
            Vector3 right = transform.right; right.y = 0; right.Normalize();
            Vector3 moveDir = -(forward * mouseY + right * mouseX);

            float zoomFactor = cam.orthographic ? cam.orthographicSize / 10f : _targetDistance / 20f;

            targetPivot += moveDir * panSpeed * zoomFactor * dt;

            // --- �����޸ģ����ƶ���Ӧ������ ---
            ClampTargetPivot();
            // -------------------------------
        }
    }

    // �������߼���ȡΪ�����ķ���
    void ClampTargetPivot() {
        if (!enablePanLimits) return;

        float clampedX = Mathf.Clamp(targetPivot.x, -panLimitArea.x, panLimitArea.x);
        float clampedZ = Mathf.Clamp(targetPivot.z, -panLimitArea.y, panLimitArea.y);

        // ���� Y �᲻�� (ͨ���ǵ���߶�)��ֻ����ˮƽ�ƶ�
        targetPivot = new Vector3(clampedX, targetPivot.y, clampedZ);
    }

    void CalculateCameraTransform() {
        _smoothYaw = Mathf.SmoothDamp(_smoothYaw, _targetYaw, ref _yawVel, rotationSmoothTime);
        _smoothPitch = Mathf.SmoothDamp(_smoothPitch, _targetPitch, ref _pitchVel, rotationSmoothTime);
        _smoothPivot = Vector3.SmoothDamp(_smoothPivot, targetPivot, ref _pivotVel, panSmoothTime);

        Quaternion rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0);

        if (cam.orthographic) {
            _smoothOrthoSize = Mathf.SmoothDamp(_smoothOrthoSize, _targetOrthoSize, ref _orthoVel, zoomSmoothTime);
            cam.orthographicSize = _smoothOrthoSize;
            transform.position = _smoothPivot - (rotation * Vector3.forward * 100f);
        } else {
            _smoothDistance = Mathf.SmoothDamp(_smoothDistance, _targetDistance, ref _distVel, zoomSmoothTime);
            Vector3 offset = Vector3.forward * _smoothDistance;
            transform.position = _smoothPivot - (rotation * offset);
        }

        transform.rotation = rotation;
    }

    // --- ���������ӻ������� ---
    // �� Scene ��ͼ��ѡ�����ʱ������ʾ��ɫ���ƶ����ƿ�
    void OnDrawGizmosSelected() {
        if (enablePanLimits) {
            Gizmos.color = new Color(1, 0, 0, 0.5f); // ��͸����ɫ
            // ������������
            Vector3 center = new Vector3(0, transform.position.y - 10, 0); // ��΢����һ�����⵲ס����
            Vector3 size = new Vector3(panLimitArea.x * 2, 1f, panLimitArea.y * 2);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }

    public void SetFocusPoint(Vector3 newPivot) {
        targetPivot = newPivot;
        ClampTargetPivot(); // �ⲿ����λ��ʱҲҪ�������
    }
}