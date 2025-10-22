using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Image))]
public class ButtonToggleSpriteSwap : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;
    public bool isOn;

    public UnityEvent<bool> onChanged;

    Image _img;

    void Awake()
    {
        _img = GetComponent<Image>();
        Refresh();
    }

    public void Click()
    {
        isOn = !isOn;
        Refresh();
        onChanged?.Invoke(isOn);
    }

    void Refresh()
    {
        if (_img)
        {
            _img.sprite = isOn ? onSprite : offSprite;
        }
    }
}