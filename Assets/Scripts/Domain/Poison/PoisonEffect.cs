using System;

namespace SnakePoison.Domain.Poison
{
    /// <summary>
    /// 毒性效果定义
    /// </summary>
    public class PoisonEffect
    {
        public PoisonType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        /// <summary>
        /// 效果持续时间（秒），-1 表示永久
        /// </summary>
        public float Duration { get; set; }
        
        /// <summary>
        /// 效果强度 0-1
        /// </summary>
        public float Intensity { get; set; }
        
        /// <summary>
        /// 负面效果描述
        /// </summary>
        public string Drawback { get; set; }
        
        /// <summary>
        /// 潜在收益描述
        /// </summary>
        public string PotentialBenefit { get; set; }
        
        /// <summary>
        /// 是否可叠加
        /// </summary>
        public bool Stackable { get; set; }
        
        /// <summary>
        /// 解毒条件
        /// </summary>
        public Func<Snake.SnakeEntity, bool> CureCondition { get; set; }
    }

    /// <summary>
    /// 毒性效果工厂
    /// </summary>
    public static class PoisonEffectFactory
    {
        public static PoisonEffect CreatePerceptionPoison(float intensity = 0.5f)
        {
            return new PoisonEffect
            {
                Type = PoisonType.Perception,
                Name = "感知之毒",
                Description = "世界的轮廓开始模糊...",
                Duration = 10f,
                Intensity = intensity,
                Drawback = "视野扭曲，距离感错误",
                PotentialBenefit = "可能看到隐藏的食物",
                Stackable = true
            };
        }

        public static PoisonEffect CreateImpulsivePoison(float intensity = 0.5f)
        {
            return new PoisonEffect
            {
                Type = PoisonType.Impulsive,
                Name = "冲动之毒",
                Description = "身体不听使唤...",
                Duration = 8f,
                Intensity = intensity,
                Drawback = "控制不稳定，可能突然转向",
                PotentialBenefit = "速度提升，可突破某些障碍",
                Stackable = false
            };
        }

        public static PoisonEffect CreateMemoryPoison(float intensity = 0.5f)
        {
            return new PoisonEffect
            {
                Type = PoisonType.Memory,
                Name = "遗忘之毒",
                Description = "过去渐渐消散...",
                Duration = 15f,
                Intensity = intensity,
                Drawback = "无法看到自己的轨迹",
                PotentialBenefit = "重置某些负面状态",
                Stackable = false
            };
        }

        public static PoisonEffect CreateEvolvingPoison(float intensity = 0.3f)
        {
            return new PoisonEffect
            {
                Type = PoisonType.Evolving,
                Name = "演化之毒",
                Description = "变化即将开始...",
                Duration = -1, // 永久，直到三阶段完成
                Intensity = intensity,
                Drawback = "效果会持续三次进化",
                PotentialBenefit = "最终阶段可能触发能力觉醒",
                Stackable = true
            };
        }
    }
}
