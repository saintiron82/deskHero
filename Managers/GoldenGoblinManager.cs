using System;
using System.IO;
using System.Linq;
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
        /// 보상 배수 계산 (2~100배, 삼각분포)
        /// 중앙값(50배 부근)이 가장 높은 확률로 나옴
        /// </summary>
        public int GetRewardMultiplier()
        {
            // 삼각분포(Triangular Distribution): 두 개의 균등 분포 평균
            // 중앙(50배)에서 가장 높은 확률, 양 끝(2배, 100배)으로 갈수록 낮아짐
            double u1 = _random.NextDouble();  // 0~1
            double u2 = _random.NextDouble();  // 0~1
            double triangular = (u1 + u2) / 2.0;  // 0~1, 중앙(0.5)에 집중

            // 2~100 범위로 매핑
            int multiplier = _config.RewardMultiplierMin +
                            (int)(triangular * (_config.RewardMultiplierMax - _config.RewardMultiplierMin));

            return multiplier;
        }

        /// <summary>
        /// 보상 배수로 등급 결정 (config/SpecialMonsters.json에서 로드)
        /// </summary>
        public GoldenGoblinGrade? FindGradeForMultiplier(int multiplier)
        {
            if (_config.Grades == null || _config.Grades.Count == 0)
                return null;

            return _config.Grades.FirstOrDefault(g => multiplier >= g.RewardMin && multiplier <= g.RewardMax);
        }

        /// <summary>
        /// 등급과 배수를 동시에 결정 (스폰 시 호출)
        /// </summary>
        public (GoldenGoblinGrade? grade, int multiplier) DetermineGrade()
        {
            int multiplier = GetRewardMultiplier();
            var grade = FindGradeForMultiplier(multiplier);
            return (grade, multiplier);
        }

        /// <summary>
        /// 사전 결정된 배수로 보상 골드 계산
        /// </summary>
        public long CalculateRewardWithMultiplier(long stageExpectedGold, int multiplier)
        {
            return Helpers.SafeMath.MulLong(stageExpectedGold, multiplier);
        }

        /// <summary>
        /// 보상 골드 계산 (새 배수 생성)
        /// </summary>
        public long CalculateReward(long stageExpectedGold)
        {
            int multiplier = GetRewardMultiplier();
            return Helpers.SafeMath.MulLong(stageExpectedGold, multiplier);
        }

        /// <summary>
        /// 황금 고블린 몬스터 생성 (등급 시스템 적용)
        /// </summary>
        public Monster CreateGoldenGoblin(int currentLevel)
        {
            var (grade, multiplier) = DetermineGrade();

            // 등급별 로컬라이즈 이름 결정
            string language = LocalizationManager.Instance.CurrentLanguage;
            string name;

            // 등급에 이름이 있으면 등급 이름 사용
            if (grade?.Name != null && grade.Name.Count > 0)
            {
                if (!grade.Name.TryGetValue(language, out var gradeName))
                {
                    grade.Name.TryGetValue("en-US", out gradeName);
                }
                name = gradeName ?? "Golden Goblin";
            }
            else
            {
                if (!_config.Name.TryGetValue(language, out var localizedName))
                {
                    _config.Name.TryGetValue("en-US", out localizedName);
                }
                name = localizedName ?? "Golden Goblin";
            }

            return new Monster(_config, currentLevel, name, grade, multiplier);
        }
    }
}
