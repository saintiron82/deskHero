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

        #endregion
    }
}
