using System;
using System.Collections.Generic;
using DeskWarrior.Models;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 게임 관리자 인터페이스 (SOLID: DIP)
    /// </summary>
    public interface IGameManager
    {
        #region Events

        event EventHandler? MonsterDefeated;
        event EventHandler? MonsterSpawned;
        event EventHandler? TimerTick;
        event EventHandler? GameOver;
        event EventHandler? StatsChanged;
        event EventHandler<DamageEventArgs>? DamageDealt;

        #endregion

        #region Properties

        int CurrentLevel { get; }
        int Gold { get; }
        int KeyboardPower { get; }
        int MousePower { get; }
        double RemainingTime { get; }
        Monster? CurrentMonster { get; }
        GameData Config { get; }
        GameData GameData { get; }
        List<HeroData> Heroes { get; }

        // In-Game Stats
        InGameStats InGameStats { get; }

        // Session Stats
        long SessionDamage { get; }
        long SessionTotalGold { get; }
        int SessionKills { get; }
        int SessionBossKills { get; }
        int SessionKeyboardInputs { get; }
        int SessionMouseInputs { get; }
        int SessionCriticalHits { get; }
        DateTime SessionStartTime { get; }

        #endregion

        #region Methods

        void StartGame();
        void OnKeyboardInput();
        void OnMouseInput();
        bool UpgradeKeyboardPower();
        bool UpgradeMousePower();
        bool UpgradeInGameStat(string statId);
        int GetInGameStatUpgradeCost(string statId);
        int CalculateUpgradeCost(int currentLevel);
        void LoadUpgrades(int keyboardPower, int mousePower);
        void RestartGame();
        SessionStats CreateSessionStats(string endReason = "timeout");
        string GetGameOverMessage(string? deathType = null);

        #endregion
    }

    /// <summary>
    /// 데미지 이벤트 인자
    /// </summary>
    public class DamageEventArgs : EventArgs
    {
        public int Damage { get; }
        public bool IsCritical { get; }
        public bool IsMouse { get; }

        public DamageEventArgs(int damage, bool isCritical, bool isMouse)
        {
            Damage = damage;
            IsCritical = isCritical;
            IsMouse = isMouse;
        }
    }
}
