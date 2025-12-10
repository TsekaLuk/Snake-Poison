using System;
using System.IO;
using UnityEngine;

namespace SnakePoison.Infrastructure.Persistence
{
    /// <summary>
    /// 存档管理器
    /// </summary>
    public class SaveManager
    {
        private const string SAVE_FILE_NAME = "snake_poison_save.json";
        private readonly string _savePath;

        public SaveData CurrentData { get; private set; }

        public SaveManager()
        {
            _savePath = Path.Combine(UnityEngine.Application.persistentDataPath, SAVE_FILE_NAME);
            LoadOrCreate();
        }

        /// <summary>
        /// 加载存档或创建新存档
        /// </summary>
        public void LoadOrCreate()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    CurrentData = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"存档加载成功: {_savePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"存档加载失败: {e.Message}");
                    CreateNewSave();
                }
            }
            else
            {
                CreateNewSave();
            }
        }

        /// <summary>
        /// 创建新存档
        /// </summary>
        private void CreateNewSave()
        {
            CurrentData = new SaveData
            {
                Version = "1.0.0",
                SaveTime = DateTime.UtcNow,
                Stats = new PlayerStats
                {
                    PoisonStats = new System.Collections.Generic.Dictionary<string, int>(),
                    DeathCauseStats = new System.Collections.Generic.Dictionary<string, int>()
                },
                UnlockedAbilities = new System.Collections.Generic.List<string>(),
                Stories = new System.Collections.Generic.List<SnakeStory>(),
                Settings = new GameSettings()
            };
            Save();
            Debug.Log("新存档已创建");
        }

        /// <summary>
        /// 保存当前数据
        /// </summary>
        public void Save()
        {
            try
            {
                CurrentData.SaveTime = DateTime.UtcNow;
                string json = JsonUtility.ToJson(CurrentData, true);
                File.WriteAllText(_savePath, json);
                Debug.Log($"存档保存成功: {_savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"存档保存失败: {e.Message}");
            }
        }

        /// <summary>
        /// 记录一局游戏
        /// </summary>
        public void RecordGame(Domain.Snake.SnakeTrajectory trajectory)
        {
            CurrentData.Stats.TotalGames++;
            CurrentData.Stats.TotalDeaths++;
            CurrentData.Stats.TotalPlayTime += (float)trajectory.LifeSpan.TotalSeconds;
            CurrentData.Stats.TotalPoisonsTaken += trajectory.PoisonsTaken;
            
            if (trajectory.MaxLength > CurrentData.Stats.MaxLength)
            {
                CurrentData.Stats.MaxLength = trajectory.MaxLength;
            }

            // 记录死因
            if (!string.IsNullOrEmpty(trajectory.DeathCause))
            {
                if (!CurrentData.Stats.DeathCauseStats.ContainsKey(trajectory.DeathCause))
                {
                    CurrentData.Stats.DeathCauseStats[trajectory.DeathCause] = 0;
                }
                CurrentData.Stats.DeathCauseStats[trajectory.DeathCause]++;
            }

            // 添加蛇生故事
            var story = new SnakeStory
            {
                Id = Guid.NewGuid().ToString(),
                BirthTime = trajectory.BirthTime,
                DeathTime = trajectory.DeathTime ?? DateTime.UtcNow,
                LifeSpan = (float)trajectory.LifeSpan.TotalSeconds,
                MaxLength = trajectory.MaxLength,
                TotalMoves = trajectory.TotalMoves,
                PoisonsTaken = trajectory.PoisonsTaken,
                DeathCause = trajectory.DeathCause,
                GeneratedStory = trajectory.GenerateStory()
            };

            CurrentData.Stories.Add(story);

            // 只保留最近100个故事
            if (CurrentData.Stories.Count > 100)
            {
                CurrentData.Stories.RemoveAt(0);
            }

            Save();
        }

        /// <summary>
        /// 解锁能力
        /// </summary>
        public void UnlockAbility(Domain.Evolution.Ability ability)
        {
            string abilityName = ability.ToString();
            if (!CurrentData.UnlockedAbilities.Contains(abilityName))
            {
                CurrentData.UnlockedAbilities.Add(abilityName);
                Save();
            }
        }

        /// <summary>
        /// 更新设置
        /// </summary>
        public void UpdateSettings(GameSettings settings)
        {
            CurrentData.Settings = settings;
            Save();
        }

        /// <summary>
        /// 重置存档
        /// </summary>
        public void Reset()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
            CreateNewSave();
        }
    }
}
