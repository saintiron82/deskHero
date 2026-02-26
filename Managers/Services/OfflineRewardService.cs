using System;
using System.IO;
using System.Text.Json;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Services
{
    /// <summary>
    /// 오프라인 보상 계산 서비스
    /// </summary>
    public class OfflineRewardService
    {
        private readonly OfflineRewardConfig _config;

        public OfflineRewardService()
        {
            _config = LoadConfig();
        }

        /// <summary>
        /// 오프라인 보상 활성화 여부
        /// </summary>
        public bool IsEnabled => _config.Enabled;

        /// <summary>
        /// 오프라인 보상 계산
        /// </summary>
        public OfflineRewardResult CalculateReward(DateTime lastOnline)
        {
            if (!_config.Enabled)
                return OfflineRewardResult.None;

            var offlineTime = DateTime.Now - lastOnline;

            // 최소 오프라인 시간 미달
            if (offlineTime.TotalMinutes < _config.MinOfflineMinutes)
                return OfflineRewardResult.None;

            // 최대 시간 제한 적용
            var hours = Math.Min(offlineTime.TotalHours, _config.MaxHours);

            return new OfflineRewardResult
            {
                Gold = (long)(hours * _config.RewardsPerHour.Gold),
                Crystals = (int)(hours * _config.RewardsPerHour.Crystals),
                OfflineHours = hours
            };
        }

        /// <summary>
        /// 팝업 메시지 생성
        /// </summary>
        public string GetPopupMessage(OfflineRewardResult result, string languageCode)
        {
            string? template;
            if (!_config.PopupMessage.TryGetValue(languageCode, out template))
            {
                if (!_config.PopupMessage.TryGetValue("en-US", out template))
                {
                    template = "Welcome back! You earned {gold} gold and {crystals} crystals during {hours} hours offline!";
                }
            }

            return template
                .Replace("{hours}", result.OfflineHours.ToString("F1"))
                .Replace("{gold}", result.Gold.ToString("N0"))
                .Replace("{crystals}", result.Crystals.ToString("N0"));
        }

        private static OfflineRewardConfig LoadConfig()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "OfflineRewards.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<OfflineRewardConfig>(json) ?? new OfflineRewardConfig();
                }
            }
            catch (Exception ex)
            {
                Helpers.Logger.Log($"[OfflineRewardService] Failed to load config: {ex.Message}");
            }

            return new OfflineRewardConfig();
        }
    }
}
