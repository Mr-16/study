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
