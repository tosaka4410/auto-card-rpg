using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleCardAnim : MonoBehaviour
{
    [SerializeField] private Image highlight; // 任意：薄い光の画像。なければnullでもOK
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float duration = 0.18f;

    private RectTransform rt;
    private Vector3 baseScale;
    private Coroutine co;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt != null ? rt.localScale : Vector3.one;
        SetPicked(false);
    }

    public void SetPicked(bool on)
    {
        if (highlight != null) highlight.enabled = on;
    }

    public void PlayPicked()
    {
        SetPicked(true);

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        if (rt == null) yield break;

        rt.localScale = baseScale * popScale;
        yield return new WaitForSeconds(duration);
        rt.localScale = baseScale;

        // ハイライトは少し残しても良い。すぐ消すなら↓
        // SetPicked(false);
    }
}
