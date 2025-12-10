using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakePoison.Domain.Snake
{
    /// <summary>
    /// 蛇生轨迹 - 记录蛇的一生，用于叙事和回放
    /// "每条蛇 = 一条人生曲线"
    /// </summary>
    public class SnakeTrajectory
    {
        public DateTime BirthTime { get; private set; }
        public DateTime? DeathTime { get; private set; }
        public string DeathCause { get; private set; }
        
        public List<TrajectoryEvent> Events { get; private set; }
        public int TotalMoves { get; private set; }
        public int MaxLength { get; private set; }
        public int PoisonsTaken { get; private set; }
        public Dictionary<Poison.PoisonType, int> PoisonHistory { get; private set; }

        public TimeSpan LifeSpan => (DeathTime ?? DateTime.UtcNow) - BirthTime;

        public SnakeTrajectory()
        {
            BirthTime = DateTime.UtcNow;
            Events = new List<TrajectoryEvent>();
            PoisonHistory = new Dictionary<Poison.PoisonType, int>();
        }

        public void RecordMove(Vector2Int position, DateTime time)
        {
            TotalMoves++;
            Events.Add(new TrajectoryEvent
            {
                Type = TrajectoryEventType.Move,
                Position = position,
                Timestamp = time
            });
        }

        public void RecordGrowth(int newLength)
        {
            if (newLength > MaxLength) MaxLength = newLength;
            
            Events.Add(new TrajectoryEvent
            {
                Type = TrajectoryEventType.Growth,
                Value = newLength,
                Timestamp = DateTime.UtcNow
            });
        }

        public void RecordPoisonEvent(Poison.PoisonType poisonType)
        {
            PoisonsTaken++;
            
            if (!PoisonHistory.ContainsKey(poisonType))
                PoisonHistory[poisonType] = 0;
            PoisonHistory[poisonType]++;

            Events.Add(new TrajectoryEvent
            {
                Type = TrajectoryEventType.Poisoned,
                PoisonType = poisonType,
                Timestamp = DateTime.UtcNow
            });
        }

        public void RecordDeath(string cause)
        {
            DeathTime = DateTime.UtcNow;
            DeathCause = cause;
            
            Events.Add(new TrajectoryEvent
            {
                Type = TrajectoryEventType.Death,
                Message = cause,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 生成蛇生故事摘要
        /// </summary>
        public string GenerateStory()
        {
            var story = $"这条蛇存活了 {LifeSpan.TotalSeconds:F1} 秒，";
            story += $"移动了 {TotalMoves} 步，";
            story += $"最大长度达到 {MaxLength}，";
            story += $"品尝了 {PoisonsTaken} 次毒。";
            
            if (!string.IsNullOrEmpty(DeathCause))
            {
                story += $"\n最终，{DeathCause}。";
            }
            
            return story;
        }
    }

    public class TrajectoryEvent
    {
        public TrajectoryEventType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public Vector2Int Position { get; set; }
        public int Value { get; set; }
        public string Message { get; set; }
        public Poison.PoisonType PoisonType { get; set; }
    }

    public enum TrajectoryEventType
    {
        Move,
        Growth,
        Poisoned,
        Evolved,
        Decision,
        Death
    }
}
