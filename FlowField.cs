using System;
using System.Collections.Generic;

public class FlowFieldCell
{
    public int X;
    public int Y;
    public int FlowDirX;
    public int FlowDirY;
    public int Cost;
    public bool IsObstacle;

    public FlowFieldCell(int x, int y)
    {
        X = x;
        Y = y;
        FlowDirX = 0;
        FlowDirY = 0;
        Cost = int.MaxValue;
        IsObstacle = false;
    }
}

public class FlowField
{
    public int Width;
    public int Height;
    public FlowFieldCell[,] Grid;

    private static readonly (int dx, int dy, int cost)[] Directions = new (int, int, int)[]
    {
        (0, 1, 10), (1, 0, 10), (0, -1, 10), (-1, 0, 10),
        (1, 1, 14), (1, -1, 14), (-1, -1, 14), (-1, 1, 14)
    };

    private int targetX, targetY;

    public FlowField(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new FlowFieldCell[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Grid[x, y] = new FlowFieldCell(x, y);
    }

    // 初始化全局流场
    public void ComputeFlowField(int targetX, int targetY)
    {
        this.targetX = targetX;
        this.targetY = targetY;

        foreach (var cell in Grid)
        {
            cell.Cost = int.MaxValue;
            cell.FlowDirX = 0;
            cell.FlowDirY = 0;
        }

        var targetCell = Grid[targetX, targetY];
        targetCell.Cost = 0;

        var pq = new PriorityQueue<FlowFieldCell, int>();
        pq.Enqueue(targetCell, 0);

        while (pq.Count > 0)
        {
            var cell = pq.Dequeue();
            int x = cell.X;
            int y = cell.Y;
            int currentCost = cell.Cost;

            foreach (var (dx, dy, moveCost) in Directions)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                    continue;

                var neighbor = Grid[nx, ny];
                if (neighbor.IsObstacle) continue;

                int newCost = currentCost + moveCost;
                if (newCost < neighbor.Cost)
                {
                    neighbor.Cost = newCost;
                    pq.Enqueue(neighbor, newCost);
                }
            }
        }

        UpdateFlowDirections();
    }

    private void UpdateFlowDirections(int minX = 0, int minY = 0, int maxX = -1, int maxY = -1)
    {
        if (maxX == -1) maxX = Width - 1;
        if (maxY == -1) maxY = Height - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var cell = Grid[x, y];
                if (cell.IsObstacle || cell.Cost == int.MaxValue)
                {
                    cell.FlowDirX = 0;
                    cell.FlowDirY = 0;
                    continue;
                }

                int minCost = cell.Cost;
                int dirX = 0, dirY = 0;

                foreach (var (dx, dy, _) in Directions)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                        continue;

                    var neighbor = Grid[nx, ny];
                    if (neighbor.Cost < minCost)
                    {
                        minCost = neighbor.Cost;
                        dirX = dx;
                        dirY = dy;
                    }
                }

