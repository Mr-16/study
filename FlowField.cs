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

    public void ComputeFlowField(int targetX, int targetY)
    {
        if (targetX < 0 || targetX >= Width || targetY < 0 || targetY >= Height)
            throw new ArgumentOutOfRangeException("目标坐标超出范围");

        this.targetX = targetX;
        this.targetY = targetY;

        // 初始化所有格子
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
                if (neighbor.IsObstacle)
                    continue;

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

    private void UpdateFlowDirections()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
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

    // 增量更新某个格子的障碍状态
    public void SetObstacle(int x, int y, bool isObstacle)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        var cell = Grid[x, y];
        if (cell.IsObstacle == isObstacle)
            return;

        cell.IsObstacle = isObstacle;

        // 只更新受影响区域
        var pq = new PriorityQueue<FlowFieldCell, int>();

        // 如果新障碍，先清除自己成本
        if (isObstacle)
        {
            cell.Cost = int.MaxValue;
        }
        else
        {
            // 非障碍，重新计算成本
            // 从邻居中找最小成本
            int minCost = int.MaxValue;
            foreach (var (dx, dy, moveCost) in Directions)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                    continue;

                var neighbor = Grid[nx, ny];
                if (neighbor.Cost < int.MaxValue)
                    minCost = Math.Min(minCost, neighbor.Cost + moveCost);
            }

            if (x == targetX && y == targetY) minCost = 0; // 目标格子
            cell.Cost = minCost;
        }

        pq.Enqueue(cell, cell.Cost);

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
                if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                    continue;

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

        UpdateFlowDirections();
    }

    // 打印八方向箭头
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

// 示例测试
public class Program
{
    public static void Main()
    {
        FlowField ff = new FlowField(10, 10);
        ff.ComputeFlowField(9, 9);

        ff.SetObstacle(4, 4, true);
        ff.SetObstacle(4, 5, true);

        ff.PrintFlow();

        Console.WriteLine("清除障碍");
        ff.SetObstacle(4, 4, false);
        ff.PrintFlow();
    }
}
