using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SnakePoison;

// ============================================
// 🐍 Snake-Poison Console Edition
// "蛇的一生，就是人的选择。"
// ============================================

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        
        var game = new Game();
        game.Run();
    }
}

// === 游戏主循环 ===
class Game
{
    private readonly World _world;
    private readonly Snake _snake;
    private readonly Renderer _renderer;
    private bool _isRunning;
    private bool _isPaused;
    private int _tickInterval = 150; // 毫秒

    public Game()
    {
        _world = new World(30, 20);
        _snake = new Snake(15, 10);
        _renderer = new Renderer(_world, _snake);
        _world.SpawnFood(_snake);
    }

    public void Run()
    {
        ShowIntro();
        _isRunning = true;

        while (_isRunning)
        {
            ProcessInput();
            
            if (!_isPaused && _snake.IsAlive)
            {
                Update();
            }
            
            _renderer.Draw();
            Thread.Sleep(_tickInterval);
        }

        ShowGameOver();
    }

    private void ShowIntro()
    {
        Console.Clear();
        Console.WriteLine(@"
    ╔═══════════════════════════════════════════════╗
    ║        🐍 S N A K E - P O I S O N 🐍          ║
    ╠═══════════════════════════════════════════════╣
    ║                                               ║
    ║   ""蛇的一生，就是人的选择。""                 ║
    ║                                               ║
    ║   ● 普通食物    成长 +1                       ║
    ║   ◆ 可疑食物    成长 +2  可能含毒             ║
    ║   ★ 高价值      成长 +3  高风险高回报         ║
    ║                                               ║
    ║   毒性类型:                                   ║
    ║   [P] 感知毒 - 视野扭曲                       ║
    ║   [I] 冲动毒 - 速度变化                       ║
    ║   [M] 记忆毒 - 轨迹模糊                       ║
    ║   [E] 三段毒 - 持续演化                       ║
    ║                                               ║
    ║   操作: ↑↓←→ 或 WASD 移动                    ║
    ║         空格 暂停  Q 退出                     ║
    ║                                               ║
    ╚═══════════════════════════════════════════════╝

              按任意键开始...
");
        Console.ReadKey(true);
    }

    private void ProcessInput()
    {
        if (!Console.KeyAvailable) return;

        var key = Console.ReadKey(true).Key;
        
        switch (key)
        {
            case ConsoleKey.UpArrow or ConsoleKey.W:
                _snake.SetDirection(0, -1);
                break;
            case ConsoleKey.DownArrow or ConsoleKey.S:
                _snake.SetDirection(0, 1);
                break;
            case ConsoleKey.LeftArrow or ConsoleKey.A:
                _snake.SetDirection(-1, 0);
                break;
            case ConsoleKey.RightArrow or ConsoleKey.D:
                _snake.SetDirection(1, 0);
                break;
            case ConsoleKey.Spacebar:
                _isPaused = !_isPaused;
                break;
            case ConsoleKey.Q:
                _isRunning = false;
                break;
        }
    }

    private void Update()
    {
        // 处理毒性效果
        ProcessPoisonEffects();
        
        // 移动蛇
        _snake.Move();

        // 检查碰撞
        if (_snake.CollidesWithWall(_world) || _snake.CollidesWithSelf())
        {
            _snake.Die(_snake.CollidesWithWall(_world) ? "撞到了世界的边界" : "咬到了自己");
            _isRunning = false;
            return;
        }

        // 检查食物
        var food = _world.GetFoodAt(_snake.HeadX, _snake.HeadY);
        if (food != null)
        {
            _snake.Grow(food.GrowthValue);
            
            if (food.IsPoisoned && food.Poison != null)
            {
                _snake.ApplyPoison(food.Poison);
            }
            
            _world.RemoveFood(food);
            _world.SpawnFood(_snake);
        }

        // 演化世界
        _world.Evolve(_snake);
    }

    private void ProcessPoisonEffects()
    {
        for (int i = _snake.ActivePoisons.Count - 1; i >= 0; i--)
        {
            var poison = _snake.ActivePoisons[i];
            
            // 应用效果
            switch (poison.Type)
            {
                case PoisonType.Impulsive:
                    // 随机速度变化
                    if (Random.Shared.NextDouble() < 0.3 * poison.Intensity)
                    {
                        _tickInterval = Random.Shared.Next(80, 200);
                    }
                    break;
                    
                case PoisonType.Evolving:
                    poison.EvolutionStage++;
                    if (poison.EvolutionStage >= 3)
                    {
                        // 觉醒！
                        _snake.UnlockAbility();
                        _snake.RemovePoison(poison);
                        continue;
                    }
                    break;
            }
            
            // 减少持续时间
            poison.RemainingTicks--;
            if (poison.RemainingTicks <= 0)
            {
                _snake.RemovePoison(poison);
                _tickInterval = 150; // 恢复正常速度
            }
        }
    }

    private void ShowGameOver()
    {
        Console.Clear();
        Console.WriteLine($@"
    ╔═══════════════════════════════════════════════╗
    ║              G A M E   O V E R                ║
    ╠═══════════════════════════════════════════════╣
    ║                                               ║
    ║   {_snake.Trajectory.DeathCause,-40} ║
    ║                                               ║
    ║   存活时间: {_snake.Trajectory.LifeSpan.TotalSeconds,6:F1} 秒                       ║
    ║   最大长度: {_snake.Trajectory.MaxLength,6}                            ║
    ║   移动步数: {_snake.Trajectory.TotalMoves,6}                            ║
    ║   品尝毒物: {_snake.Trajectory.PoisonsTaken,6} 次                          ║
    ║                                               ║
    ║   ""你不是输，你只是做了一个选择。""           ║
    ║                                               ║
    ╚═══════════════════════════════════════════════╝

    {_snake.Trajectory.GenerateStory()}
");
    }
}

// === 蛇实体 ===
class Snake
{
    public List<(int X, int Y)> Body { get; } = new();
    public int DirectionX { get; private set; } = 1;
    public int DirectionY { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public List<ActivePoison> ActivePoisons { get; } = new();
    public SnakeTrajectory Trajectory { get; } = new();
    public List<string> Abilities { get; } = new();

    public int HeadX => Body[0].X;
    public int HeadY => Body[0].Y;
    public int Length => Body.Count;

    public Snake(int startX, int startY)
    {
        Body.Add((startX, startY));
        Body.Add((startX - 1, startY));
        Body.Add((startX - 2, startY));
    }

    public void SetDirection(int dx, int dy)
    {
        // 防止180度转向
        if (dx + DirectionX != 0 || dy + DirectionY != 0)
        {
            DirectionX = dx;
            DirectionY = dy;
        }
    }

    public void Move()
    {
        var newHead = (HeadX + DirectionX, HeadY + DirectionY);
        Body.Insert(0, newHead);
        Body.RemoveAt(Body.Count - 1);
        Trajectory.RecordMove();
    }

    public void Grow(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Body.Add(Body[^1]);
        }
        Trajectory.RecordGrowth(Length);
    }

    public bool CollidesWithWall(World world)
    {
        return HeadX < 0 || HeadX >= world.Width || HeadY < 0 || HeadY >= world.Height;
    }

    public bool CollidesWithSelf()
    {
        for (int i = 1; i < Body.Count; i++)
        {
            if (Body[i].X == HeadX && Body[i].Y == HeadY) return true;
        }
        return false;
    }

    public void ApplyPoison(PoisonEffect poison)
    {
        ActivePoisons.Add(new ActivePoison
        {
            Type = poison.Type,
            Intensity = poison.Intensity,
            RemainingTicks = (int)(poison.Duration * 5), // 转换为tick
            EvolutionStage = 0
        });
        Trajectory.RecordPoison(poison.Type);
    }

    public void RemovePoison(ActivePoison poison) => ActivePoisons.Remove(poison);

    public void Die(string cause)
    {
        IsAlive = false;
        Trajectory.RecordDeath(cause);
    }

    public void UnlockAbility()
    {
        var abilities = new[] { "真视", "冲刺", "重生", "穿越" };
        var ability = abilities[Random.Shared.Next(abilities.Length)];
        if (!Abilities.Contains(ability))
        {
            Abilities.Add(ability);
        }
    }

    public bool HasPoison(PoisonType type) => ActivePoisons.Any(p => p.Type == type);
}

// === 蛇生轨迹 ===
class SnakeTrajectory
{
    public DateTime BirthTime { get; } = DateTime.Now;
    public DateTime? DeathTime { get; private set; }
    public string? DeathCause { get; private set; }
    public int TotalMoves { get; private set; }
    public int MaxLength { get; private set; } = 3;
    public int PoisonsTaken { get; private set; }
    public TimeSpan LifeSpan => (DeathTime ?? DateTime.Now) - BirthTime;

    public void RecordMove() => TotalMoves++;
    
    public void RecordGrowth(int length)
    {
        if (length > MaxLength) MaxLength = length;
    }

    public void RecordPoison(PoisonType type) => PoisonsTaken++;

    public void RecordDeath(string cause)
    {
        DeathTime = DateTime.Now;
        DeathCause = cause;
    }

    public string GenerateStory()
    {
        return $"这条蛇存活了 {LifeSpan.TotalSeconds:F1} 秒，移动了 {TotalMoves} 步，" +
               $"最大长度达到 {MaxLength}，品尝了 {PoisonsTaken} 次毒。" +
               (DeathCause != null ? $"\n    最终，{DeathCause}。" : "");
    }
}

// === 世界 ===
class World
{
    public int Width { get; }
    public int Height { get; }
    public List<FoodItem> Foods { get; } = new();
    public float Difficulty { get; private set; } = 0.5f;

    public World(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public FoodItem? GetFoodAt(int x, int y) => Foods.FirstOrDefault(f => f.X == x && f.Y == y);

    public void RemoveFood(FoodItem food) => Foods.Remove(food);

    public void SpawnFood(Snake snake)
    {
        var emptyPositions = new List<(int, int)>();
        
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (!snake.Body.Any(b => b.X == x && b.Y == y) &&
                    !Foods.Any(f => f.X == x && f.Y == y))
                {
                    emptyPositions.Add((x, y));
                }
            }
        }

        if (emptyPositions.Count == 0) return;

        var pos = emptyPositions[Random.Shared.Next(emptyPositions.Count)];
        var roll = Random.Shared.NextDouble();

        FoodItem food;
        if (roll < 0.55)
        {
            // 普通食物
            food = new FoodItem(pos.Item1, pos.Item2, FoodType.Normal, 1);
        }
        else if (roll < 0.80)
        {
            // 可疑食物
            food = new FoodItem(pos.Item1, pos.Item2, FoodType.Suspicious, 2)
            {
                IsPoisoned = true,
                Poison = PoisonFactory.CreateRandom()
            };
        }
        else
        {
            // 高价值食物
            food = new FoodItem(pos.Item1, pos.Item2, FoodType.Valuable, 3)
            {
                IsPoisoned = Random.Shared.NextDouble() > 0.5,
                Poison = Random.Shared.NextDouble() > 0.5 ? PoisonFactory.CreateEvolving() : null
            };
        }

        Foods.Add(food);
    }

    public void Evolve(Snake snake)
    {
        // 根据玩家风险偏好调整
        if (snake.Trajectory.TotalMoves > 0)
        {
            var riskPref = (float)snake.Trajectory.PoisonsTaken / snake.Trajectory.TotalMoves * 10f;
            Difficulty = Math.Clamp(Difficulty + (riskPref > 0.7f ? 0.01f : -0.01f), 0f, 1f);
        }
    }
}

// === 食物 ===
class FoodItem
{
    public int X { get; }
    public int Y { get; }
    public FoodType Type { get; }
    public int GrowthValue { get; }
    public bool IsPoisoned { get; set; }
    public PoisonEffect? Poison { get; set; }

