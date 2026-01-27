using DeskWarrior.Managers;
using DeskWarrior.Models;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 몬스터 UI ViewModel
    /// </summary>
    public class MonsterViewModel : ViewModelBase
    {
        private readonly GameManager _gameManager;

        private string _emoji = "";
        private string _name = "";
        private string _skinType = "";
        private int _currentHp;
        private int _maxHp;
        private double _hpRatio = 1.0;
        private bool _isBoss;

        public MonsterViewModel(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        #region Properties

        public Monster? CurrentMonster => _gameManager.CurrentMonster;

        public string Emoji
        {
            get => _emoji;
            private set => SetProperty(ref _emoji, value);
        }

        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        public string SkinType
        {
            get => _skinType;
            private set => SetProperty(ref _skinType, value);
        }

        public int CurrentHp
        {
            get => _currentHp;
            private set => SetProperty(ref _currentHp, value);
        }

        public int MaxHp
        {
            get => _maxHp;
            private set => SetProperty(ref _maxHp, value);
        }

        public double HpRatio
        {
            get => _hpRatio;
            private set => SetProperty(ref _hpRatio, value);
        }

        public bool IsBoss
        {
            get => _isBoss;
            private set => SetProperty(ref _isBoss, value);
        }

        public string HpText => $"{CurrentHp:N0}/{MaxHp:N0}";

        #endregion

        #region Methods

        /// <summary>
        /// 몬스터 UI 갱신
        /// </summary>
        public void Update()
        {
            var monster = _gameManager.CurrentMonster;
            if (monster == null) return;

            Emoji = monster.Emoji;
            Name = monster.Name;
            SkinType = monster.SkinType;
            CurrentHp = monster.CurrentHp;
            MaxHp = monster.MaxHp;
            HpRatio = monster.HpRatio;
            IsBoss = monster.IsBoss;

            OnPropertyChanged(nameof(HpText));
        }

        #endregion
    }
}
