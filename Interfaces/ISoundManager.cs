using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DeskWarrior.Models;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 사운드 타입 열거형
    /// </summary>
    public enum SoundType
    {
        KeyboardHit,    // 키보드 입력
        MouseClick,     // 마우스 클릭
        Defeat,         // 몬스터 처치
        Upgrade,        // 업그레이드
        GameOver,       // 하드 리셋
        Achievement,    // 업적 해금
        BossAppear,     // 보스 등장
        Critical,       // 크리티컬 히트
        BossDefeat,     // 보스 처치
        Combo,          // 콤보 발동
        LevelUp,        // 레벨업
        OfflineReward,  // 오프라인 보상 수령
        GoldenGoblinAppear,  // 황금 고블린 등장
        GoldenGoblinDefeat,  // 황금 고블린 처치
        GoldenGoblinEscape,  // 황금 고블린 도주

        // 레거시 호환 (deprecated)
        [Obsolete("Use KeyboardHit instead")]
        Hit = KeyboardHit
    }

    /// <summary>
    /// 사운드 카테고리 정의 (카테고리 이름 → 포함된 SoundType 목록)
    /// </summary>
    public static class SoundCategory
    {
        public const string Input = "Input";
        public const string Combat = "Combat";
        public const string Progression = "Progression";
        public const string Event = "Event";
        public const string Special = "Special";

        /// <summary>전체 카테고리 이름 목록 (UI 표시 순서)</summary>
        public static readonly IReadOnlyList<string> AllCategories = new[]
        {
            Input, Combat, Progression, Event, Special
        };

        /// <summary>각 카테고리에 속한 SoundType 목록 (첫 항목 = 프리뷰 사운드)</summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<SoundType>> CategorySoundTypes =
            new ReadOnlyDictionary<string, IReadOnlyList<SoundType>>(
                new Dictionary<string, IReadOnlyList<SoundType>>
                {
                    [Input] = new[] { SoundType.KeyboardHit, SoundType.MouseClick },
                    [Combat] = new[] { SoundType.Defeat, SoundType.Critical, SoundType.BossAppear, SoundType.BossDefeat },
                    [Progression] = new[] { SoundType.LevelUp, SoundType.Upgrade, SoundType.OfflineReward },
                    [Event] = new[] { SoundType.Achievement, SoundType.Combo, SoundType.GameOver },
                    [Special] = new[] { SoundType.GoldenGoblinAppear, SoundType.GoldenGoblinDefeat, SoundType.GoldenGoblinEscape },
                });

        /// <summary>카테고리 프리뷰 사운드 (첫 번째 항목)</summary>
        public static SoundType GetPreviewSound(string category) =>
            CategorySoundTypes.TryGetValue(category, out var list) ? list[0] : SoundType.KeyboardHit;

        /// <summary>SoundType이 속한 카테고리 이름을 반환</summary>
        public static string GetCategory(SoundType type)
        {
            foreach (var kvp in CategorySoundTypes)
                if (kvp.Value.Contains(type))
                    return kvp.Key;
            return Input;
        }
    }

    /// <summary>
    /// 사운드팩 정보
    /// </summary>
    public class SoundPackInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameLocalized { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public bool IsBuiltin { get; set; }
        public bool IsCustom { get; set; }
        public string FolderPath { get; set; } = "";
    }

    /// <summary>
    /// 사운드팩 변경 이벤트 인자
    /// </summary>
    public class SoundPackChangedEventArgs : EventArgs
    {
        public string OldPackId { get; set; } = "";
        public string NewPackId { get; set; } = "";
    }

    /// <summary>
    /// 사운드 관리자 인터페이스 (SOLID: DIP, ISP)
    /// </summary>
    public interface ISoundManager : IDisposable
    {
        /// <summary>
        /// 사운드 활성화 여부
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 볼륨 (0.0 ~ 1.0)
        /// </summary>
        double Volume { get; set; }

        /// <summary>
        /// 현재 사운드팩 ID (전역)
        /// </summary>
        string CurrentSoundPackId { get; }

        /// <summary>
        /// 카테고리별 현재 사운드팩 ID
        /// </summary>
        IReadOnlyDictionary<string, string> CategorySoundPackIds { get; }

        /// <summary>
        /// 사용 가능한 사운드팩 목록
        /// </summary>
        IReadOnlyList<SoundPackInfo> AvailableSoundPacks { get; }

        /// <summary>
        /// 사운드팩 변경 (전체 카테고리에 일괄 적용)
        /// </summary>
        bool ChangeSoundPack(string packId);

        /// <summary>
        /// 특정 카테고리의 사운드팩 변경
        /// </summary>
        bool ChangeCategorySoundPack(string category, string packId);

        /// <summary>
        /// 카테고리 설정 일괄 적용 (앱 시작 시 호출)
        /// </summary>
        void ApplyCategorySettings(Dictionary<string, string> categoryPacks, string globalPack);

        /// <summary>
        /// 사운드 재생
        /// </summary>
        void Play(SoundType soundType);

        /// <summary>
        /// 사운드팩 새로고침 (커스텀 팩 스캔)
        /// </summary>
        void RefreshSoundPacks();

        /// <summary>
        /// 사운드팩 변경 이벤트
        /// </summary>
        event EventHandler<SoundPackChangedEventArgs>? SoundPackChanged;

        // ── 사운드 테마 ──

        /// <summary>사용 가능한 테마 목록</summary>
        IReadOnlyList<SoundThemeData> AvailableSoundThemes { get; }

        /// <summary>현재 적용된 테마 ID</summary>
        string CurrentThemeId { get; }

        /// <summary>테마 적용 (override 초기화)</summary>
        bool ApplySoundTheme(string themeId);

        /// <summary>테마 + override 일괄 적용 (앱 시작 시)</summary>
        void ApplyThemeSettings(string themeId, Dictionary<string, double> overrides);

        /// <summary>개별 사운드 override 설정 (UI에서 실시간 변경)</summary>
        void SetSoundTypeOverride(string soundTypeKey, double volume);

        /// <summary>커스텀 테마 추가 (JSON 저장 포함)</summary>
        bool AddCustomTheme(SoundThemeData theme);

        /// <summary>커스텀 테마 삭제 (builtin 삭제 불가)</summary>
        bool DeleteCustomTheme(string themeId);
    }
}