    public FoodItem(int x, int y, FoodType type, int growthValue)
    {
        X = x;
        Y = y;
        Type = type;
        GrowthValue = growthValue;
    }
}

enum FoodType { Normal, Suspicious, Valuable }

// === 毒性系统 ===
enum PoisonType { Perception, Impulsive, Memory, Evolving }

class PoisonEffect
{
    public PoisonType Type { get; init; }
    public string Name { get; init; } = "";
    public float Duration { get; init; }
    public float Intensity { get; init; }
}

class ActivePoison
{
    public PoisonType Type { get; set; }
    public float Intensity { get; set; }
    public int RemainingTicks { get; set; }
    public int EvolutionStage { get; set; }
}

static class PoisonFactory
{
    public static PoisonEffect CreateRandom()
    {
        var type = (PoisonType)Random.Shared.Next(4);
        return type switch
        {
            PoisonType.Perception => new PoisonEffect { Type = type, Name = "感知之毒", Duration = 10, Intensity = 0.5f },
            PoisonType.Impulsive => new PoisonEffect { Type = type, Name = "冲动之毒", Duration = 8, Intensity = 0.5f },
            PoisonType.Memory => new PoisonEffect { Type = type, Name = "遗忘之毒", Duration = 15, Intensity = 0.5f },
            PoisonType.Evolving => CreateEvolving(),
            _ => new PoisonEffect { Type = type, Name = "未知之毒", Duration = 10, Intensity = 0.3f }
        };
    }

