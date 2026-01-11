using System;
using System.IO;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 저장/로드 관리 클래스
    /// </summary>
    public class SaveManager
    {
        #region Fields

        private readonly string _savePath;
        private UserSave _currentSave;

        #endregion

        #region Properties

        public UserSave CurrentSave => _currentSave;

        #endregion

        #region Constructor

        public SaveManager()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var saveDir = Path.Combine(appData, "DeskWarrior");
            
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            _savePath = Path.Combine(saveDir, "UserSave.json");
            _currentSave = new UserSave();
        }

        #endregion

        #region Public Methods

        public void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    var json = File.ReadAllText(_savePath);
                    _currentSave = JsonSerializer.Deserialize<UserSave>(json) ?? new UserSave();
                }
            }
            catch
            {
                _currentSave = new UserSave();
            }

            // 날짜 확인 - 다른 날이면 오늘 입력 수 초기화
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_currentSave.Stats.LastPlayed != today)
            {
                _currentSave.Stats.TodayInputs = 0;
                _currentSave.Stats.LastPlayed = today;
            }
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_currentSave, options);
                File.WriteAllText(_savePath, json);
            }
            catch
            {
                // 저장 실패 시 무시
            }
        }

        public void UpdateWindowPosition(double x, double y)
        {
            _currentSave.Position.X = x;
            _currentSave.Position.Y = y;
        }

        public void AddInput()
        {
            _currentSave.Stats.TotalInputs++;
            _currentSave.Stats.TodayInputs++;
        }

        public void UpdateMaxLevel(int level)
        {
            if (level > _currentSave.Stats.MaxLevel)
            {
                _currentSave.Stats.MaxLevel = level;
            }
        }

        public void UpdateUpgrades(int keyboardPower, int mousePower)
        {
            _currentSave.Upgrades.KeyboardPower = keyboardPower;
            _currentSave.Upgrades.MousePower = mousePower;
        }

        public (int keyboard, int mouse) GetUpgrades()
        {
            return (_currentSave.Upgrades.KeyboardPower, _currentSave.Upgrades.MousePower);
        }

        public void AddDamage(long damage)
        {
            // Total Damage
            _currentSave.Stats.TotalDamage += damage;

            // Max Damage
            if (damage > _currentSave.Stats.MaxDamage)
            {
                _currentSave.Stats.MaxDamage = damage;
            }

            // History (Sliding Window)
            UpdateHistory(damage, 0);
        }

        public void AddKill()
        {
            _currentSave.Stats.MonsterKills++;
            UpdateHistory(0, 1);
        }

        private void UpdateHistory(long damage, int kills)
        {
            var now = DateTime.Now;
            var history = _currentSave.Stats.History;

            // 히스토리가 비었거나, 마지막 기록이 다른 시간대(Hour)라면 새 버킷 생성
            if (history.Count == 0 || history[history.Count - 1].TimeStamp.Hour != now.Hour || (now - history[history.Count - 1].TimeStamp).TotalHours >= 1)
            {
                // 빈 버킷 채우기 (그래프가 끊기지 않게 하려면 필요하지만, 여기서는 단순화하여 새 시간대만 추가)
                // 만약 24시간이 넘게 지났다면 리셋하는게 맞을 수도 있지만, 일단은 단순 추가
                history.Add(new HourlyData { TimeStamp = now, Damage = damage, Kills = kills });
            }
            else
            {
                // 현재 시간대 버킷 업데이트
                var currentBucket = history[history.Count - 1];
                currentBucket.Damage += damage;
                currentBucket.Kills += kills;
                // TimeStamp는 업데이트 하지 않음 (시작 시간 기준)
            }

            // 24개 초과 시 오래된 데이터 제거
            while (history.Count > 24)
            {
                history.RemoveAt(0);
            }
            
            _currentSave.Stats.LastUpdated = now;
        }

        #endregion
    }
}
