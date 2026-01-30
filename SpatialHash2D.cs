public class SpatialHash2D<T>
{
    private readonly float cellSize;
    private readonly Dictionary<(int, int), List<T>> cells;

    // 物体位置记录，用于删除或更新
    private readonly Dictionary<T, Vector2> objectPositions;

    public SpatialHash2D(float cellSize)
    {
        if (cellSize <= 0)
            throw new ArgumentException("Cell size must be positive");
        this.cellSize = cellSize;
        cells = new Dictionary<(int, int), List<T>>();
        objectPositions = new Dictionary<T, Vector2>();
    }

    /// <summary>
    /// 将物体插入空间哈希
    /// </summary>
    public void Insert(T obj, Vector2 position)
    {
        var key = GetCellKey(position);

        if (!cells.TryGetValue(key, out var list))
        {
            list = new List<T>();
            cells[key] = list;
        }

        list.Add(obj);
        objectPositions[obj] = position;
    }

    /// <summary>
    /// 移除物体
    /// </summary>
    public void Remove(T obj)
    {
        if (!objectPositions.TryGetValue(obj, out var pos))
            return;

        var key = GetCellKey(pos);

        if (cells.TryGetValue(key, out var list))
        {
            list.Remove(obj);
            if (list.Count == 0)
                cells.Remove(key);
        }

        objectPositions.Remove(obj);
    }

    /// <summary>
    /// 更新物体位置
    /// </summary>
    public void Update(T obj, Vector2 newPos)
    {
        Remove(obj);
        Insert(obj, newPos);
    }

    /// <summary>
    /// 查询当前位置附近的所有物体
    /// </summary>
    public IEnumerable<T> QueryNearby(Vector2 position, float radius)
    {
        int minX = (int)Math.Floor((position.X - radius) / cellSize);
        int maxX = (int)Math.Floor((position.X + radius) / cellSize);
        int minY = (int)Math.Floor((position.Y - radius) / cellSize);
        int maxY = (int)Math.Floor((position.Y + radius) / cellSize);

        HashSet<T> result = new HashSet<T>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (cells.TryGetValue((x, y), out var list))
                {
                    foreach (var obj in list)
                    {
                        // 可选：精确距离过滤
                        if (Vector2.Distance(objectPositions[obj], position) <= radius)
                            result.Add(obj);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 根据世界坐标获取 cell key
    /// </summary>
    private (int, int) GetCellKey(Vector2 position)
    {
        int x = (int)Math.Floor(position.X / cellSize);
        int y = (int)Math.Floor(position.Y / cellSize);
        return (x, y);
    }
}
