using System;
using System.Collections.Generic;
using SnakePoison.Domain.Snake;
using SnakePoison.Domain.Poison;

namespace SnakePoison.Domain.Evolution
{
    /// <summary>
    /// 演化引擎 - 管理蛇的成长策略
    /// "Evolution is inevitable."
    /// </summary>
    public class EvolutionEngine
    {
        private readonly Dictionary<PoisonType, EvolutionPath> _evolutionPaths;
        private readonly List<Ability> _unlockedAbilities;

        public event Action<Ability> OnAbilityUnlocked;
        public event Action<EvolutionStage> OnEvolutionTriggered;

        public EvolutionEngine()
        {
            _evolutionPaths = new Dictionary<PoisonType, EvolutionPath>();
            _unlockedAbilities = new List<Ability>();
            InitializeEvolutionPaths();
        }

        private void InitializeEvolutionPaths()
        {
            // 感知毒演化路径
            _evolutionPaths[PoisonType.Perception] = new EvolutionPath
            {
                Type = PoisonType.Perception,
                Stages = new List<EvolutionStage>
                {
                    new() { Level = 1, Name = "模糊视界", Effect = "视野轻微扭曲" },
                    new() { Level = 2, Name = "双重视界", Effect = "可同时看到两个平行世界" },
                    new() { Level = 3, Name = "真视", Effect = "看穿所有幻象，发现隐藏物品", UnlocksAbility = Ability.TrueSight }
                }
            };

            // 冲动毒演化路径
            _evolutionPaths[PoisonType.Impulsive] = new EvolutionPath
            {
                Type = PoisonType.Impulsive,
                Stages = new List<EvolutionStage>
                {
                    new() { Level = 1, Name = "微颤", Effect = "偶尔失控" },
                    new() { Level = 2, Name = "脉冲", Effect = "短暂加速，方向不稳" },
                    new() { Level = 3, Name = "闪电", Effect = "可控的瞬间移动", UnlocksAbility = Ability.Dash }
                }
            };

            // 记忆毒演化路径
            _evolutionPaths[PoisonType.Memory] = new EvolutionPath
            {
                Type = PoisonType.Memory,
                Stages = new List<EvolutionStage>
                {
                    new() { Level = 1, Name = "健忘", Effect = "轨迹显示模糊" },
                    new() { Level = 2, Name = "失忆", Effect = "完全看不到自己的尾巴" },
                    new() { Level = 3, Name = "重生", Effect = "清除所有负面状态，长度减半但获得保护", UnlocksAbility = Ability.Rebirth }
                }
            };

            // 三段毒演化路径
            _evolutionPaths[PoisonType.Evolving] = new EvolutionPath
            {
                Type = PoisonType.Evolving,
                Stages = new List<EvolutionStage>
                {
                    new() { Level = 1, Name = "萌芽", Effect = "未知变化开始酝酿" },
                    new() { Level = 2, Name = "蜕变", Effect = "效果不断增强" },
                    new() { Level = 3, Name = "觉醒", Effect = "随机获得一种终极能力", UnlocksAbility = Ability.Random }
                }
            };
        }

        /// <summary>
        /// 处理毒性演化
        /// </summary>
        public EvolutionResult ProcessEvolution(SnakeEntity snake, ActivePoison poison)
        {
            if (!_evolutionPaths.TryGetValue(poison.Type, out var path))
            {
                return new EvolutionResult { Success = false };
            }

            // 三段毒的特殊处理
            if (poison.Type == PoisonType.Evolving)
            {
                poison.EvolutionStage++;
                if (poison.EvolutionStage > 3)
                {
                    // 完成三阶段演化
                    return CompleteEvolution(snake, poison, path);
                }
            }

            var currentStage = path.Stages[Math.Min(poison.EvolutionStage, path.Stages.Count - 1)];
            OnEvolutionTriggered?.Invoke(currentStage);

            return new EvolutionResult
            {
                Success = true,
                Stage = currentStage,
                Message = $"{currentStage.Name}: {currentStage.Effect}"
            };
        }

        private EvolutionResult CompleteEvolution(SnakeEntity snake, ActivePoison poison, EvolutionPath path)
        {
            var finalStage = path.Stages[^1];
            
            if (finalStage.UnlocksAbility != Ability.None)
            {
                var ability = finalStage.UnlocksAbility;
                
                // 随机能力处理
                if (ability == Ability.Random)
                {
                    var abilities = new[] { Ability.TrueSight, Ability.Dash, Ability.Rebirth, Ability.PhaseThrough };
                    ability = abilities[UnityEngine.Random.Range(0, abilities.Length)];
                }

                _unlockedAbilities.Add(ability);
                OnAbilityUnlocked?.Invoke(ability);
            }

            snake.RemovePoison(poison);

            return new EvolutionResult
            {
                Success = true,
                Stage = finalStage,
                UnlockedAbility = finalStage.UnlocksAbility,
                Message = $"觉醒：{finalStage.Effect}"
            };
        }

        /// <summary>
        /// 检查是否已解锁能力
        /// </summary>
        public bool HasAbility(Ability ability) => _unlockedAbilities.Contains(ability);

        /// <summary>
        /// 获取所有已解锁能力
        /// </summary>
        public IReadOnlyList<Ability> GetUnlockedAbilities() => _unlockedAbilities.AsReadOnly();
    }

    /// <summary>
    /// 演化路径
    /// </summary>
    public class EvolutionPath
    {
        public PoisonType Type { get; set; }
        public List<EvolutionStage> Stages { get; set; }
    }

    /// <summary>
    /// 演化阶段
    /// </summary>
    public class EvolutionStage
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public string Effect { get; set; }
        public Ability UnlocksAbility { get; set; }
    }

    /// <summary>
    /// 演化结果
    /// </summary>
    public class EvolutionResult
    {
        public bool Success { get; set; }
        public EvolutionStage Stage { get; set; }
        public Ability UnlockedAbility { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 能力类型
    /// </summary>
    public enum Ability
    {
        None,
        
        /// <summary>
        /// 真视 - 看穿幻象
        /// </summary>
        TrueSight,
        
        /// <summary>
        /// 冲刺 - 瞬间移动
        /// </summary>
        Dash,
        
        /// <summary>
        /// 重生 - 清除负面状态
        /// </summary>
        Rebirth,
        
        /// <summary>
        /// 穿越 - 穿过自己身体
        /// </summary>
        PhaseThrough,
        
        /// <summary>
        /// 随机能力（三段毒觉醒）
        /// </summary>
        Random
    }
}
