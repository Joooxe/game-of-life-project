using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class NeighborhoodRenderUI : MonoBehaviour
{
    public const int SIZE = 15;

    [SerializeField] Color aliveColor = Color.white;
    [SerializeField] Color aliveGray = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] Color deadColor = new Color(0, 0, 0, 0);

    RawImage _img;
    Texture2D _tex;
    Color32[] _buf;
    RectTransform _rt;

    NeighborhoodData _data;

    public bool GrayMode { get; private set; }

    void Awake()
    {
        _img = GetComponent<RawImage>();
        _rt = GetComponent<RectTransform>();

        _tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        _tex.filterMode = FilterMode.Point;
        _tex.wrapMode = TextureWrapMode.Clamp;

        _buf = new Color32[SIZE * SIZE];
        for (int i = 0; i < _buf.Length; i++) _buf[i] = deadColor;
        _tex.SetPixels32(_buf);
        _tex.Apply(false);

        _img.texture = _tex;
        _img.color = Color.white;
    }

    public void Bind(NeighborhoodData data)
    {
        _data = data;
        Redraw();
    }

    public void SetGrayMode(bool on)
    {
        GrayMode = on;
        Redraw();
    }

    public void Redraw()
    {
        if (_data == null || _tex == null)
        {
            return;
        }

        Color32 alive = (Color32)(GrayMode ? aliveGray : aliveColor);
        Color32 dead = (Color32)deadColor;

        int w = SIZE;
        for (int y = 0; y < SIZE; y++)
        {
            int row = y * w;
            for (int x = 0; x < SIZE; x++)
                _buf[row + x] = _data.Get(x, y) ? alive : dead;
        }

        _tex.SetPixels32(_buf);
        _tex.Apply(false);
    }

    public bool ScreenToCell(Vector2 screenPos, out int gx, out int gy)
    {
        gx = gy = -1;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, screenPos, null, out var local))
            return false;

        Rect r = _rt.rect;
        if (!r.Contains(local))
        {
            return false;
        }

        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        gx = Mathf.Clamp(Mathf.FloorToInt(nx * SIZE), 0, SIZE - 1);
        gy = Mathf.Clamp(Mathf.FloorToInt(ny * SIZE), 0, SIZE - 1);
        return true;
    }
}