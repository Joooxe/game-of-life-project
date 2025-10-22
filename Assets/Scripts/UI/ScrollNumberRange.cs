using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ScrollNumberRange : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler,
    IPointerUpHandler
{
    public enum Target
    {
        SMin,
        SMax,
        BMin,
        BMax
    }

    public Target target;
    public RulesRange range;

    TMP_Text _label;
    bool _drag;
    Vector2 _start;
    float _accum;

    const float pixelsPerStep = 10f;

    void Awake()
    {
        _label = GetComponentInChildren<TMP_Text>();
        Refresh();
        if (range)
        {
            range.OnValuesChanged += Refresh;
        }
    }

    void OnDestroy()
    {
        if (range)
        {
            range.OnValuesChanged -= Refresh;
        }
    }

    void Refresh()
    {
        if (!_label || !range)
        {
            return;
        }
        _label.text = range.GetValue(target).ToString();
    }

    void ApplyDelta(int d)
    {
        range.Adjust(target, d);
    }

    public void OnPointerClick(PointerEventData e)
    {
        int dir = e.button == PointerEventData.InputButton.Left ? +1 : -1;
        ApplyDelta(dir);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        _drag = true;
        _start = e.position;
        _accum = 0f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_drag)
        {
            return;
        }
        float dy = e.position.y - _start.y;
        _accum += dy;
        _start = e.position;
        while (Mathf.Abs(_accum) >= pixelsPerStep)
        {
            int dir = _accum > 0 ? +1 : -1;
            ApplyDelta(dir);
            _accum -= dir * pixelsPerStep;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        _drag = false;
    }
}