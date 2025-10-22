using System;
using System.Collections.Generic;
using Unity.Mathematics;

public class NeighborhoodData
{
    public const int Size = 15;
    public const int cen = Size / 2;

    ulong _tl, _tr, _bl, _br;
    bool _active = true;

    bool _dirty = true;
    int[] _dxs = Array.Empty<int>();
    int[] _dys = Array.Empty<int>();
    int _count = 0;

    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            Changed();
        }
    }

    public event Action OnChanged;

    public void Clear()
    {
        _tl = _tr = _bl = _br = 0UL;
        Changed();
    }

    public bool Get(int x, int y)
    {
        if ((uint)x >= Size || (uint)y >= Size) return false;
        int quad;
        int bit = BitIndex(x, y, out quad);
        ulong mask = 1UL << bit;
        return quad switch
        {
            0 => (_tl & mask) != 0,
            1 => (_tr & mask) != 0,
            2 => (_bl & mask) != 0,
            3 => (_br & mask) != 0,
            _ => false
        };
    }

    public void Set(int x, int y, bool v)
    {
        if ((uint)x >= Size || (uint)y >= Size) return;
        int quad;
        int bit = BitIndex(x, y, out quad);
        ulong mask = 1UL << bit;

        switch (quad)
        {
            case 0:
                if (v) _tl |= mask;
                else _tl &= ~mask;
                break;
            case 1:
                if (v) _tr |= mask;
                else _tr &= ~mask;
                break;
            case 2:
                if (v) _bl |= mask;
                else _bl &= ~mask;
                break;
            case 3:
                if (v) _br |= mask;
                else _br &= ~mask;
                break;
        }

        Changed();
    }

    public int ActiveCount
    {
        get
        {
            ulong tlNoCenter = _tl & ~(1UL << 63);
            int cnt = math.countbits((uint)tlNoCenter) + math.countbits((uint)(tlNoCenter >> 32))
                                                       + math.countbits((uint)_tr) + math.countbits((uint)(_tr >> 32))
                                                       + math.countbits((uint)_bl) + math.countbits((uint)(_bl >> 32))
                                                       + math.countbits((uint)_br) + math.countbits((uint)(_br >> 32));
            return cnt;
        }
    }

    public void GetOffsets(out int[] dxs, out int[] dys, out int count)
    {
        if (!_active)
        {
            dxs = Array.Empty<int>();
            dys = Array.Empty<int>();
            count = 0;
            return;
        }

        if (_dirty) RebuildOffsetsCache();
        dxs = _dxs;
        dys = _dys;
        count = _count;
    }

    void RebuildOffsetsCache()
    {
        List<int> dx = new List<int>(224);
        List<int> dy = new List<int>(224);

        void emit(int x, int y)
        {
            if (x == cen && y == cen) return;
            dx.Add(x - cen);
            dy.Add(y - cen);
        }

        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            if (((_tl >> (y * 8 + x)) & 1UL) != 0)
                emit(x, y);

        // x: (8, 14)
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 7; x++)
            if (((_tr >> (y * 8 + x)) & 1UL) != 0)
                emit(8 + x, y);

        // y: (8, 14)
        for (int y = 0; y < 7; y++)
        for (int x = 0; x < 8; x++)
            if (((_bl >> (y * 8 + x)) & 1UL) != 0)
                emit(x, 8 + y);

        for (int y = 0; y < 7; y++)
        for (int x = 0; x < 7; x++)
            if (((_br >> (y * 8 + x)) & 1UL) != 0)
                emit(8 + x, 8 + y);

        _dxs = dx.ToArray();
        _dys = dy.ToArray();
        _count = _dxs.Length;
        _dirty = false;
    }

    int BitIndex(int x, int y, out int quad)
    {
        if (y < 8)
        {
            if (x < 8)
            {
                quad = 0;
                return y * 8 + x;
            }
            else
            {
                quad = 1;
                return y * 8 + (x - 8);
            }
        }
        else
        {
            if (x < 8)
            {
                quad = 2;
                return (y - 8) * 8 + x;
            }
            else
            {
                quad = 3;
                return (y - 8) * 8 + (x - 8);
            }
        }
    }

    void Changed()
    {
        _dirty = true;
        OnChanged?.Invoke();
    }
}