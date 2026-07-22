using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraViewportFitter : MonoBehaviour
{
    [Tooltip("固定表示に使うアスペクト比。例: 16:9")]
    [SerializeField] private Vector2 targetAspect = new Vector2(16f, 9f);

    [Tooltip("常に表示するワールド領域の高さ")]
    [SerializeField] private float visibleWorldHeight = 16f;

    private const string LetterboxCameraName = "LetterboxFillCamera";

    private Camera targetCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    /// <summary>固定表示に使う横:縦の比率。</summary>
    public float TargetAspect => targetAspect.y > 0f ? targetAspect.x / targetAspect.y : 1f;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning("CameraViewportFitter は Orthographic Camera でのみ使用できます。", this);
            enabled = false;
            return;
        }

        targetCamera.orthographicSize = visibleWorldHeight / 2f;
        ConfigureLetterboxFillCamera();
        ApplyRect();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        ApplyRect();
    }

    private void ConfigureLetterboxFillCamera()
    {
        Camera fillCamera = null;
        foreach (Camera childCamera in GetComponentsInChildren<Camera>(true))
        {
            if (childCamera != targetCamera && childCamera.name == LetterboxCameraName)
            {
                fillCamera = childCamera;
                break;
            }
        }

        if (fillCamera == null)
        {
            GameObject fillObject = new GameObject(LetterboxCameraName);
            fillObject.transform.SetParent(transform, false);
            fillCamera = fillObject.AddComponent<Camera>();
        }

        fillCamera.depth = targetCamera.depth - 1f;
        fillCamera.clearFlags = CameraClearFlags.SolidColor;
        fillCamera.backgroundColor = Color.black;
        fillCamera.cullingMask = 0;
        fillCamera.orthographic = true;
        fillCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private void ApplyRect()
    {
        float target = TargetAspect;
        float screenAspect = (float)Screen.width / Screen.height;

        // レベル形状は固定の 16:9 表示を前提に作る。画面比で表示範囲を広げず、
        // 画面外のスポーン地点が見えてしまわないよう余白は黒帯で埋める。
        if (screenAspect > target)
        {
            float width = target / screenAspect;
            targetCamera.rect = new Rect((1f - width) / 2f, 0f, width, 1f);
        }
        else if (screenAspect < target)
        {
            float height = screenAspect / target;
            targetCamera.rect = new Rect(0f, (1f - height) / 2f, 1f, height);
        }
        else
        {
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }
}
