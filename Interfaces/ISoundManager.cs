using System;
using System.Collections.Generic;

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

        // 레거시 호환 (deprecated)
        [Obsolete("Use KeyboardHit instead")]
        Hit = KeyboardHit
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
        /// 현재 사운드팩 ID
        /// </summary>
        string CurrentSoundPackId { get; }

        /// <summary>
        /// 사용 가능한 사운드팩 목록
        /// </summary>
        IReadOnlyList<SoundPackInfo> AvailableSoundPacks { get; }

        /// <summary>
        /// 사운드팩 변경
        /// </summary>
        bool ChangeSoundPack(string packId);

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
    }
}
