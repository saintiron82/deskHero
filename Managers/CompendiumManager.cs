using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeskWarrior.Helpers;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 몬스터 도감 관리자
    /// </summary>
    public class CompendiumManager
    {
        #region Fields

        private MonsterCompendiumConfig _config;
        private readonly SaveManager _saveManager;
        private UserCompendium UserCompendium => _saveManager.CurrentSave.MonsterCompendium;

        #endregion

        #region Events

        public event EventHandler<CompendiumRewardEventArgs>? RewardAvailable;

        #endregion

        #region Constructor

        public CompendiumManager(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _config = LoadConfig();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 몬스터 조우 기록 (스폰 시)
        /// </summary>
        public void RecordEncounter(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) return;

            var compendium = UserCompendium;
            if (!compendium.Entries.ContainsKey(monsterId))
            {
                compendium.Entries[monsterId] = new CompendiumEntry
                {
                    FirstEncounter = DateTime.Now,
                    KillCount = 0,
                    MaxDamageDealt = 0
                };
                Logger.Log($"[Compendium] First encounter: {monsterId}");
            }
        }

        /// <summary>
        /// 몬스터 처치 기록
        /// </summary>
        public void RecordKill(string monsterId, long damageDealt)
        {
            if (string.IsNullOrEmpty(monsterId)) return;

            var compendium = UserCompendium;
            if (!compendium.Entries.TryGetValue(monsterId, out var entry))
            {
                entry = new CompendiumEntry
                {
                    FirstEncounter = DateTime.Now
                };
                compendium.Entries[monsterId] = entry;
            }

            entry.KillCount++;
            if (damageDealt > entry.MaxDamageDealt)
            {
                entry.MaxDamageDealt = damageDealt;
            }

            // 보상 체크
            CheckCompletionRewards();
        }

        /// <summary>
        /// 도감 진행도 가져오기
        /// </summary>
        public (int discovered, int total) GetProgress()
        {
            int totalMonsters = _config.Monsters.Count + _config.Bosses.Count;
            int discovered = UserCompendium.Entries.Count;
            return (discovered, totalMonsters);
        }

        /// <summary>
        /// 수령 가능한 보상 목록
        /// </summary>
        public List<CompendiumReward> GetClaimableRewards()
        {
            var (discovered, _) = GetProgress();
            var claimable = new List<CompendiumReward>();

            foreach (var reward in _config.CompletionRewards)
            {
                if (discovered >= reward.Threshold &&
                    !UserCompendium.ClaimedRewards.Contains(reward.Threshold))
                {
                    claimable.Add(reward);
                }
            }

            return claimable;
        }

        /// <summary>
        /// 보상 수령
        /// </summary>
        public int ClaimReward(int threshold)
        {
            var reward = _config.CompletionRewards.FirstOrDefault(r => r.Threshold == threshold);
            if (reward == null) return 0;

            var (discovered, _) = GetProgress();
            if (discovered < threshold) return 0;
            if (UserCompendium.ClaimedRewards.Contains(threshold)) return 0;

            UserCompendium.ClaimedRewards.Add(threshold);
            _saveManager.CurrentSave.PermanentCurrency.Crystals += reward.CrystalReward;

            Logger.Log($"[Compendium] Claimed reward: threshold={threshold}, crystals={reward.CrystalReward}");
            return reward.CrystalReward;
        }

        /// <summary>
        /// 모든 몬스터 정의 가져오기
        /// </summary>
        public IReadOnlyList<CompendiumMonsterDefinition> GetAllMonsters()
        {
            return _config.Monsters.Concat(_config.Bosses).ToList();
        }

        /// <summary>
        /// 특정 몬스터 정의 가져오기
        /// </summary>
        public CompendiumMonsterDefinition? GetMonsterDefinition(string monsterId)
        {
            return _config.Monsters.Concat(_config.Bosses)
                .FirstOrDefault(m => m.Id == monsterId);
        }

        /// <summary>
        /// 특정 몬스터 도감 항목 가져오기
        /// </summary>
        public CompendiumEntry? GetEntry(string monsterId)
        {
            return UserCompendium.Entries.TryGetValue(monsterId, out var entry) ? entry : null;
        }

        /// <summary>
        /// 몬스터가 발견되었는지 여부
        /// </summary>
        public bool IsDiscovered(string monsterId)
        {
            return UserCompendium.Entries.ContainsKey(monsterId);
        }

        #endregion

        #region Private Methods

        private void CheckCompletionRewards()
        {
            var claimable = GetClaimableRewards();
            if (claimable.Count > 0)
            {
                RewardAvailable?.Invoke(this, new CompendiumRewardEventArgs(claimable));
            }
        }

        private static MonsterCompendiumConfig LoadConfig()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "MonsterCompendium.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<MonsterCompendiumConfig>(json) ?? new MonsterCompendiumConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[CompendiumManager] Failed to load config: {ex.Message}");
            }

            return new MonsterCompendiumConfig();
        }

        #endregion
    }

    /// <summary>
    /// 도감 보상 이벤트 인자
    /// </summary>
    public class CompendiumRewardEventArgs : EventArgs
    {
        public List<CompendiumReward> Rewards { get; }

        public CompendiumRewardEventArgs(List<CompendiumReward> rewards)
        {
            Rewards = rewards;
        }
    }
}
