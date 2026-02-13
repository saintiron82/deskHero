using System;
using System.Collections.Generic;
using System.Linq;
using DeskWarrior.Helpers;
using DeskWarrior.Models;

namespace DeskWarrior.Managers
{
    /// <summary>
    /// 보상 계산 및 지급 관리자
    /// - 골드 보상 (일반/보스/황금 고블린)
    /// - 크리스탈 보상 (보스/스테이지 클리어)
    /// - 도감 완성 보상 (마일스톤/종족 완성)
    /// </summary>
    public class RewardManager
    {
        #region Fields

        private readonly PermanentProgressionManager _permanentProgression;
        private readonly StatGrowthManager _statGrowth;
        private readonly MonsterCollection _monsterCollection;
        private readonly CollectionRewards _collectionRewards;
        private readonly SessionTracker _sessionTracker;
        private readonly GoldenGoblinManager _goldenGoblinManager;
        private readonly MonsterSpawnManager _monsterSpawnManager;
        private readonly SaveManager _saveManager;

        #endregion

        #region Events

        public event EventHandler<BossDropResult>? CrystalDropped;

        #endregion

        #region Constructor

        public RewardManager(
            PermanentProgressionManager permanentProgression,
            StatGrowthManager statGrowth,
            MonsterCollection monsterCollection,
            CollectionRewards collectionRewards,
            SessionTracker sessionTracker,
            GoldenGoblinManager goldenGoblinManager,
            MonsterSpawnManager monsterSpawnManager,
            SaveManager saveManager)
        {
            _permanentProgression = permanentProgression;
            _statGrowth = statGrowth;
            _monsterCollection = monsterCollection;
            _collectionRewards = collectionRewards;
            _sessionTracker = sessionTracker;
            _goldenGoblinManager = goldenGoblinManager;
            _monsterSpawnManager = monsterSpawnManager;
            _saveManager = saveManager;
        }

        #endregion

        #region Gold Rewards

        /// <summary>
        /// 일반/보스 몬스터 처치 시 골드 보상 계산
        /// </summary>
        /// <param name="monster">처치한 몬스터</param>
        /// <returns>획득할 골드량</returns>
        public int CalculateMonsterGoldReward(Monster monster)
        {
            // 골드 획득 공식 (영구 스탯만 사용)
            // 기본 = 몬스터 기본 골드
            double baseGold = monster.GoldReward;

            // +가산 = 기본 + gold_flat_perm (영구)
            var permStats = _saveManager.CurrentSave?.PermanentStats;
            double goldFlatPerm = _statGrowth.GetPermanentStatEffect("gold_flat_perm", permStats?.GoldFlatPermLevel ?? 0);
            double goldFlat = baseGold + goldFlatPerm;

            // ×배수 = +가산 × (1 + gold_multi_perm (영구))
            double goldMultiPerm = _statGrowth.GetPermanentStatEffect("gold_multi_perm", permStats?.GoldMultiPermLevel ?? 0) / 100.0;
            int goldReward = (int)(goldFlat * (1.0 + goldMultiPerm));

            return goldReward;
        }

        /// <summary>
        /// 황금 고블린 처치 시 보상 계산
        /// </summary>
        /// <param name="currentStage">현재 스테이지 번호</param>
        /// <returns>(보상 골드, 배율)</returns>
        public (int reward, int multiplier) CalculateGoldenGoblinReward(int currentStage)
        {
            int expectedGold = _monsterSpawnManager.CalculateStageExpectedGold(currentStage);
            int reward = _goldenGoblinManager.CalculateReward(expectedGold);
            int multiplier = reward / Math.Max(expectedGold, 1);

            return (reward, multiplier);
        }

        #endregion

        #region Crystal Rewards

        /// <summary>
        /// 스테이지 클리어 시 크리스탈 보상 지급
        /// </summary>
        /// <param name="clearedStage">클리어한 스테이지 번호</param>
        public void ProcessStageClearReward(int clearedStage)
        {
            _permanentProgression.ProcessStageClear(clearedStage);
        }

        /// <summary>
        /// 보스 처치 시 크리스탈 보상 지급
        /// </summary>
        /// <param name="bossLevel">보스 레벨</param>
        /// <param name="bossElement">보스 속성</param>
        /// <param name="elementProperties">속성별 크리스탈 배율</param>
        /// <returns>드롭 결과</returns>
        public BossDropResult ProcessBossKillReward(int bossLevel, string bossElement, Dictionary<string, ElementProperties> elementProperties)
        {
            // 속성별 크리스탈 배율 추출
            var crystalMultipliers = elementProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.CrystalMultiplier
            );

