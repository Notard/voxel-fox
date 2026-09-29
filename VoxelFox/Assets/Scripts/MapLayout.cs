using System;
using System.Collections.Generic;
using UnityEngine;

// 맵 레이아웃 문자열을 칸 목록으로 바꾼다.
// 첫 줄이 가장 먼 줄(z 최대)이고, 칸 좌표는 (x, z)다.
//   S 시작 · H 구멍 · C 아이템 · . 일반 타일
public class MapLayout
{
    public const char Start = 'S';
    public const char Hole = 'H';
    public const char Coin = 'C';
    public const char Ground = '.';

    public int Width { get; private set; }
    public int Depth { get; private set; }
    public Vector2Int StartCell { get; private set; }
    public readonly List<Vector2Int> Tiles = new();  // 바닥이 있는 칸 (S, C 포함)
    public readonly List<Vector2Int> Holes = new();
    public readonly List<Vector2Int> Coins = new();

    public static MapLayout Parse(string[] rows)
    {
        if (rows == null || rows.Length == 0)
            throw new ArgumentException("레이아웃이 비어 있음");

        var layout = new MapLayout { Width = rows[0].Length, Depth = rows.Length };
        int starts = 0;
        for (int row = 0; row < rows.Length; row++)
        {
            if (rows[row].Length != layout.Width)
                throw new ArgumentException($"{row + 1}번째 줄 길이가 다름: \"{rows[row]}\"");

            int z = rows.Length - 1 - row;
            for (int x = 0; x < layout.Width; x++)
            {
                var cell = new Vector2Int(x, z);
                switch (rows[row][x])
                {
                    case Ground: layout.Tiles.Add(cell); break;
                    case Hole: layout.Holes.Add(cell); break;
                    case Coin: layout.Tiles.Add(cell); layout.Coins.Add(cell); break;
                    case Start: layout.Tiles.Add(cell); layout.StartCell = cell; starts++; break;
                    default: throw new ArgumentException($"알 수 없는 문자 '{rows[row][x]}' ({x}, {z})");
                }
            }
        }
        if (starts != 1)
            throw new ArgumentException($"시작 지점(S)은 1개여야 함: {starts}개");
        return layout;
    }
}