                cell.FlowDirX = dirX;
                cell.FlowDirY = dirY;
            }
        }
    }

    // 高效局部更新障碍
    public void SetObstacle(int x, int y, bool isObstacle, int radius = 10)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;

        var cell = Grid[x, y];
        if (cell.IsObstacle == isObstacle) return;

        cell.IsObstacle = isObstacle;

        int minX = Math.Max(0, x - radius);
        int maxX = Math.Min(Width - 1, x + radius);
        int minY = Math.Max(0, y - radius);
        int maxY = Math.Min(Height - 1, y + radius);

        // 初始化局部成本
        for (int ix = minX; ix <= maxX; ix++)
            for (int iy = minY; iy <= maxY; iy++)
                if (!Grid[ix, iy].IsObstacle)
                    Grid[ix, iy].Cost = int.MaxValue;

        // 局部优先队列更新
        var pq = new PriorityQueue<FlowFieldCell, int>();
        var targetCell = Grid[targetX, targetY];
        if (targetCell.X >= minX && targetCell.X <= maxX && targetCell.Y >= minY && targetCell.Y <= maxY)
        {
            targetCell.Cost = 0;
            pq.Enqueue(targetCell, 0);
        }
        else
        {
            // 将边界非障碍格子加入队列，保证局部连通
            for (int ix = minX; ix <= maxX; ix++)
            {
                for (int iy = minY; iy <= maxY; iy++)
                {
                    var c = Grid[ix, iy];
                    if (!c.IsObstacle)
                    {
                        foreach (var (dx, dy, moveCost) in Directions)
                        {
                            int nx = ix + dx;
                            int ny = iy + dy;
                            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;
                            var neighbor = Grid[nx, ny];
                            if (!neighbor.IsObstacle && neighbor.Cost < int.MaxValue)
                            {
                                c.Cost = neighbor.Cost + moveCost;
                                pq.Enqueue(c, c.Cost);
                                break;
                            }
                        }
                    }
                }
            }
        }

        while (pq.Count > 0)
        {
            var c = pq.Dequeue();
            int cx = c.X;
            int cy = c.Y;
            int curCost = c.Cost;

            foreach (var (dx, dy, moveCost) in Directions)
            {
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx < minX || nx > maxX || ny < minY || ny > maxY) continue;

                var neighbor = Grid[nx, ny];
                if (neighbor.IsObstacle) continue;

                int newCost = curCost + moveCost;
                if (newCost < neighbor.Cost)
                {
                    neighbor.Cost = newCost;
                    pq.Enqueue(neighbor, newCost);
                }
            }
        }

        UpdateFlowDirections(minX, minY, maxX, maxY);
    }

    public void PrintFlow()
    {
        for (int y = Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                var cell = Grid[x, y];
                if (cell.IsObstacle)
                {
                    Console.Write(" X ");
                    continue;
                }

                string dir = (cell.FlowDirX, cell.FlowDirY) switch
                {
                    (0, 1) => " ↑ ",
                    (1, 0) => " → ",
                    (0, -1) => " ↓ ",
                    (-1, 0) => " ← ",
                    (1, 1) => " ↗ ",
                    (1, -1) => " ↘ ",
                    (-1, -1) => " ↙ ",
                    (-1, 1) => " ↖ ",
                    (0,0) => " . ",
                    _ => " ? "
                };
                Console.Write(dir);
            }
            Console.WriteLine();
        }
    }
}

