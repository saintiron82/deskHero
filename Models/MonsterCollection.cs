using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 몬스터 도감 데이터 (세이브 파일에 저장됨)
    /// </summary>
    public class MonsterCollection
    {
        /// <summary>
        /// 종족별 조우한 속성 목록 (species -> {element1, element2, ...})
        /// </summary>
        [JsonPropertyName("encountered_variations")]
        public Dictionary<string, HashSet<string>> EncounteredVariations { get; set; } = new();

        /// <summary>
        /// 종족별 속성별 처치 횟수 (species -> element -> count)
        /// </summary>
        [JsonPropertyName("kill_counts")]
        public Dictionary<string, Dictionary<string, int>> KillCounts { get; set; } = new();

        /// <summary>
        /// 보상 수령 기록 (reward_id)
        /// </summary>
        [JsonPropertyName("claimed_rewards")]
        public HashSet<string> ClaimedRewards { get; set; } = new();

        /// <summary>
        /// 첫 조우 타임스탬프 (species_element -> timestamp)
        /// </summary>
        [JsonPropertyName("first_encounters")]
        public Dictionary<string, DateTime> FirstEncounters { get; set; } = new();

        /// <summary>
        /// 종족 도감 완성 여부 확인
        /// </summary>
        /// <param name="species">종족 이름</param>
        /// <param name="requiredVariations">필요한 속성 개수 (기본 6개)</param>
        /// <returns>완성 여부</returns>
        public bool IsSpeciesComplete(string species, int requiredVariations = 6)
        {
            return EncounteredVariations.TryGetValue(species, out var variations)
                && variations.Count >= requiredVariations;
        }

        /// <summary>
        /// 배치 완성도 계산
        /// </summary>
        /// <param name="speciesInBatch">배치에 속한 종족 목록</param>
        /// <param name="requiredVariations">종족당 필요한 속성 개수</param>
        /// <returns>완성도 퍼센트 (0~100)</returns>
        public double GetBatchCompletion(List<string> speciesInBatch, int requiredVariations = 6)
        {
            if (speciesInBatch.Count == 0) return 0;

            int completedSpecies = speciesInBatch.Count(species => IsSpeciesComplete(species, requiredVariations));
            return (double)completedSpecies / speciesInBatch.Count * 100;
        }

        /// <summary>
        /// 몬스터 처치 기록
        /// </summary>
        /// <param name="species">종족</param>
        /// <param name="element">속성</param>
        public void RecordKill(string species, string element)
        {
            // 조우 기록
            if (!EncounteredVariations.ContainsKey(species))
            {
                EncounteredVariations[species] = new HashSet<string>();
            }

            bool isFirstEncounter = !EncounteredVariations[species].Contains(element);
            EncounteredVariations[species].Add(element);

            // 처치 횟수 증가
            if (!KillCounts.ContainsKey(species))
            {
                KillCounts[species] = new Dictionary<string, int>();
            }
            if (!KillCounts[species].ContainsKey(element))
            {
                KillCounts[species][element] = 0;
            }
            KillCounts[species][element]++;

            // 첫 조우 시간 기록
            if (isFirstEncounter)
            {
                string key = $"{species}_{element}";
                FirstEncounters[key] = DateTime.Now;
            }
        }

        /// <summary>
        /// 특정 종족의 완성도 퍼센트
        /// </summary>
        public double GetSpeciesCompletion(string species, int requiredVariations = 6)
        {
            if (!EncounteredVariations.TryGetValue(species, out var variations))
            {
                return 0;
            }
            return (double)variations.Count / requiredVariations * 100;
        }

        /// <summary>
        /// 특정 몬스터(종족+속성)를 처치했는지 확인
        /// </summary>
        public bool HasEncountered(string species, string element)
        {
            return EncounteredVariations.TryGetValue(species, out var variations)
                && variations.Contains(element);
        }

        /// <summary>
        /// 특정 보상을 수령했는지 확인
        /// </summary>
        public bool HasClaimedReward(string rewardId)
        {
            return ClaimedRewards.Contains(rewardId);
        }

        /// <summary>
        /// 보상 수령 처리
        /// </summary>
        public void ClaimReward(string rewardId)
        {
            ClaimedRewards.Add(rewardId);
        }
    }
}
