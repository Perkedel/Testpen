using Sandbox;

namespace HitShapes;

internal sealed class RectGridHitShape : IHitShape
{
    readonly int _cols;
    readonly int _rows;

    public RectGridHitShape(int cols, int rows)
    {
        _cols = cols;
        _rows = rows;
    }

    public int SlotCount => _cols * _rows;

    public int? Resolve(Vector2 local, Vector2 size)
    {
        if (local.x < 0 || local.y < 0 || local.x >= size.x || local.y >= size.y)
            return null;
        var col = (int)(local.x / (size.x / _cols));
        var row = (int)(local.y / (size.y / _rows));
        if (col >= _cols) col = _cols - 1;
        if (row >= _rows) row = _rows - 1;
        return row * _cols + col;
    }
}
