namespace SnakePoison.Domain.Poison
{
    /// <summary>
    /// 毒性类型 - 核心机制
    /// 毒并非纯负面，是"代价换能力"的策略机制
    /// </summary>
    public enum PoisonType
    {
        /// <summary>
        /// 感知毒 - 让蛇对世界的理解产生偏差
        /// 副作用：视野扭曲、距离感错误
        /// 潜在收益：可能看到隐藏物品
        /// </summary>
        Perception,

        /// <summary>
        /// 冲动毒 - 蛇会突然加速或转向
        /// 副作用：控制不稳定
        /// 潜在收益：速度提升、突破障碍
        /// </summary>
        Impulsive,

        /// <summary>
        /// 记忆毒 - 无法记住路线
        /// 副作用：轨迹显示消失、迷失方向
        /// 潜在收益：重置负面状态、获得新视角
        /// </summary>
        Memory,

        /// <summary>
        /// 三段毒 - 吞食后副作用会连续三次进化
        /// 第一阶段：轻微影响
        /// 第二阶段：效果增强
        /// 第三阶段：极端效果或能力觉醒
        /// </summary>
        Evolving
    }
}