            // 보스 속성 전달하여 크리스탈 지급
            var dropResult = _permanentProgression.ProcessBossKill(
                bossLevel,
                bossElement,
                crystalMultipliers
            );

            // 드롭 이벤트 발생 (100% 지급이므로 조건 불필요)
            CrystalDropped?.Invoke(this, dropResult);

            return dropResult;
        }

        #endregion

        #region Collection Rewards

        /// <summary>
        /// 도감 완성 체크 및 보상 지급
        /// </summary>
        /// <param name="species">처치한 몬스터 종족</param>
        /// <param name="element">처치한 몬스터 속성</param>
        public void CheckAndGrantCollectionRewards(string species, string element)
        {
            // 첫 Holy/Dark 조우 마일스톤
            CheckFirstRareElementMilestone(species, element);

            // 종족 도감 완성 보상
            CheckSpeciesCompletionReward(species);

            // 모든 속성 최소 1마리 처치 마일스톤
            CheckAllElementsUnlockedMilestone();
        }

        /// <summary>
        /// 첫 Holy/Dark 조우 마일스톤 체크
        /// </summary>
        private void CheckFirstRareElementMilestone(string species, string element)
        {
            if ((element != "holy" && element != "dark"))
                return;

            // 이 종족의 이 속성을 처음 처치했는지 확인
            if (!_monsterCollection.KillCounts.TryGetValue(species, out var kills))
                return;

            if (!kills.TryGetValue(element, out var count) || count != 1)
                return;

            string milestoneId = $"first_{element}";
            if (_monsterCollection.HasClaimedReward(milestoneId))
                return;

            if (!_collectionRewards.MilestoneRewards.TryGetValue(milestoneId, out var milestone))
                return;

            // 보상 지급
            _permanentProgression.AddCrystals(milestone.Crystals, $"collection_{milestoneId}");
            _monsterCollection.ClaimReward(milestoneId);
            _sessionTracker.RecordAchievementCrystals(milestone.Crystals);

            Logger.Log($"[CollectionReward] First {element} milestone: {milestone.Crystals} crystals");
        }

        /// <summary>
        /// 종족 도감 완성 보상 체크
        /// </summary>
        private void CheckSpeciesCompletionReward(string species)
        {
            if (!_monsterCollection.IsSpeciesComplete(species))
                return;

            string rewardId = $"species_{species}";
            if (_monsterCollection.HasClaimedReward(rewardId))
                return;

            if (!_collectionRewards.SpeciesCompletion.TryGetValue(species, out var speciesReward))
                return;

            // 보상 지급
            _permanentProgression.AddCrystals(speciesReward.Rewards.Crystals, $"collection_species_{species}");
            _monsterCollection.ClaimReward(rewardId);
            _sessionTracker.RecordAchievementCrystals(speciesReward.Rewards.Crystals);

            Logger.Log($"[CollectionReward] Species {species} complete: {speciesReward.Rewards.Crystals} crystals");

            // TODO: 칭호 및 영구 보너스 적용 (향후 구현)
        }

        /// <summary>
        /// 모든 속성 최소 1마리 처치 마일스톤 체크
        /// </summary>
        private void CheckAllElementsUnlockedMilestone()
        {
            var allElements = new[] { "normal", "fire", "ice", "wind", "holy", "dark" };
            bool hasAllElements = allElements.All(elem =>
                _monsterCollection.EncounteredVariations.Values.Any(set => set.Contains(elem)));

            if (!hasAllElements)
                return;

            string milestoneId = "all_elements_unlocked";
            if (_monsterCollection.HasClaimedReward(milestoneId))
                return;

            if (!_collectionRewards.MilestoneRewards.TryGetValue(milestoneId, out var allElementsMilestone))
                return;

            // 보상 지급
            _permanentProgression.AddCrystals(allElementsMilestone.Crystals, "collection_all_elements");
            _monsterCollection.ClaimReward(milestoneId);
            _sessionTracker.RecordAchievementCrystals(allElementsMilestone.Crystals);

            Logger.Log($"[CollectionReward] All elements unlocked: {allElementsMilestone.Crystals} crystals");
        }

        #endregion
    }
}
