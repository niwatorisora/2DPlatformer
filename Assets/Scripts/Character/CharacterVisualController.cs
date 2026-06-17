using UnityEngine;

/// <summary>
/// Prefab のルート（Rigidbody2D があるオブジェクト）にアタッチして使う。
/// VisualRoot 子オブジェクトの回転をワールド空間で常に正立（identity）に保つことで、
/// 物理演算によるルートの回転が画像に伝わらないようにする。
/// 移動方向に応じて SpriteRenderer を水平フリップし、左右向きを自動制御する。
///
/// セットアップ:
///   1. キャラクタールート直下に空の子 GameObject "VisualRoot" を作成する。
///   2. SpriteRenderer を VisualRoot に移動する（またはその子に配置する）。
///   3. このコンポーネントをルートにアタッチし、visualRoot フィールドに "VisualRoot" を設定する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterVisualController : MonoBehaviour
{
    [Tooltip("SpriteRenderer を持つ子 Transform。Prefab の 'VisualRoot' を割り当てる。")]
    [SerializeField] Transform visualRoot;

    [Tooltip("フリップ判定に使う最低水平速度。これ未満の速度では向きを変えない。")]
    [SerializeField] float flipSpeedThreshold = 0.1f;

    [Tooltip("スプライトがデフォルトで右向きなら true、左向きなら false に設定する。")]
    [SerializeField] bool defaultFacingRight = true;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (visualRoot != null)
            // 子階層に SpriteRenderer が 1 つあれば自動取得。無ければ無視。
            spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>(true);
    }

    void LateUpdate()
    {
        if (visualRoot == null) return;

        // 物理ボディが回転しても VisualRoot のワールド回転を常に正立に戻す。
        // これによりスプライトが "転がる" 見た目を防ぐ。
        visualRoot.rotation = Quaternion.identity;

        if (rb == null || spriteRenderer == null) return;

        // 閾値を超えた水平速度でのみ向きを更新する（停止時は向きを維持）。
        float vx = rb.linearVelocity.x;
        if (Mathf.Abs(vx) > flipSpeedThreshold)
        {
            bool movingRight = vx > 0f;
            spriteRenderer.flipX = defaultFacingRight ? !movingRight : movingRight;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        flipSpeedThreshold = Mathf.Max(0f, flipSpeedThreshold);
    }
#endif
}
