using System.Collections.Generic;
using DeskWarrior.Models;

namespace DeskWarrior.Interfaces
{
    /// <summary>
    /// 로컬라이제이션 제공자 인터페이스 (의존성 역전)
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>
        /// 현재 언어 코드
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// 키로 로컬라이즈된 문자열 가져오기
        /// </summary>
        string GetString(string key);

        /// <summary>
        /// 형식 문자열로 로컬라이즈된 문자열 가져오기
        /// </summary>
        string Format(string key, params object[] args);

        /// <summary>
        /// 업적 로컬라이즈 텍스트 가져오기
        /// </summary>
        AchievementLocalization GetAchievementLocalization(AchievementDefinition definition);
    }
}
