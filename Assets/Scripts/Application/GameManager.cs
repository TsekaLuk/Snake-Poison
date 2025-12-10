using UnityEngine;
using SnakePoison.Domain.Snake;
using SnakePoison.Domain.World;
using SnakePoison.Domain.Poison;
using SnakePoison.Domain.Evolution;
using SnakePoison.Application.Events;

namespace SnakePoison.Application
{
    /// <summary>
    /// 游戏管理器 - 协调各领域模块
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("World Settings")]
        [SerializeField] private int worldWidth = 20;
        [SerializeField] private int worldHeight = 20;
        [SerializeField] private float moveInterval = 0.2f;

        [Header("Game State")]
        [SerializeField] private bool isPlaying;
        [SerializeField] private bool isPaused;

        public SnakeEntity Snake { get; private set; }
        public WorldGrid World { get; private set; }
        public EvolutionEngine Evolution { get; private set; }

        private float _moveTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (!isPlaying || isPaused) return;

            _moveTimer += Time.deltaTime;
            if (_moveTimer >= moveInterval)
            {
                _moveTimer = 0f;
                ProcessGameTick();
            }
        }

        public void InitializeGame()
        {
            // 初始化世界
            World = new WorldGrid(worldWidth, worldHeight, WorldStyle.Minimal);

            // 初始化蛇
            var startPosition = new Vector2Int(worldWidth / 2, worldHeight / 2);
            Snake = new SnakeEntity(startPosition);

            // 初始化演化引擎
            Evolution = new EvolutionEngine();
            Evolution.OnAbilityUnlocked += OnAbilityUnlocked;
            Evolution.OnEvolutionTriggered += OnEvolutionTriggered;

            // 订阅蛇事件
            Snake.OnMove += OnSnakeMove;
            Snake.OnGrow += OnSnakeGrow;
            Snake.OnPoisoned += OnSnakePoisoned;
            Snake.OnDeath += OnSnakeDeath;

            // 生成初始食物
            SpawnFood();

            isPlaying = false;
            isPaused = false;
        }

        public void StartGame()
        {
            isPlaying = true;
            isPaused = false;
            GameEvents.TriggerGameStart();
        }

        public void PauseGame()
        {
            isPaused = true;
            GameEvents.TriggerGamePause();
        }

        public void ResumeGame()
        {
            isPaused = false;
            GameEvents.TriggerGameResume();
        }

        public void RestartGame()
        {
            GameEvents.ClearAllSubscriptions();
            InitializeGame();
            StartGame();
        }

        private void ProcessGameTick()
        {
            // 移动蛇
            Snake.Move();

            // 检查碰撞
            CheckCollisions();

            // 处理毒性效果
            ProcessPoisonEffects();

            // 演化世界
            UpdateWorld();
        }

        private void CheckCollisions()
        {
            var head = Snake.Head;

            // 检查边界碰撞
            if (!World.IsInBounds(head))
            {
                Snake.Die("撞到了世界的边界");
                GameEvents.TriggerGameOver(GameOverReason.HitWall);
                isPlaying = false;
                return;
            }

            // 检查自身碰撞
            if (Snake.CollidesWithSelf())
            {
                Snake.Die("咬到了自己");
                GameEvents.TriggerGameOver(GameOverReason.HitSelf);
                isPlaying = false;
                return;
            }

            // 检查食物碰撞
            var food = World.GetFoodAt(head);
            if (food != null)
            {
                ConsumeFood(food);
            }
        }

        private void ConsumeFood(FoodItem food)
        {
            // 成长
            Snake.Grow(food.GrowthValue);
            GameEvents.TriggerFoodConsumed(food);

            // 处理毒性
            if (food.IsPoisoned && food.PoisonEffect != null)
            {
                var activePoison = new ActivePoison
                {
                    Type = food.PoisonEffect.Type,
                    RemainingDuration = food.PoisonEffect.Duration,
                    Intensity = food.PoisonEffect.Intensity,
                    EvolutionStage = 0
                };
                Snake.ApplyPoison(activePoison);
                GameEvents.TriggerSnakePoisoned(Snake, food.PoisonEffect);
            }

            // 移除食物并生成新的
            World.RemoveFood(food);
            SpawnFood();
        }

        private void SpawnFood()
        {
            var position = World.GetRandomEmptyPosition();
            if (position == null) return;

            FoodItem food;
            float roll = Random.value;

            if (roll < 0.6f)
            {
                // 60% 普通食物
                food = FoodItem.CreateNormal(position.Value);
            }
            else if (roll < 0.85f)
            {
                // 25% 可疑食物（含毒）
                var poisonType = (PoisonType)Random.Range(0, 4);
                var effect = poisonType switch
                {
                    PoisonType.Perception => PoisonEffectFactory.CreatePerceptionPoison(),
                    PoisonType.Impulsive => PoisonEffectFactory.CreateImpulsivePoison(),
                    PoisonType.Memory => PoisonEffectFactory.CreateMemoryPoison(),
                    PoisonType.Evolving => PoisonEffectFactory.CreateEvolvingPoison(),
                    _ => PoisonEffectFactory.CreatePerceptionPoison()
                };
                food = FoodItem.CreatePoisoned(position.Value, effect);
            }
            else
            {
                // 15% 高价值食物
                food = FoodItem.CreateValuable(position.Value);
            }

            World.PlaceFood(food);
            GameEvents.TriggerFoodSpawned(food);
        }

        private void ProcessPoisonEffects()
        {
            for (int i = Snake.ActivePoisons.Count - 1; i >= 0; i--)
            {
                var poison = Snake.ActivePoisons[i];
                
                // 处理演化
                if (poison.Type == PoisonType.Evolving)
                {
                    Evolution.ProcessEvolution(Snake, poison);
                }

                // 减少持续时间
                if (poison.RemainingDuration > 0)
                {
                    poison.RemainingDuration -= moveInterval;
                    if (poison.RemainingDuration <= 0)
                    {
                        Snake.RemovePoison(poison);
                    }
                }
            }
        }

        private void UpdateWorld()
        {
            var context = new WorldEvolutionContext
            {
                RiskPreference = CalculateRiskPreference(),
                TotalMoves = Snake.Trajectory.TotalMoves,
                TotalPoisonsTaken = Snake.Trajectory.PoisonsTaken
            };

            World.Evolve(context);
        }

        private float CalculateRiskPreference()
        {
            // 根据玩家吃毒的频率计算风险偏好
            if (Snake.Trajectory.TotalMoves == 0) return 0.5f;
            return Mathf.Clamp01((float)Snake.Trajectory.PoisonsTaken / Snake.Trajectory.TotalMoves * 10f);
        }

        // === 输入处理 ===

        public void SetDirection(Vector2Int direction)
        {
            Snake?.SetDirection(direction);
        }

        // === 事件处理 ===

        private void OnSnakeMove(SnakeEntity snake, Vector2Int position)
        {
            GameEvents.TriggerSnakeMove(snake, position);
        }

        private void OnSnakeGrow(SnakeEntity snake)
        {
            GameEvents.TriggerSnakeGrow(snake, snake.Length);
        }

        private void OnSnakePoisoned(SnakeEntity snake)
        {
            // 已在 ConsumeFood 中处理
        }

        private void OnSnakeDeath(SnakeEntity snake)
        {
            GameEvents.TriggerSnakeDeath(snake, snake.Trajectory.DeathCause);
        }

        private void OnAbilityUnlocked(Ability ability)
        {
            GameEvents.TriggerAbilityUnlocked(ability);
            Debug.Log($"能力解锁：{ability}");
        }

        private void OnEvolutionTriggered(EvolutionStage stage)
        {
            GameEvents.TriggerEvolutionStage(stage);
            Debug.Log($"演化：{stage.Name} - {stage.Effect}");
        }

        private void OnDestroy()
        {
            GameEvents.ClearAllSubscriptions();
        }
    }
}
