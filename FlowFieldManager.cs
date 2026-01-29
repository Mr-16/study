using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LargeScaleFlowField
{
    public static class GlobalConstant
    {
        public static float WorldWidth = 256;
        public static float WorldHeight = 256;
        
    }

    public static class GlobalHelper
    {
        public static Vec2I WorldToGrid(Vector3 worldPos, float cellSize)
        {
            int x = (int)MathF.Floor((worldPos.X + GlobalConstant.WorldWidth / 2) / cellSize);
            int y = (int)MathF.Floor((worldPos.Z + GlobalConstant.WorldHeight / 2) / cellSize);
            x = (int)Math.Clamp(x, 0, GlobalConstant.WorldWidth - 1);
            y = (int)Math.Clamp(y, 0, GlobalConstant.WorldHeight - 1);
            return new Vec2I(x, y);
        }
    }

    class Program
    {
        static void Main()
        {
            
            while (true)
            {
                float delta = 3000;
                Task.Delay((int)delta).Wait();
                Console.WriteLine("1f : ");

                Stopwatch sw1 = new Stopwatch();
                sw1.Start();

                //Vector3 targetPos1 = new Vector3(127, 0, 127);
                //FlowField flowField1 = new FlowField(1);
                //flowField1.ComputeFlowField(targetPos1);
                
                sw1.Stop();
                Console.WriteLine($"ComputeFlowField Time: {sw1.ElapsedMilliseconds} ms");


                Stopwatch sw2 = new Stopwatch();
                sw2.Start();

                Vector3 targetPos2 = new Vector3(3, 0, 3);
                FlowField flowField2 = new FlowField(4);
                flowField2.ComputeFlowField(targetPos2);

                sw2.Stop();
                Console.WriteLine($"ComputeFlowField Time: {sw2.ElapsedMilliseconds} ms");



                Stopwatch sw3 = new Stopwatch();
                sw3.Start();

                Task.Run(()=> FlowFieldManager.Instance().RebuildAll());

                sw3.Stop();
                Console.WriteLine($"RebuildAll Time: {sw3.ElapsedMilliseconds} ms");
                
                //Console.WriteLine(FlowFieldManager.Instance().FlowFieldList.Count);
                
                //break;
            }
        }

        
    }

    public struct Vec2I
    {
        public int X;
        public int Y;
        public Vec2I(int x, int y) { X = x; Y = y; }
    }

    public struct FlowFieldCell
    {
        public Vec2I Pos;
        public bool IsObstacle;
        public sbyte FlowDirX; // -1,0,1
        public sbyte FlowDirY; // -1,0,1
        public float Cost;     // 到目标的累计代价
        public FlowFieldCell(Vec2I pos)
        {
            Pos = pos;
            IsObstacle = false;
            FlowDirX = 0;
            FlowDirY = 0;
            Cost = float.MaxValue;
        }
    }

    public class FlowField
    {
        private int _width;
        private int _height;
        private float _cellSize;
        private FlowFieldCell[,] _grid;

        public FlowField(float cellSize)
        {
            this._cellSize = cellSize;
            _width = (int)MathF.Ceiling(GlobalConstant.WorldWidth / cellSize);
            _height = (int)MathF.Ceiling(GlobalConstant.WorldHeight / cellSize);

            _grid = new FlowFieldCell[_width, _height];
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    _grid[x, y] = new FlowFieldCell(new Vec2I(x, y));
        }

        // 8方向偏移
        private static readonly int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] dy = { 1, 1, 1, 0, 0, -1, -1, -1 };
        private static readonly float[] moveCost = { 1.414f, 1, 1.414f, 1, 1, 1.414f, 1, 1.414f };

        public void ComputeFlowField(Vector3 worldTargetPos)
        {
            Vec2I target = GlobalHelper.WorldToGrid(worldTargetPos, _cellSize);

            // 初始化
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y].FlowDirX = 0;
                    _grid[x, y].FlowDirY = 0;
                    _grid[x, y].Cost = float.MaxValue;
                }

            PriorityQueue<Vec2I, float> pq = new PriorityQueue<Vec2I, float>();
            _grid[target.X, target.Y].Cost = 0;
            pq.Enqueue(target, 0);

            while (pq.Count > 0)
            {
                var current = pq.Dequeue();
                ref var currentCell = ref _grid[current.X, current.Y];

                for (int i = 0; i < 8; i++)
                {
                    int nx = current.X + dx[i];
                    int ny = current.Y + dy[i];
                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                    {
                        var neighbor = _grid[nx, ny];
                        if (neighbor.IsObstacle) continue;

                        float newCost = currentCell.Cost + moveCost[i];
                        if (newCost < neighbor.Cost)
                        {
                            neighbor.Cost = newCost;
                            // 方向指向目标
                            neighbor.FlowDirX = (sbyte)Math.Clamp(current.X - nx, -1, 1);
                            neighbor.FlowDirY = (sbyte)Math.Clamp(current.Y - ny, -1, 1);

                            _grid[nx, ny] = neighbor;
                            pq.Enqueue(new Vec2I(nx, ny), newCost);
                        }
                    }
                }
            }
        }

        public Vector3 GetDir(Vector3 worldPos)
        {
            Vec2I cellPos = GlobalHelper.WorldToGrid(worldPos, _cellSize);
            FlowFieldCell cell = _grid[cellPos.X, cellPos.Y];
            Vector3 dir = new Vector3(cell.FlowDirX, 0, cell.FlowDirY);
            //if (dir.sqrMagnitude > 0)
            //    dir.Normalize();
            return dir;
        }

        public void SetObstacle(int x, int y, bool isObstacle)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
                _grid[x, y].IsObstacle = isObstacle;
        }
    }

    public class FlowFieldManager
    {
        private static FlowFieldManager instance = new FlowFieldManager();
        public static FlowFieldManager Instance()
        {
            return instance;
        }
        public List<FlowField> FlowFieldList = new List<FlowField>();
        private FlowFieldManager()
        {
            for (int i = 0; i < 500; i++)
            {
                FlowField flowField = new FlowField(4);
                flowField.ComputeFlowField(new Vector3(1,0,3));
                FlowFieldList.Add(flowField);
            }
        }
        public void RebuildAll()
        {
            for(int i = 0; i < FlowFieldList.Count;i++)
            {
                FlowFieldList[i].ComputeFlowField(new Vector3(1, 0, 3));
            }
        }
        //private float cellSize = 16;
        //private float flowFieldSize = 4;
        //private Dictionary<Vec2I, FlowField> FlowFieldMap = new Dictionary<Vec2I, FlowField>();

        //public FlowField GetFlowField(Vector3 targetWorldPos)
        //{
        //    Vec2I targetGridPos = GlobalHelper.WorldToGrid(targetWorldPos, cellSize);
        //    if (FlowFieldMap.TryGetValue(targetGridPos, out FlowField flowField) == false)
        //    {
        //        flowField = new FlowField(flowFieldSize);
        //        flowField.ComputeFlowField(targetWorldPos);
        //        FlowFieldMap[targetGridPos] = flowField;
        //    }
        //    return flowField;
        //}
    }
}
