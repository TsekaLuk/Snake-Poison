using System;
using UnityEngine;
using SnakePoison.Domain.Snake;
using SnakePoison.Domain.Poison;
using SnakePoison.Domain.Evolution;

namespace SnakePoison.Application.Events
{
    /// <summary>
    /// 游戏事件中心 - 解耦各模块通信
    /// </summary>
    public static class GameEvents
    {
        // === 蛇相关事件 ===
        
        /// <summary>
        /// 蛇移动事件
        /// </summary>
        public static event Action<SnakeEntity, Vector2Int> OnSnakeMove;
        
        /// <summary>
        /// 蛇成长事件
        /// </summary>
        public static event Action<SnakeEntity, int> OnSnakeGrow;
        
        /// <summary>
        /// 蛇死亡事件
        /// </summary>
        public static event Action<SnakeEntity, string> OnSnakeDeath;
        
        /// <summary>
        /// 蛇中毒事件
        /// </summary>
        public static event Action<SnakeEntity, PoisonEffect> OnSnakePoisoned;

        // === 食物相关事件 ===
        
        /// <summary>
        /// 食物被吃掉事件
        /// </summary>
        public static event Action<FoodItem> OnFoodConsumed;
        
        /// <summary>
        /// 新食物生成事件
        /// </summary>
        public static event Action<FoodItem> OnFoodSpawned;

        // === 演化相关事件 ===
        
        /// <summary>
        /// 能力解锁事件
        /// </summary>
        public static event Action<Ability> OnAbilityUnlocked;
        
        /// <summary>
        /// 演化阶段触发事件
        /// </summary>
        public static event Action<EvolutionStage> OnEvolutionStageTriggered;

        // === 世界相关事件 ===
        
        /// <summary>
        /// 世界风格变化事件
        /// </summary>
        public static event Action<Domain.World.WorldStyle> OnWorldStyleChanged;
        
        /// <summary>
        /// 难度变化事件
        /// </summary>
        public static event Action<float> OnDifficultyChanged;

        // === 游戏状态事件 ===
        
        /// <summary>
        /// 游戏开始事件
        /// </summary>
        public static event Action OnGameStart;
        
        /// <summary>
        /// 游戏暂停事件
        /// </summary>
        public static event Action OnGamePause;
        
        /// <summary>
        /// 游戏恢复事件
        /// </summary>
        public static event Action OnGameResume;
        
        /// <summary>
        /// 游戏结束事件
        /// </summary>
        public static event Action<GameOverReason> OnGameOver;

        // === 触发方法 ===

        public static void TriggerSnakeMove(SnakeEntity snake, Vector2Int position) 
            => OnSnakeMove?.Invoke(snake, position);

        public static void TriggerSnakeGrow(SnakeEntity snake, int newLength) 
            => OnSnakeGrow?.Invoke(snake, newLength);

        public static void TriggerSnakeDeath(SnakeEntity snake, string cause) 
            => OnSnakeDeath?.Invoke(snake, cause);

        public static void TriggerSnakePoisoned(SnakeEntity snake, PoisonEffect effect) 
            => OnSnakePoisoned?.Invoke(snake, effect);

        public static void TriggerFoodConsumed(FoodItem food) 
            => OnFoodConsumed?.Invoke(food);

        public static void TriggerFoodSpawned(FoodItem food) 
            => OnFoodSpawned?.Invoke(food);

        public static void TriggerAbilityUnlocked(Ability ability) 
            => OnAbilityUnlocked?.Invoke(ability);

        public static void TriggerEvolutionStage(EvolutionStage stage) 
            => OnEvolutionStageTriggered?.Invoke(stage);

        public static void TriggerWorldStyleChanged(Domain.World.WorldStyle style) 
            => OnWorldStyleChanged?.Invoke(style);

        public static void TriggerDifficultyChanged(float difficulty) 
            => OnDifficultyChanged?.Invoke(difficulty);

        public static void TriggerGameStart() 
            => OnGameStart?.Invoke();

        public static void TriggerGamePause() 
            => OnGamePause?.Invoke();

        public static void TriggerGameResume() 
            => OnGameResume?.Invoke();

        public static void TriggerGameOver(GameOverReason reason) 
            => OnGameOver?.Invoke(reason);

        /// <summary>
        /// 清除所有事件订阅（场景切换时调用）
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnSnakeMove = null;
            OnSnakeGrow = null;
            OnSnakeDeath = null;
            OnSnakePoisoned = null;
            OnFoodConsumed = null;
            OnFoodSpawned = null;
            OnAbilityUnlocked = null;
            OnEvolutionStageTriggered = null;
            OnWorldStyleChanged = null;
            OnDifficultyChanged = null;
            OnGameStart = null;
            OnGamePause = null;
            OnGameResume = null;
            OnGameOver = null;
        }
    }

    /// <summary>
    /// 游戏结束原因
    /// </summary>
    public enum GameOverReason
    {
        /// <summary>
        /// 撞墙
        /// </summary>
        HitWall,
        
        /// <summary>
        /// 撞到自己
        /// </summary>
        HitSelf,
        
        /// <summary>
        /// 毒性过量
        /// </summary>
        PoisonOverdose,
        
        /// <summary>
        /// 玩家退出
        /// </summary>
        PlayerQuit,
        
        /// <summary>
        /// 达成目标
        /// </summary>
        Victory
    }
}
