using UnityEngine;
using UnityEngine.UI;

public class PieceButton : MonoBehaviour
{
    [SerializeField] PvpController controller;
    [SerializeField] PatternId pattern = PatternId.Dot;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        controller?.SelectPattern(pattern);
    }
}