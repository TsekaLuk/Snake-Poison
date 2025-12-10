using System.Collections.Generic;
using UnityEngine;
using SnakePoison.Domain.Poison;

namespace SnakePoison.Domain.World
{
    /// <summary>
    /// 世界网格 - 可演化的游戏世界
    /// "地图会根据玩家习惯自动变化"
    /// </summary>
    public class WorldGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public WorldStyle Style { get; private set; }
        public float Difficulty { get; private set; }
        
        private CellType[,] _cells;
        private List<FoodItem> _foodItems;
        private List<Obstacle> _obstacles;

        public WorldGrid(int width, int height, WorldStyle style = WorldStyle.Minimal)
        {
            Width = width;
            Height = height;
            Style = style;
            Difficulty = 0.5f;
            
            _cells = new CellType[width, height];
            _foodItems = new List<FoodItem>();
            _obstacles = new List<Obstacle>();
            
            InitializeCells();
        }

        private void InitializeCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _cells[x, y] = CellType.Empty;
                }
            }
        }

        public CellType GetCell(Vector2Int position)
        {
            if (!IsInBounds(position)) return CellType.Wall;
            return _cells[position.x, position.y];
        }

        public void SetCell(Vector2Int position, CellType type)
        {
            if (IsInBounds(position))
            {
                _cells[position.x, position.y] = type;
            }
        }

        public bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width &&
                   position.y >= 0 && position.y < Height;
        }

        public bool IsWalkable(Vector2Int position)
        {
            if (!IsInBounds(position)) return false;
            var cell = GetCell(position);
            return cell == CellType.Empty || cell == CellType.Food;
        }

        /// <summary>
        /// 获取所有食物
        /// </summary>
        public IReadOnlyList<FoodItem> GetFoodItems() => _foodItems.AsReadOnly();

        /// <summary>
        /// 放置食物
        /// </summary>
        public void PlaceFood(FoodItem food)
        {
            _foodItems.Add(food);
            SetCell(food.Position, CellType.Food);
        }

        /// <summary>
        /// 移除食物
        /// </summary>
        public void RemoveFood(FoodItem food)
        {
            _foodItems.Remove(food);
            SetCell(food.Position, CellType.Empty);
        }

        /// <summary>
        /// 获取指定位置的食物
        /// </summary>
        public FoodItem GetFoodAt(Vector2Int position)
        {
            return _foodItems.Find(f => f.Position == position);
        }

        /// <summary>
        /// 根据玩家行为调整难度
        /// </summary>
        public void AdaptDifficulty(float playerSkillLevel)
        {
            // 动态难度调整
            Difficulty = Mathf.Lerp(Difficulty, playerSkillLevel, 0.1f);
        }

        /// <summary>
        /// 演化世界
        /// </summary>
        public void Evolve(WorldEvolutionContext context)
        {
            // 根据玩家风险偏好调整毒性分布
            if (context.RiskPreference > 0.7f)
            {
                // 高风险偏好：增加高价值含毒食物
                Difficulty = Mathf.Min(1f, Difficulty + 0.05f);
            }
            else if (context.RiskPreference < 0.3f)
            {
                // 低风险偏好：增加安全食物
                Difficulty = Mathf.Max(0f, Difficulty - 0.05f);
            }

            // 根据玩家习惯改变世界风格
            if (context.TotalPoisonsTaken > 10 && Style == WorldStyle.Minimal)
            {
                Style = WorldStyle.Cyber;
            }
        }

        /// <summary>
        /// 获取随机空位置
        /// </summary>
        public Vector2Int? GetRandomEmptyPosition()
        {
            var emptyPositions = new List<Vector2Int>();
            
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_cells[x, y] == CellType.Empty)
                    {
                        emptyPositions.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (emptyPositions.Count == 0) return null;
            return emptyPositions[Random.Range(0, emptyPositions.Count)];
        }
    }

    /// <summary>
    /// 单元格类型
    /// </summary>
    public enum CellType
    {
        Empty,
        Wall,
        Food,
        Obstacle,
        Portal
    }

    /// <summary>
    /// 世界风格 - Notion + Cyber Minimalism
    /// </summary>
    public enum WorldStyle
    {
        /// <summary>
        /// 极简风 - 白底黑线
        /// </summary>
        Minimal,

        /// <summary>
        /// 赛博风 - 深色霓虹
        /// </summary>
        Cyber,

        /// <summary>
        /// 有机风 - 自然曲线
        /// </summary>
        Organic,

        /// <summary>
        /// 混沌风 - 感知毒触发
        /// </summary>
        Chaotic
    }

    /// <summary>
    /// 障碍物
    /// </summary>
    public class Obstacle
    {
        public Vector2Int Position { get; set; }
        public ObstacleType Type { get; set; }
        public bool Destructible { get; set; }
    }

    public enum ObstacleType
    {
        Wall,
        Block,
        Spike
    }

    /// <summary>
    /// 世界演化上下文
    /// </summary>
    public class WorldEvolutionContext
    {
        public float RiskPreference { get; set; }
        public int TotalMoves { get; set; }
        public int TotalPoisonsTaken { get; set; }
        public float AverageSpeed { get; set; }
        public Dictionary<PoisonType, int> PoisonPreferences { get; set; }
    }
}