// 测试示例
public class Program
{
    public static void Main()
    {
        FlowField ff = new FlowField(20, 20);
        ff.ComputeFlowField(19, 19);

        ff.SetObstacle(10, 10, true, radius: 5);
        ff.SetObstacle(11, 10, true, radius: 5);

        Console.WriteLine("局部障碍更新后：");
        ff.PrintFlow();

        Console.WriteLine("清除障碍后局部更新：");
        ff.SetObstacle(10, 10, false, radius: 5);
        ff.PrintFlow();
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Numerics; // Vector2

public partial class Unit : Node3D
{
    [Export] public float MoveSpeed = 5f;
    [Export] public float CellSize = 1f;
    [Export] public float Radius = 0.5f;        // 单位半径
    [Export] public float NeighborDist = 3f;    // RVO 邻居检测距离
    [Export] public float TimeHorizon = 1.0f;   // RVO 预测时间

    public FlowField FlowFieldRef;
    public Vector3 Position3D
    {
        get => GlobalPosition;
        set => GlobalPosition = value;
    }

    private Vector2 velocity = Vector2.Zero;

    public override void _Process(double delta)
    {
        if (FlowFieldRef == null) return;

        // 1. 期望速度
        Vector2 desiredDir = GetFlowDirection(Position3D.X, Position3D.Z);
        Vector2 vDesired = desiredDir * MoveSpeed;

        // 2. 获取邻居
        List<Unit> neighbors = GetNearbyUnits();

        // 3. RVO 计算安全速度
        Vector2 vSafe = ComputeRVO(vDesired, neighbors, (float)delta);

        // 4. 移动单位
        velocity = vSafe;
        Position3D += new Vector3(velocity.X, 0, velocity.Y) * (float)delta;
    }

    private Vector2 GetFlowDirection(float worldX, float worldZ)
    {
        float gx = worldX / CellSize;
        float gz = worldZ / CellSize;

        int x0 = (int)Mathf.Floor(gx);
        int z0 = (int)Mathf.Floor(gz);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        x0 = Mathf.Clamp(x0, 0, FlowFieldRef.Width - 1);
        x1 = Mathf.Clamp(x1, 0, FlowFieldRef.Width - 1);
        z0 = Mathf.Clamp(z0, 0, FlowFieldRef.Height - 1);
        z1 = Mathf.Clamp(z1, 0, FlowFieldRef.Height - 1);

        float tx = gx - x0;
        float tz = gz - z0;

        Vector2 f00 = new Vector2(FlowFieldRef.Grid[x0, z0].FlowDirX, FlowFieldRef.Grid[x0, z0].FlowDirY);
        Vector2 f10 = new Vector2(FlowFieldRef.Grid[x1, z0].FlowDirX, FlowFieldRef.Grid[x1, z0].FlowDirY);
        Vector2 f01 = new Vector2(FlowFieldRef.Grid[x0, z1].FlowDirX, FlowFieldRef.Grid[x0, z1].FlowDirY);
        Vector2 f11 = new Vector2(FlowFieldRef.Grid[x1, z1].FlowDirX, FlowFieldRef.Grid[x1, z1].FlowDirY);

        Vector2 f0 = f00 * (1 - tx) + f10 * tx;
        Vector2 f1 = f01 * (1 - tx) + f11 * tx;
        Vector2 f = f0 * (1 - tz) + f1 * tz;

        if (f.LengthSquared() > 0)
            f = Vector2.Normalize(f);

        return f;
    }

    private List<Unit> GetNearbyUnits()
    {
        List<Unit> neighbors = new List<Unit>();
        foreach (var u in GetTree().GetNodesInGroup("units"))
        {
            if (u == this) continue;
            var other = u as Unit;
            if (other == null) continue;

            Vector2 diff = new Vector2(other.Position3D.X - Position3D.X, other.Position3D.Z - Position3D.Z);
            if (diff.Length() < NeighborDist)
                neighbors.Add(other);
        }
        return neighbors;
    }

    private Vector2 ComputeRVO(Vector2 desiredVelocity, List<Unit> neighbors, float delta)
    {
        Vector2 newVel = desiredVelocity;

        foreach (var neighbor in neighbors)
        {
            Vector2 posA = new Vector2(Position3D.X, Position3D.Z);
            Vector2 posB = new Vector2(neighbor.Position3D.X, neighbor.Position3D.Z);

            Vector2 relPos = posB - posA;
            Vector2 relVel = newVel - new Vector2(neighbor.velocity.X, neighbor.velocity.Y);

            float dist = relPos.Length();
            float combinedRadius = Radius + neighbor.Radius;

            // 时间预测
            float t = TimeHorizon;

            // 简化 VO 检测
            Vector2 w = relVel - relPos / t;
            float wLen = w.Length();
            if (wLen < combinedRadius / t)
            {
                // 调整速度远离邻居，比例为距离缺口
                Vector2 avoidDir = Vector2.Normalize(-relPos);
                float strength = (combinedRadius - dist) / combinedRadius;
                newVel += avoidDir * MoveSpeed * strength;
            }
        }

        // 限制最大速度
        if (newVel.Length() > MoveSpeed)
            newVel = Vector2.Normalize(newVel) * MoveSpeed;

        return newVel;
    }
}
