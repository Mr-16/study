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
    public int Width = 100;
    public int Height = 100;

    public FlowFieldCell[,] Grid;

    // 8方向：上下左右 + 四个对角
    private static readonly (int dx, int dy, int cost)[] Directions = new (int, int, int)[]
    {
        (0, 1, 10),    // 上
        (1, 0, 10),    // 右
        (0, -1, 10),   // 下
        (-1, 0, 10),   // 左
        (1, 1, 14),    // 右上
        (1, -1, 14),   // 右下
        (-1, -1, 14),  // 左下
        (-1, 1, 14)    // 左上
    };

    public FlowField(int width = 100, int height = 100)
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

        // 初始化
        foreach (var cell in Grid)
        {
            cell.Cost = int.MaxValue;
            cell.FlowDirX = 0;
            cell.FlowDirY = 0;
        }

        var targetCell = Grid[targetX, targetY];
        targetCell.Cost = 0;

        // Dijkstra 使用优先队列
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

        // 计算流向
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cell = Grid[x, y];
                if (cell.IsObstacle || cell.Cost == int.MaxValue)
                    continue;

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

    // 调试打印八方向箭头
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
        ff.Grid[4, 4].IsObstacle = true;
        ff.Grid[4, 5].IsObstacle = true;
        ff.ComputeFlowField(9, 9);
        ff.PrintFlow();
    }
}