    public static PoisonEffect CreateEvolving() => new()
    {
        Type = PoisonType.Evolving,
        Name = "演化之毒",
        Duration = 20,
        Intensity = 0.3f
    };
}

// === 渲染器 ===
class Renderer
{
    private readonly World _world;
    private readonly Snake _snake;
    private readonly StringBuilder _buffer = new();

    public Renderer(World world, Snake snake)
    {
        _world = world;
        _snake = snake;
    }

    public void Draw()
    {
        _buffer.Clear();
        Console.SetCursorPosition(0, 0);

        // 顶部边框
        _buffer.AppendLine("╔" + new string('═', _world.Width * 2) + "╗");

        // 游戏区域
        for (int y = 0; y < _world.Height; y++)
        {
            _buffer.Append('║');
            for (int x = 0; x < _world.Width; x++)
            {
                _buffer.Append(GetCellChar(x, y));
            }
            _buffer.AppendLine("║");
        }

        // 底部边框
        _buffer.AppendLine("╚" + new string('═', _world.Width * 2) + "╝");

        // 状态信息
        _buffer.AppendLine($" 长度: {_snake.Length}  移动: {_snake.Trajectory.TotalMoves}  毒: {_snake.Trajectory.PoisonsTaken}");
        
        // 当前毒性状态
        if (_snake.ActivePoisons.Count > 0)
        {
            _buffer.Append(" 状态: ");
            foreach (var p in _snake.ActivePoisons)
            {
                var icon = p.Type switch
                {
                    PoisonType.Perception => "[P]感知",
                    PoisonType.Impulsive => "[I]冲动",
                    PoisonType.Memory => "[M]遗忘",
                    PoisonType.Evolving => $"[E]演化{p.EvolutionStage}/3",
                    _ => "[?]"
                };
                _buffer.Append($"{icon} ");
            }
            _buffer.AppendLine();
        }

        // 能力
        if (_snake.Abilities.Count > 0)
        {
            _buffer.AppendLine($" 觉醒能力: {string.Join(", ", _snake.Abilities)}");
        }

        Console.Write(_buffer);
    }

    private string GetCellChar(int x, int y)
    {
        // 检查是否是蛇头
        if (_snake.Body[0].X == x && _snake.Body[0].Y == y)
        {
            return _snake.HasPoison(PoisonType.Perception) ? "◎ " : "◉ ";
        }

        // 检查是否是蛇身
        for (int i = 1; i < _snake.Body.Count; i++)
        {
            if (_snake.Body[i].X == x && _snake.Body[i].Y == y)
            {
                // 记忆毒效果：尾巴变模糊
                if (_snake.HasPoison(PoisonType.Memory) && i > _snake.Body.Count / 2)
                {
                    return "░░";
                }
                return "○ ";
            }
        }

        // 检查食物
        var food = _world.Foods.FirstOrDefault(f => f.X == x && f.Y == y);
        if (food != null)
        {
            // 感知毒效果：食物位置可能偏移显示
            if (_snake.HasPoison(PoisonType.Perception) && Random.Shared.NextDouble() < 0.3)
            {
                return "  "; // 有时看不到
            }
            
            return food.Type switch
            {
                FoodType.Normal => "● ",
                FoodType.Suspicious => "◆ ",
                FoodType.Valuable => "★ ",
                _ => "? "
            };
        }

        return "  ";
    }
}
