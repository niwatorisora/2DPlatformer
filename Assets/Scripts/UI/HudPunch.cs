using System.Collections;
using UnityEngine;

public class HudPunch : MonoBehaviour
{
    const float Duration = 0.1f;
    static readonly Vector3 PunchScale = Vector3.one * 1.15f;

    Coroutine routine;

    public void Punch()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        transform.localScale = PunchScale;
        yield return new WaitForSecondsRealtime(Duration);
        transform.localScale = Vector3.one;
        routine = null;
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        transform.localScale = Vector3.one;
    }
}
