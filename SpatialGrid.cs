using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace rogue_like;

public class SpatialGrid
{
    private readonly int _cellSize;
    private readonly int _cols;
    private readonly int _rows;
    private readonly Dictionary<int, List<Enemy>> _cells = new();

    public SpatialGrid(int mapWidth, int mapHeight, int cellSize)
    {
        _cellSize = cellSize;
        _cols = (int)Math.Ceiling((float)mapWidth / cellSize);
        _rows = (int)Math.Ceiling((float)mapHeight / cellSize);
    }

    public void Clear()
    {
        foreach (var cell in _cells.Values) cell.Clear();
    }

    public void Add(Enemy enemy)
    {
        int col = (int)(MathHelper.Clamp(enemy.Position.X, 0, _cols * _cellSize - 1) / _cellSize);
        int row = (int)(MathHelper.Clamp(enemy.Position.Y, 0, _rows * _cellSize - 1) / _cellSize);
        int key = col + row * _cols;

        if (!_cells.TryGetValue(key, out var cell))
        {
            cell = new List<Enemy>(50); // Prealokacja pod Garbage Collectora
            _cells[key] = cell;
        }
        cell.Add(enemy);
    }

    public void GetNearby(Enemy enemy, List<Enemy> results)
    {
        results.Clear();
        int col = (int)(MathHelper.Clamp(enemy.Position.X, 0, _cols * _cellSize - 1) / _cellSize);
        int row = (int)(MathHelper.Clamp(enemy.Position.Y, 0, _rows * _cellSize - 1) / _cellSize);

        for (int y = row - 1; y <= row + 1; y++)
        {
            for (int x = col - 1; x <= col + 1; x++)
            {
                if (x < 0 || x >= _cols || y < 0 || y >= _rows) continue;
                int key = x + y * _cols;
                
                if (_cells.TryGetValue(key, out var cell))
                {
                    // Używamy for zamiast AddRange, żeby uniknąć jakiejkolwiek alokacji
                    for (int i = 0; i < cell.Count; i++)
                    {
                        results.Add(cell[i]);
                    }
                }
            }
        }
    }
    
    public void GetNearby(Vector2 position, List<Enemy> results)
    {
        results.Clear();
        int col = (int)(MathHelper.Clamp(position.X, 0, _cols * _cellSize - 1) / _cellSize);
        int row = (int)(MathHelper.Clamp(position.Y, 0, _rows * _cellSize - 1) / _cellSize);

        for (int y = row - 1; y <= row + 1; y++)
        {
            for (int x = col - 1; x <= col + 1; x++)
            {
                if (x < 0 || x >= _cols || y < 0 || y >= _rows) continue;
                int key = x + y * _cols;
                
                if (_cells.TryGetValue(key, out var cell))
                {
                    for (int i = 0; i < cell.Count; i++)
                    {
                        results.Add(cell[i]);
                    }
                }
            }
        }
    }
}