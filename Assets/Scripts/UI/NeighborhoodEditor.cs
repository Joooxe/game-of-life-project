using UnityEngine;
using UnityEngine.InputSystem;

public class NeighborhoodEditor : MonoBehaviour
{
    [SerializeField] NeighborhoodRenderUI renderUI;

    NeighborhoodData _data = new NeighborhoodData();
    public NeighborhoodData Data => _data;

    const int cen = NeighborhoodData.cen;

    [Header("Размер фигур")] [SerializeField]
    int maxSize = NeighborhoodData.Size;

    [SerializeField] int minSize = 3;
    [SerializeField] float shapeResetDelay = 1.5f;

    [Header("Defaults")] [SerializeField] bool seedConwayOnStart = true;

    int _drawSize;
    int _eraseSize;

    float _lastDrawTime = -999f;
    float _lastEraseTime = -999f;

    bool _lmbArmed, _rmbArmed;
    int _lastLX = -1, _lastLY = -1;
    int _lastRX = -1, _lastRY = -1;

    void Awake()
    {
        _drawSize = minSize;
        _eraseSize = minSize;

        _data.Active = true;

        if (renderUI)
        {
            renderUI.Bind(_data);
            renderUI.SetGrayMode(!_data.Active);
        }
    }

    void Start()
    {
        if (seedConwayOnStart && _data.ActiveCount == 0)
        {
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };
            for (int i = 0; i < dx.Length; i++)
                _data.Set(cen + dx[i], cen + dy[i], true);

            if (renderUI)
            {
                renderUI.Redraw();
            }
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 mp = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _lmbArmed = renderUI.ScreenToCell(mp, out _, out _);
            _lastLX = _lastLY = -1;
        }

        if (_lmbArmed && mouse.leftButton.isPressed)
        {
            if (renderUI.ScreenToCell(mp, out int x, out int y))
            {
                if (!(x == cen && y == cen) && (x != _lastLX || y != _lastLY))
                {
                    _data.Set(x, y, true);
                    renderUI.Redraw();
                    _lastLX = x;
                    _lastLY = y;
                }
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            _lmbArmed = false;
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            _rmbArmed = renderUI.ScreenToCell(mp, out _, out _);
            _lastRX = _lastRY = -1;
        }

        if (_rmbArmed && mouse.rightButton.isPressed)
        {
            if (renderUI.ScreenToCell(mp, out int x, out int y))
            {
                if (!(x == cen && y == cen) && (x != _lastRX || y != _lastRY))
                {
                    _data.Set(x, y, false);
                    renderUI.Redraw();
                    _lastRX = x;
                    _lastRY = y;
                }
            }
        }

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            _rmbArmed = false;
        }
    }

    public void BtnClear()
    {
        _data.Clear();
        _drawSize = _eraseSize = minSize;
        if (renderUI)
        {
            renderUI.Redraw();
        }
    }

    public void BtnRandom()
    {
        var rng = new System.Random();
        for (int y = 0; y < NeighborhoodData.Size; y++)
        for (int x = 0; x < NeighborhoodData.Size; x++)
            if (!(x == cen && y == cen))
            {
                _data.Set(x, y, rng.NextDouble() < 0.25);
            }

        if (renderUI)
        {
            renderUI.Redraw();
        }
    }

    public void BtnToggleGray()
    {
        _data.Active = !_data.Active;
        if (renderUI)
        {
            renderUI.SetGrayMode(!_data.Active);
        }
    }

    public void SquareLeft()
    {
        ShapeLeft(DrawHollowSquare);
    }

    public void SquareRight()
    {
        ShapeRight(DrawHollowSquare);
    }

    public void CircleLeft()
    {
        ShapeLeft(DrawHollowCircle);
    }

    public void CircleRight()
    {
        ShapeRight(DrawHollowCircle);
    }

    delegate void ShapeFn(int cx, int cy, int size, bool val);

    void ShapeLeft(ShapeFn fn)
    {
        if (Time.time - _lastDrawTime > shapeResetDelay)
        {
            _drawSize = minSize;
        }
        fn(cen, cen, _drawSize, true);
        _drawSize = Mathf.Min(_drawSize + 1, maxSize);
        _lastDrawTime = Time.time;
        if (renderUI)
        {
            renderUI.Redraw();
        }
    }

    void ShapeRight(ShapeFn fn)
    {
        if (Time.time - _lastEraseTime > shapeResetDelay)
        {
            _eraseSize = minSize;
        }
        fn(cen, cen, _eraseSize, false);
        _eraseSize = Mathf.Min(_eraseSize + 1, maxSize);
        _lastEraseTime = Time.time;
        if (renderUI)
        {
            renderUI.Redraw();
        }
    }

    void SetSafe(int x, int y, bool v)
    {
        if ((uint)x >= NeighborhoodData.Size || (uint)y >= NeighborhoodData.Size)
        {
            return;
        }

        if (x == cen && y == cen)
        {
            return;
        }
        _data.Set(x, y, v);
    }

    void DrawHollowSquare(int cx, int cy, int size, bool v)
    {
        size = Mathf.Max(minSize, size);
        int r = Mathf.Max(1, size - 2);
        for (int dx = -r; dx <= r; dx++)
        {
            SetSafe(cx + dx, cy - r, v);
            SetSafe(cx + dx, cy + r, v);
        }

        for (int dy = -r; dy <= r; dy++)
        {
            SetSafe(cx - r, cy + dy, v);
            SetSafe(cx + r, cy + dy, v);
        }
    }

    void DrawHollowCircle(int cx, int cy, int size, bool v)
    {
        size = Mathf.Max(minSize, size);
        float r = (size - 1) * 0.5f;
        int max = Mathf.CeilToInt(r);
        for (int x = 0; x <= max; x++)
        {
            float yf = Mathf.Sqrt(Mathf.Max(0f, r * r - x * x));
            int y = Mathf.RoundToInt(yf);
            Plot8(cx, cy, x, y, v);
        }
    }

    void Plot8(int cx, int cy, int x, int y, bool v)
    {
        SetSafe(cx + x, cy + y, v);
        SetSafe(cx + x, cy - y, v);
        SetSafe(cx - x, cy + y, v);
        SetSafe(cx - x, cy - y, v);
        SetSafe(cx + y, cy + x, v);
        SetSafe(cx + y, cy - x, v);
        SetSafe(cx - y, cy + x, v);
        SetSafe(cx - y, cy - x, v);
    }
}