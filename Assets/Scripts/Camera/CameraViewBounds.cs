using UnityEngine;

/// <summary>
/// 固定表示領域の外周に、物理用の見えない壁を作る。
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public sealed class CameraViewBounds : MonoBehaviour
{
    private const float WallThickness = 1f;
    private const float CornerOverlap = 1f;

    [Tooltip("境界壁に使うレイヤー名。未登録時は壁を作成しない。")]
    [SerializeField] private string boundsLayerName = "ViewBounds";

    private Camera targetCamera;
    private CameraViewportFitter viewportFitter;
    private Transform boundsRoot;
    private bool warnedAboutMissingLayer;

    private void Start()
    {
        targetCamera = GetComponent<Camera>();
        viewportFitter = GetComponent<CameraViewportFitter>();

        if (targetCamera == null || !targetCamera.orthographic)
        {
            GameLog.Warning(this, "CameraViewBounds は Orthographic Camera でのみ使用できます。");
            return;
        }

        int boundsLayer = LayerMask.NameToLayer(boundsLayerName);
        if (boundsLayer < 0)
        {
            if (!warnedAboutMissingLayer)
            {
                warnedAboutMissingLayer = true;
                GameLog.Warning(this,
                    $"レイヤー '{boundsLayerName}' がありません。Tags & Layers で作成するまで ViewBounds は生成しません。");
            }
            return;
        }

        CreateOrUpdateBounds(boundsLayer);
    }

    private void CreateOrUpdateBounds(int boundsLayer)
    {
        // プレイヤーと弾だけを閉じ込め、敵は素通しにするためレイヤーマトリクスで制御する。
        boundsRoot = transform.Find("ViewBounds");
        if (boundsRoot == null)
        {
            var rootObject = new GameObject("ViewBounds");
            boundsRoot = rootObject.transform;
            boundsRoot.SetParent(transform, false);
        }

        float visibleHeight = targetCamera.orthographicSize * 2f;
        float targetAspect = viewportFitter != null ? viewportFitter.TargetAspect : targetCamera.aspect;
        float visibleWidth = visibleHeight * targetAspect;
        float halfWidth = visibleWidth * 0.5f;
        float halfHeight = visibleHeight * 0.5f;
        float halfThickness = WallThickness * 0.5f;

        ConfigureWall("Left", new Vector2(-halfWidth - halfThickness, 0f),
            new Vector2(WallThickness, visibleHeight + CornerOverlap), boundsLayer);
        ConfigureWall("Right", new Vector2(halfWidth + halfThickness, 0f),
            new Vector2(WallThickness, visibleHeight + CornerOverlap), boundsLayer);
        ConfigureWall("Top", new Vector2(0f, halfHeight + halfThickness),
            new Vector2(visibleWidth + CornerOverlap, WallThickness), boundsLayer);
        ConfigureWall("Bottom", new Vector2(0f, -halfHeight - halfThickness),
            new Vector2(visibleWidth + CornerOverlap, WallThickness), boundsLayer);
        FollowCameraAtPhysicsPlane();
    }

    private void LateUpdate()
    {
        if (boundsRoot != null) FollowCameraAtPhysicsPlane();
    }

    private void FollowCameraAtPhysicsPlane()
    {
        // Camera は通常 z=-10 にあるため、物理壁はワールド z=0 を維持しつつ追従させる。
        boundsRoot.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    private void ConfigureWall(string wallName, Vector2 localPosition, Vector2 size, int layer)
    {
        Transform wall = boundsRoot.Find(wallName);
        if (wall == null)
        {
            var wallObject = new GameObject(wallName);
            wall = wallObject.transform;
            wall.SetParent(boundsRoot, false);
        }

        wall.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        wall.localRotation = Quaternion.identity;
        wall.localScale = Vector3.one;
        wall.gameObject.layer = layer;

        BoxCollider2D collider = wall.GetComponent<BoxCollider2D>();
        if (collider == null) collider = wall.gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;
        collider.isTrigger = false;
    }
}
