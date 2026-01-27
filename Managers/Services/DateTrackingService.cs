using System;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Services
{
    /// <summary>
    /// 날짜 추적 서비스 (연속 플레이, 일일 리셋)
    /// </summary>
    public class DateTrackingService
    {
        /// <summary>
        /// 날짜 변경 확인 및 처리
        /// </summary>
        /// <returns>true: 날짜 변경됨, false: 같은 날</returns>
        public bool CheckAndUpdateDate(UserSave save)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            if (save.Stats.LastPlayed == today)
            {
                return false;
            }

            // 다른 날이면 오늘 입력 수 초기화
            save.Stats.TodayInputs = 0;
            save.Stats.LastPlayed = today;

            // 연속 플레이 체크
            UpdateConsecutiveDays(save.LifetimeStats, today);

            return true;
        }

        /// <summary>
        /// 연속 플레이 일수 업데이트
        /// </summary>
        private void UpdateConsecutiveDays(LifetimeStats lifetime, string today)
        {
            var lastDate = lifetime.LastPlayDate;

            if (string.IsNullOrEmpty(lastDate))
            {
                lifetime.ConsecutiveDays = 1;
            }
            else
            {
                if (DateTime.TryParse(lastDate, out var lastDateTime) &&
                    DateTime.TryParse(today, out var todayDateTime))
                {
                    var diff = (todayDateTime - lastDateTime).Days;
                    if (diff == 1)
                    {
                        lifetime.ConsecutiveDays++;
                    }
                    else if (diff > 1)
                    {
                        lifetime.ConsecutiveDays = 1;
                    }
                }
            }

            lifetime.LastPlayDate = today;
        }
    }
}
