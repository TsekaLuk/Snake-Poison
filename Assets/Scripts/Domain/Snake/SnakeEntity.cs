using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakePoison.Domain.Snake
{
    /// <summary>
    /// 蛇的核心实体 - 承载状态、毒性、成长轨迹
    /// </summary>
    public class SnakeEntity
    {
        public string Id { get; private set; }
        public SnakeState State { get; private set; }
        public List<Vector2Int> Body { get; private set; }
        public Vector2Int Direction { get; private set; }
        public List<ActivePoison> ActivePoisons { get; private set; }
        public SnakeTrajectory Trajectory { get; private set; }

        public int Length => Body.Count;
        public Vector2Int Head => Body[0];
        public bool IsAlive => State != SnakeState.Dead;

        public event Action<SnakeEntity> OnGrow;
        public event Action<SnakeEntity> OnPoisoned;
        public event Action<SnakeEntity> OnDeath;
        public event Action<SnakeEntity, Vector2Int> OnMove;

        public SnakeEntity(Vector2Int startPosition)
        {
            Id = Guid.NewGuid().ToString();
            State = SnakeState.Normal;
            Body = new List<Vector2Int> { startPosition };
            Direction = Vector2Int.right;
            ActivePoisons = new List<ActivePoison>();
            Trajectory = new SnakeTrajectory();
        }

        public void Move()
        {
            if (!IsAlive) return;

            var newHead = Head + Direction;
            Body.Insert(0, newHead);
            Body.RemoveAt(Body.Count - 1);
            
            Trajectory.RecordMove(newHead, DateTime.UtcNow);
            OnMove?.Invoke(this, newHead);
        }

        public void Grow(int amount = 1)
        {
            if (!IsAlive) return;

            for (int i = 0; i < amount; i++)
            {
                Body.Add(Body[Body.Count - 1]);
            }
            
            Trajectory.RecordGrowth(Length);
            OnGrow?.Invoke(this);
        }

        public void SetDirection(Vector2Int newDirection)
        {
            // 防止180度转向
            if (newDirection + Direction != Vector2Int.zero)
            {
                Direction = newDirection;
            }
        }

        public void ApplyPoison(ActivePoison poison)
        {
            if (!IsAlive) return;

            ActivePoisons.Add(poison);
            State = SnakeState.Poisoned;
            Trajectory.RecordPoisonEvent(poison.Type);
            OnPoisoned?.Invoke(this);
        }

        public void RemovePoison(ActivePoison poison)
        {
            ActivePoisons.Remove(poison);
            if (ActivePoisons.Count == 0)
            {
                State = SnakeState.Normal;
            }
        }

        public void Die(string cause)
        {
            State = SnakeState.Dead;
            Trajectory.RecordDeath(cause);
            OnDeath?.Invoke(this);
        }

        public bool CollidesWithSelf()
        {
            for (int i = 1; i < Body.Count; i++)
            {
                if (Body[i] == Head) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 记录当前激活的毒性效果
    /// </summary>
    public class ActivePoison
    {
        public Poison.PoisonType Type { get; set; }
        public float RemainingDuration { get; set; }
        public int EvolutionStage { get; set; } // 用于三段毒
        public float Intensity { get; set; }
    }
}
