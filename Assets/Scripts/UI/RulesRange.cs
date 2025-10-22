using System;
using UnityEngine;

public class RulesRange : MonoBehaviour
{
    [Range(0, 255)] public int SMin = 2;
    [Range(0, 255)] public int SMax = 3;
    [Range(0, 255)] public int BMin = 3;
    [Range(0, 255)] public int BMax = 3;

    const int MIN = 0, MAX = 255;

    public event Action OnValuesChanged;

    public int GetValue(ScrollNumberRange.Target t) => t switch
    {
        ScrollNumberRange.Target.SMin => SMin,
        ScrollNumberRange.Target.SMax => SMax,
        ScrollNumberRange.Target.BMin => BMin,
        ScrollNumberRange.Target.BMax => BMax,
        _ => 0
    };

    public void Adjust(ScrollNumberRange.Target t, int delta)
    {
        switch (t)
        {
            case ScrollNumberRange.Target.SMin: SMin += delta; break;
            case ScrollNumberRange.Target.SMax: SMax += delta; break;
            case ScrollNumberRange.Target.BMin: BMin += delta; break;
            case ScrollNumberRange.Target.BMax: BMax += delta; break;
        }

        ClampFix();
        OnValuesChanged?.Invoke();
    }

    void ClampFix()
    {
        SMin = Mathf.Clamp(SMin, MIN, MAX);
        SMax = Mathf.Clamp(SMax, MIN, MAX);
        BMin = Mathf.Clamp(BMin, MIN, MAX);
        BMax = Mathf.Clamp(BMax, MIN, MAX);
        if (SMin > SMax) SMax = SMin;
        if (BMin > BMax) BMax = BMin;
    }

    void OnValidate()
    {
        ClampFix();
        OnValuesChanged?.Invoke();
    }
}