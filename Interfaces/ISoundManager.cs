using System;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 사운드 타입 열거형
    /// </summary>
    public enum SoundType
    {
        Hit,        // 공격 시
        Defeat,     // 몬스터 처치
        Upgrade,    // 업그레이드
        GameOver,   // 하드 리셋
        Achievement,// 업적 해금
        BossAppear  // 보스 등장
    }

    /// <summary>
    /// 사운드 관리자 인터페이스 (SOLID: DIP, ISP)
    /// </summary>
    public interface ISoundManager : IDisposable
    {
        /// <summary>
        /// 볼륨 (0.0 ~ 1.0)
        /// </summary>
        double Volume { get; set; }

        /// <summary>
        /// 사운드 재생
        /// </summary>
        void Play(SoundType soundType);
    }
}
