using System;
using DeskWarrior.Interfaces;
using DeskWarrior.Models;

namespace DeskWarrior.ViewModels
{
    /// <summary>
    /// 업적 ViewModel (로컬라이제이션 처리 담당)
    /// </summary>
    public class AchievementViewModel : ViewModelBase
    {
        private readonly AchievementDefinition _definition;
        private readonly ILocalizationProvider _localizationProvider;
        private AchievementProgress? _progress;

        public AchievementViewModel(
            AchievementDefinition definition,
            ILocalizationProvider localizationProvider,
            AchievementProgress? progress = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _localizationProvider = localizationProvider ?? throw new ArgumentNullException(nameof(localizationProvider));
            _progress = progress;
        }

        #region Definition Properties

        public string Id => _definition.Id;
        public string Icon => _definition.Icon;
        public string Category => _definition.Category;
        public string Metric => _definition.Metric;
        public long Target => _definition.Target;
        public bool IsHidden => _definition.IsHidden;
        public int CrystalReward => _definition.CrystalReward;

        #endregion

        #region Localized Properties

        public string Name => _localizationProvider.GetAchievementLocalization(_definition).Name;
        public string Description => _localizationProvider.GetAchievementLocalization(_definition).Description;
        public string UnlockMessage => _localizationProvider.GetAchievementLocalization(_definition).UnlockMessage;

        #endregion

        #region Progress Properties

        public AchievementProgress? Progress
        {
            get => _progress;
            set
            {
                if (SetProperty(ref _progress, value))
                {
                    OnPropertyChanged(nameof(IsUnlocked));
                    OnPropertyChanged(nameof(UnlockedAt));
                    OnPropertyChanged(nameof(CurrentProgress));
                    OnPropertyChanged(nameof(ProgressRatio));
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public bool IsUnlocked => _progress?.IsUnlocked ?? false;
        public DateTime? UnlockedAt => _progress?.UnlockedAt;
        public long CurrentProgress => _progress?.CurrentProgress ?? 0;

        public double ProgressRatio
        {
            get
            {
                if (Target <= 0) return 0;
                return Math.Min(1.0, (double)CurrentProgress / Target);
            }
        }

        public string ProgressText => $"{CurrentProgress:N0} / {Target:N0}";

        #endregion

        #region Display Properties

        public bool ShouldDisplay => !IsHidden || IsUnlocked;

        public string StatusText => IsUnlocked ? _localizationProvider.GetString("ui.common.unlocked") : $"{ProgressRatio:P0}";

        #endregion

        #region Methods

        /// <summary>
        /// 로컬라이제이션 새로고침 (언어 변경 시)
        /// </summary>
        public void RefreshLocalization()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(UnlockMessage));
        }

        /// <summary>
        /// 원본 Definition 가져오기
        /// </summary>
        public AchievementDefinition GetDefinition() => _definition;

        #endregion
    }
}
