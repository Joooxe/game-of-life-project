using System.Collections.Generic;
using UnityEngine;

public enum PatternId
{
    Dot,
    Block,
    Blinker,
    Glider,
    LWSS,
    MWSS,
    HWSS,
    Pulsar,
    Pentadecathlon,
    GosperGliderGun,
    Bee,
    Meta1,
    Meta2
}

// Если вы видите этот код - я тоже удивлён
public static class PatternLibrary
{
    public static IReadOnlyList<Vector2Int> GetCells(PatternId id)
    {
        if (_builtIn.TryGetValue(id, out var list)) return list;
        if (_ascii.TryGetValue(id, out var ascii))
            return ParseAsciiCentered(ascii);

        Debug.LogWarning($"Pattern '{id}' не найден. Возвращаю Dot.");
        return _builtIn[PatternId.Dot];
    }

    public static int Cost(PatternId id) => GetCells(id).Count;

    static readonly Dictionary<PatternId, List<Vector2Int>> _builtIn = new()
    {
        { PatternId.Dot, new List<Vector2Int> { new(0, 0) } },

        {
            PatternId.Block, new List<Vector2Int>
                { new(0, 0), new(1, 0), new(0, 1), new(1, 1) }
        },

        {
            PatternId.Blinker, new List<Vector2Int>
                { new(-1, 0), new(0, 0), new(1, 0) }
        },


        {
            PatternId.Glider, new List<Vector2Int>
                { new(1, 0), new(2, 1), new(0, 2), new(1, 2), new(2, 2) }
        },
    };

    static readonly Dictionary<PatternId, string[]> _ascii = new()
    {
        {
            PatternId.LWSS, new[]
            {
                ".O..O",
                "O....",
                "....O",
                "OOOO."
            }
        },

        {
            PatternId.MWSS, new[]
            {
                "..O..O",
                "O.....",
                ".....O",
                "OOOOO.",
                ".O...."
            }
        },

        {
            PatternId.HWSS, new[]
            {
                "..OO..O",
                "O......",
                "......O",
                "OOOOOO.",
                ".O...O."
            }
        },

        {
            PatternId.Pulsar, new[]
            {
                "..OOO...OOO..",
                ".............",
                "O....O.O....O",
                "O....O.O....O",
                "O....O.O....O",
                "..OOO...OOO..",
                ".............",
                "..OOO...OOO..",
                "O....O.O....O",
                "O....O.O....O",
                "O....O.O....O",
                ".............",
                "..OOO...OOO.."
            }
        },

        {
            PatternId.Pentadecathlon, new[]
            {
                "..O..",
                "OO.OO",
                "..O..",
                "..O..",
                "..O..",
                "..O..",
                "..O..",
                "..O..",
                "OO.OO",
                "..O.."
            }
        },

        {
            PatternId.GosperGliderGun, new[]
            {
                "........................O...........",
                "......................O.O...........",
                "............OO......OO............OO",
                "...........O...O....OO............OO",
                "OO........O.....O...OO..............",
                "OO........O...O.OO....O.O...........",
                "..........O.....O.......O...........",
                "...........O...O....................",
                "............OO......................"
            }
        },

        {
            PatternId.Bee, new[]
            {
                ".OO.",
                "O..O",
                ".OO."
            }
        },

        {
            PatternId.Meta1, new[]
            {
                "......O.",
                "....O.OO",
                "....O.O.",
                "....O...",
                "..O.....",
                "O.O....."
            }
        },
        {
            PatternId.Meta2, new[]
            {
                "OOO.O",
                "O....",
                "...OO",
                ".OO.O",
                "O.O.O"
            }
        },
    };

    static List<Vector2Int> ParseAsciiCentered(string[] lines)
    {
        var cells = new List<Vector2Int>(256);
        if (lines == null || lines.Length == 0)
        {
            return cells;
        }

        int h = lines.Length;
        int w = 0;
        for (int i = 0; i < h; i++)
            if (lines[i] != null && lines[i].Length > w)
            {
                w = lines[i].Length;
            }

        for (int y = 0; y < h; y++)
        {
            var row = lines[y] ?? string.Empty;
            for (int x = 0; x < row.Length; x++)
            {
                char c = row[x];
                if (c == 'O' || c == 'o' || c == 'X' || c == '#')
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (cells.Count == 0)
        {
            return cells;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var p in cells)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        int cx = (minX + maxX) / 2;
        int cy = (minY + maxY) / 2;

        for (int i = 0; i < cells.Count; i++)
            cells[i] = new Vector2Int(cells[i].x - cx, cells[i].y - cy);

        return cells;
    }
}