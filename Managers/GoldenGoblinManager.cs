using System;
using System.IO;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 황금 고블린 시스템 관리자
    /// </summary>
    public class GoldenGoblinManager
    {
        private readonly Random _random = new();
        private GoldenGoblinConfig _config;

        /// <summary>
        /// 마지막 황금 고블린 등장 이후 처치한 몬스터 수
        /// </summary>
        public int KillsSinceLastGoblin { get; private set; }

        /// <summary>
        /// 현재 설정
        /// </summary>
        public GoldenGoblinConfig Config => _config;

        public GoldenGoblinManager()
        {
            _config = new GoldenGoblinConfig();
            LoadConfig();
        }

        /// <summary>
        /// 설정 파일 로드
        /// </summary>
        private void LoadConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "SpecialMonsters.json");

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<SpecialMonstersConfig>(json);
                    if (config?.GoldenGoblin != null)
                    {
                        _config = config.GoldenGoblin;
                    }
                }
                catch (Exception)
                {
                    // 로드 실패 시 기본값 사용
                }
            }
        }

        /// <summary>
        /// 저장 데이터에서 쿨다운 로드
        /// </summary>
        public void LoadFromSave(UserSave save)
        {
            KillsSinceLastGoblin = save.GoldenGoblinCooldown;
        }

        /// <summary>
        /// 저장 데이터에 쿨다운 저장
        /// </summary>
        public void SaveToSave(UserSave save)
        {
            save.GoldenGoblinCooldown = KillsSinceLastGoblin;
        }

        /// <summary>
        /// 황금 고블린 등장 가능 여부 (쿨다운 체크)
        /// </summary>
        public bool CanSpawn()
        {
            return KillsSinceLastGoblin >= _config.CooldownKills;
        }

        /// <summary>
        /// 황금 고블린 스폰 여부 판정 (확률 체크)
        /// </summary>
        public bool ShouldSpawn()
        {
            if (!CanSpawn()) return false;
            return _random.NextDouble() < _config.SpawnChance;
        }

        /// <summary>
        /// 몬스터 처치 기록 (쿨다운 업데이트)
        /// </summary>
        /// <param name="wasGoldenGoblin">황금 고블린이었는지 여부</param>
        public void RecordKill(bool wasGoldenGoblin)
        {
            if (wasGoldenGoblin)
            {
                KillsSinceLastGoblin = 0;
            }
            else
            {
                KillsSinceLastGoblin++;
            }
        }

        /// <summary>
        /// 보상 배수 계산 (2~100배)
        /// </summary>
        public int GetRewardMultiplier()
        {
            return _random.Next(_config.RewardMultiplierMin, _config.RewardMultiplierMax + 1);
        }

        /// <summary>
        /// 보상 골드 계산
        /// </summary>
        /// <param name="stageExpectedGold">해당 스테이지 예상 골드</param>
        public int CalculateReward(int stageExpectedGold)
        {
            int multiplier = GetRewardMultiplier();
            return stageExpectedGold * multiplier;
        }

        /// <summary>
        /// 황금 고블린 몬스터 생성
        /// </summary>
        public Monster CreateGoldenGoblin(int currentLevel)
        {
            string language = LocalizationManager.Instance.CurrentLanguage;
            string name = _config.Name.TryGetValue(language, out var localizedName)
                ? localizedName
                : _config.Name.GetValueOrDefault("en-US", "Golden Goblin");

            return new Monster(_config, currentLevel, name);
        }
    }
}
