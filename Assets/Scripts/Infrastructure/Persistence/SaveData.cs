using System;
using System.Collections.Generic;

namespace SnakePoison.Infrastructure.Persistence
{
    /// <summary>
    /// 存档数据模型
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string Version = "1.0.0";
        public DateTime SaveTime;
        
        // 玩家统计
        public PlayerStats Stats;
        
        // 已解锁能力
        public List<string> UnlockedAbilities;
        
        // 蛇生故事集
        public List<SnakeStory> Stories;
        
        // 设置
        public GameSettings Settings;
    }

    /// <summary>
    /// 玩家统计数据
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public int TotalGames;
        public int TotalDeaths;
        public int MaxLength;
        public int TotalPoisonsTaken;
        public float TotalPlayTime;
        public int TotalFoodEaten;
        
        public Dictionary<string, int> PoisonStats;
        public Dictionary<string, int> DeathCauseStats;
    }

    /// <summary>
    /// 蛇生故事
    /// </summary>
    [Serializable]
    public class SnakeStory
    {
        public string Id;
        public DateTime BirthTime;
        public DateTime DeathTime;
        public float LifeSpan;
        public int MaxLength;
        public int TotalMoves;
        public int PoisonsTaken;
        public string DeathCause;
        public string GeneratedStory;
    }

    /// <summary>
    /// 游戏设置
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool Vibration = true;
        public float GameSpeed = 1f;
        public string Theme = "Minimal";
    }
}
