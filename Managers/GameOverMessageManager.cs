using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 게임 오버 메시지 선택 및 관리
    /// </summary>
    public class GameOverMessageManager
    {
        #region Fields

        private readonly GameOverMessageData? _messageData;
        private readonly Random _random = new();

        #endregion

        #region Constructor

        public GameOverMessageManager()
        {
            _messageData = LoadMessagesFromAssets();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 게임 상태에 맞는 메시지 선택
        /// </summary>
        /// <param name="level">도달한 레벨</param>
        /// <param name="gold">획득한 골드</param>
        /// <param name="damage">가한 총 데미지</param>
        /// <param name="kills">처치한 몬스터 수</param>
        /// <param name="deathType">사망 타입 ("boss", "timeout", "normal")</param>
        /// <returns>선택된 메시지 (변수 치환 완료)</returns>
        public string SelectMessage(int level, long gold, long damage, int kills, string? deathType = null)
        {
            if (_messageData?.GameOverMessages == null)
            {
                return GetFallbackMessage(level, gold, damage, kills);
            }

            var messages = _messageData.GameOverMessages;

            // 1. 조건부 메시지 체크 (우선순위 순)
            var matchedRule = messages.Conditions
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault(r => MatchesCondition(r.Condition, level, deathType));

            if (matchedRule != null && matchedRule.Messages.Count > 0)
            {
                string message = matchedRule.Messages[_random.Next(matchedRule.Messages.Count)];
                return ReplaceVariables(message, level, gold, damage, kills);
            }

            // 2. 레벨 범위 기반 메시지
            var levelMessage = GetLevelBasedMessage(messages.LevelBased, level);
            if (levelMessage != null)
            {
                return ReplaceVariables(levelMessage, level, gold, damage, kills);
            }

            // 3. Fallback 메시지
            if (messages.Fallback.Count > 0)
            {
                string fallbackMsg = messages.Fallback[_random.Next(messages.Fallback.Count)];
                return ReplaceVariables(fallbackMsg, level, gold, damage, kills);
            }

            // 4. 최종 기본 메시지
            return GetFallbackMessage(level, gold, damage, kills);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Assets에서 JSON 로드
        /// </summary>
        private GameOverMessageData? LoadMessagesFromAssets()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/Data/GameOverMessages.json");
                var resourceStream = Application.GetResourceStream(uri);

                if (resourceStream == null)
                {
                    System.Diagnostics.Debug.WriteLine("GameOverMessages.json not found in Assets");
                    return null;
                }

                using var reader = new StreamReader(resourceStream.Stream);
                string json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<GameOverMessageData>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load GameOverMessages: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 조건 매칭 체크
        /// </summary>
        private bool MatchesCondition(MessageCondition condition, int level, string? deathType)
        {
            // level 정확히 일치
            if (condition.Level.HasValue && condition.Level.Value != level)
                return false;

            // level_min ~ level_max 범위
            if (condition.LevelMin.HasValue && level < condition.LevelMin.Value)
                return false;

            if (condition.LevelMax.HasValue && level > condition.LevelMax.Value)
                return false;

            // death_type 매칭
            if (!string.IsNullOrEmpty(condition.DeathType) && condition.DeathType != deathType)
                return false;

            return true;
        }

        /// <summary>
        /// 레벨 범위 기반 메시지 선택
        /// </summary>
        private string? GetLevelBasedMessage(Dictionary<string, List<string>> levelBased, int level)
        {
            string? key = null;

            // 범위 매칭 (1-3, 4-9, 10-19, 20-49, 50+)
            if (level >= 1 && level <= 3)
                key = "1-3";
            else if (level >= 4 && level <= 9)
                key = "4-9";
            else if (level >= 10 && level <= 19)
                key = "10-19";
            else if (level >= 20 && level <= 49)
                key = "20-49";
            else if (level >= 50)
                key = "50+";

            if (key != null && levelBased.TryGetValue(key, out var messageList) && messageList.Count > 0)
            {
                return messageList[_random.Next(messageList.Count)];
            }

            return null;
        }

        /// <summary>
        /// 변수 치환
        /// </summary>
        private string ReplaceVariables(string message, int level, long gold, long damage, int kills)
        {
            return message
                .Replace("{level}", level.ToString())
                .Replace("{gold}", gold.ToString("N0"))
                .Replace("{damage}", damage.ToString("N0"))
                .Replace("{kills}", kills.ToString());
        }

        /// <summary>
        /// 하드코딩된 기본 메시지
        /// </summary>
        private string GetFallbackMessage(int level, long gold, long damage, int kills)
        {
            return $"GAME OVER - Level {level} | {gold:N0}G | {damage:N0} DMG";
        }

        #endregion
    }
}
