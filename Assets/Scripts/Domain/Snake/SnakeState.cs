namespace SnakePoison.Domain.Snake
{
    /// <summary>
    /// 蛇的状态枚举
    /// </summary>
    public enum SnakeState
    {
        /// <summary>
        /// 正常状态
        /// </summary>
        Normal,

        /// <summary>
        /// 中毒状态 - 至少有一种毒性效果激活
        /// </summary>
        Poisoned,

        /// <summary>
        /// 进化状态 - 正在经历能力提升
        /// </summary>
        Evolving,

        /// <summary>
        /// 洞察状态 - 可看穿世界幻象
        /// </summary>
        Enlightened,

        /// <summary>
        /// 死亡状态
        /// </summary>
        Dead
    }
}
