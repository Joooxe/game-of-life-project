using System;

// Однажды Эрнес Хэмингуэй пытался сделать тут всё битвайз, но у него не получилось. У меня тоже.
public class LifeGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    bool[] _current;
    bool[] _next;

    int _sMin = 2, _sMax = 3;
    int _bMin = 3, _bMax = 3;

    int[] _dxs = Array.Empty<int>();
    int[] _dys = Array.Empty<int>();
    int _kCount;

    public LifeGrid(int width, int height)
    {
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _current = new bool[Width * Height];
        _next = new bool[Width * Height];
    }

    public bool Get(int x, int y) => _current[y * Width + x];
    public void Set(int x, int y, bool alive) => _current[y * Width + x] = alive;
    public void Clear() => Array.Fill(_current, false);

    public void Randomize(float aliveProbability, Random rng)
    {
        aliveProbability = Math.Clamp(aliveProbability, 0f, 1f);
        for (int i = 0; i < _current.Length; i++)
            _current[i] = rng.NextDouble() < aliveProbability;
    }

    public void SetRules(int sMin, int sMax, int bMin, int bMax)
    {
        _sMin = sMin;
        _sMax = sMax;
        _bMin = bMin;
        _bMax = bMax;
    }

    public void SetKernel(int[] dxs, int[] dys, int count)
    {
        _dxs = dxs ?? Array.Empty<int>();
        _dys = dys ?? Array.Empty<int>();
        _kCount = Math.Max(0, Math.Min(count, Math.Min(_dxs.Length, _dys.Length)));
    }

    public void Step()
    {
        for (int y = 0; y < Height; y++)
        {
            int rowIdx = y * Width;
            for (int x = 0; x < Width; x++)
            {
                int i = rowIdx + x;
                bool alive = _current[i];

                int n = 0;
                for (int k = 0; k < _kCount; k++)
                {
                    int nx = x + _dxs[k];
                    int ny = y + _dys[k];
                    if ((uint)nx >= (uint)Width || (uint)ny >= (uint)Height)
                    {
                        continue;
                    }

                    if (_current[ny * Width + nx])
                    {
                        n++;
                    }
                }

                bool nextAlive =
                    (alive && n >= _sMin && n <= _sMax) ||
                    (!alive && n >= _bMin && n <= _bMax);

                _next[i] = nextAlive;
            }
        }

        var tmp = _current;
        _current = _next;
        _next = tmp;
    }
}