using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LifeRenderer : MonoBehaviour
{
    [Header("Colors")] [SerializeField] Color aliveColor = Color.white;
    [SerializeField] Color deadColor = Color.black;
    [SerializeField] Color editingAliveColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    SpriteRenderer _spriteRenderer;
    Texture2D _texture;
    Color32[] _buffer;
    LifeGrid _grid;

    bool _editingVisual = false;

    public int GridWidth => _grid?.Width ?? 0;
    public int GridHeight => _grid?.Height ?? 0;

    void EnsureSpriteRenderer()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Init(LifeGrid grid)
    {
        _grid = grid;
        EnsureSpriteRenderer();
        RecreateTexture();
        Redraw();
    }

    public void Redraw()
    {
        if (_grid == null || _texture == null)
        {
            return;
        }

        Color32 alive = _editingVisual ? editingAliveColor : aliveColor;
        Color32 dead = deadColor;

        int texW = _texture.width;
        for (int gy = 0; gy < _grid.Height; gy++)
        {
            int rowStart = gy * texW;
            for (int gx = 0; gx < _grid.Width; gx++)
            {
                _buffer[rowStart + gx] = _grid.Get(gx, gy) ? alive : dead;
            }
        }

        _texture.SetPixels32(_buffer);
        _texture.Apply(false);
    }

    public void OnGridResized()
    {
        RecreateTexture();
        Redraw();
    }

    void RecreateTexture()
    {
        EnsureSpriteRenderer();

        int texW = Mathf.Max(1, _grid.Width);
        int texH = Mathf.Max(1, _grid.Height);

        if (_texture != null)
        {
            Destroy(_texture);
            _texture = null;
        }

        _texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        _texture.filterMode = FilterMode.Point;
        _texture.wrapMode = TextureWrapMode.Clamp;
        _buffer = new Color32[texW * texH];

        var sprite = Sprite.Create(
            _texture,
            new Rect(0, 0, texW, texH),
            new Vector2(0.5f, 0.5f),
            1f
        );

        _spriteRenderer.sprite = sprite;

        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    public bool WorldToCell(Vector3 world, out int gx, out int gy)
    {
        gx = gy = -1;
        if (_grid == null)
        {
            return false;
        }

        var local = transform.InverseTransformPoint(world);
        gx = Mathf.FloorToInt(local.x + _grid.Width * 0.5f);
        gy = Mathf.FloorToInt(local.y + _grid.Height * 0.5f);

        return gx >= 0 && gy >= 0 && gx < _grid.Width && gy < _grid.Height;
    }
}