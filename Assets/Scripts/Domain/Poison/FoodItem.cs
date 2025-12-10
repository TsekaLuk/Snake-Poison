using UnityEngine;

namespace SnakePoison.Domain.Poison
{
    /// <summary>
    /// 食物实体 - 可能含毒，需要玩家抉择
    /// "选择吃还是不吃，就是玩家的价值判断"
    /// </summary>
    public class FoodItem
    {
        public string Id { get; private set; }
        public Vector2Int Position { get; set; }
        public FoodType Type { get; set; }
        public int GrowthValue { get; set; }
        
        /// <summary>
        /// 是否含毒
        /// </summary>
        public bool IsPoisoned { get; set; }
        
        /// <summary>
        /// 毒性效果（如果含毒）
        /// </summary>
        public PoisonEffect PoisonEffect { get; set; }
        
        /// <summary>
        /// 视觉线索强度 0-1
        /// 越高越容易被玩家识别是否有毒
        /// </summary>
        public float VisualHintStrength { get; set; }
        
        /// <summary>
        /// 是否为幻象（感知毒效果下可能出现）
        /// </summary>
        public bool IsIllusion { get; set; }

        public FoodItem(Vector2Int position, FoodType type = FoodType.Normal)
        {
            Id = System.Guid.NewGuid().ToString();
            Position = position;
            Type = type;
            GrowthValue = 1;
            VisualHintStrength = 0.5f;
        }

        /// <summary>
        /// 创建普通食物
        /// </summary>
        public static FoodItem CreateNormal(Vector2Int position)
        {
            return new FoodItem(position, FoodType.Normal)
            {
                GrowthValue = 1,
                IsPoisoned = false
            };
        }

        /// <summary>
        /// 创建含毒食物
        /// </summary>
        public static FoodItem CreatePoisoned(Vector2Int position, PoisonEffect effect)
        {
            return new FoodItem(position, FoodType.Suspicious)
            {
                GrowthValue = 2, // 含毒食物成长值更高
                IsPoisoned = true,
                PoisonEffect = effect
            };
        }

        /// <summary>
        /// 创建高价值食物（风险更高）
        /// </summary>
        public static FoodItem CreateValuable(Vector2Int position)
        {
            var item = new FoodItem(position, FoodType.Valuable)
            {
                GrowthValue = 3,
                IsPoisoned = Random.value > 0.5f // 50% 概率含毒
            };

            if (item.IsPoisoned)
            {
                item.PoisonEffect = PoisonEffectFactory.CreateEvolvingPoison();
            }

            return item;
        }
    }

    /// <summary>
    /// 食物类型
    /// </summary>
    public enum FoodType
    {
        /// <summary>
        /// 普通食物 - 安全
        /// </summary>
        Normal,

        /// <summary>
        /// 可疑食物 - 可能含毒
        /// </summary>
        Suspicious,

        /// <summary>
        /// 高价值食物 - 高风险高回报
        /// </summary>
        Valuable,

        /// <summary>
        /// 幻象食物 - 不存在，感知毒产生
        /// </summary>
        Illusion
    }
}
