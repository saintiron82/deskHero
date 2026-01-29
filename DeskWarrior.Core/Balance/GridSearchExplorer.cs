using DeskWarrior.Core.Models;

namespace DeskWarrior.Core.Balance;

/// <summary>
/// Grid Search 탐색기
/// 배분 비율을 격자 단위로 탐색하여 모든 조합 생성
/// </summary>
public class GridSearchExplorer
{
    private readonly string[] _allStatIds;
    private readonly int _granularity;  // 10 = 10% 단위

    public GridSearchExplorer(IEnumerable<string> statIds, int granularity = 10)
    {
        _allStatIds = statIds.ToArray();
        _granularity = granularity;
    }

    /// <summary>
    /// 주요 스탯에 대한 Grid 패턴 생성
    /// </summary>
    /// <param name="focusStats">집중할 스탯들 (5-6개 권장)</param>
    /// <returns>모든 가능한 배분 조합</returns>
    public IEnumerable<AllocationPattern> GenerateGridPatterns(string[] focusStats)
    {
        if (focusStats.Length == 0)
            yield break;

        // 합이 100%인 모든 조합 생성
        foreach (var allocation in GeneratePartitions(focusStats.Length, _granularity))
        {
            var pattern = new AllocationPattern();
            for (int i = 0; i < focusStats.Length; i++)
            {
                pattern.Allocation[focusStats[i]] = allocation[i] / 100.0;
            }
            yield return pattern;
        }
    }

    /// <summary>
    /// 단일 스탯 전용 패턴 생성 (19개)
    /// </summary>
    public IEnumerable<AllocationPattern> GenerateSingleStatPatterns()
    {
        foreach (var statId in _allStatIds)
        {
            var pattern = new AllocationPattern
            {
                PatternId = $"single_{statId}"
            };
            pattern.Allocation[statId] = 1.0;
            yield return pattern;
        }
    }

    /// <summary>
    /// 2개 스탯 조합 패턴 생성
    /// </summary>
    public IEnumerable<AllocationPattern> GenerateTwoStatPatterns()
    {
        for (int i = 0; i < _allStatIds.Length; i++)
        {
            for (int j = i + 1; j < _allStatIds.Length; j++)
            {
                // 다양한 비율로
                foreach (var ratio in new[] { 0.3, 0.5, 0.7 })
                {
                    var pattern = new AllocationPattern
                    {
                        PatternId = $"duo_{_allStatIds[i]}_{_allStatIds[j]}_{(int)(ratio * 100)}"
                    };
                    pattern.Allocation[_allStatIds[i]] = ratio;
                    pattern.Allocation[_allStatIds[j]] = 1.0 - ratio;
                    yield return pattern;
                }
            }
        }
    }

    /// <summary>
    /// 카테고리 균형 패턴 생성
    /// </summary>
    public IEnumerable<AllocationPattern> GenerateCategoryBalancedPatterns(
        Dictionary<string, string[]> categories)
    {
        // 각 카테고리에서 하나씩 선택
        var categoryStats = categories.Values
            .Select(stats => stats.FirstOrDefault())
            .Where(s => s != null)
            .ToArray();

        if (categoryStats.Length == 0)
            yield break;

        foreach (var allocation in GeneratePartitions(categoryStats.Length!, _granularity))
        {
            var pattern = new AllocationPattern
            {
                PatternId = $"balanced_{string.Join("_", allocation)}"
            };
            for (int i = 0; i < categoryStats.Length; i++)
            {
                if (categoryStats[i] != null)
                    pattern.Allocation[categoryStats[i]!] = allocation[i] / 100.0;
            }
            yield return pattern;
        }
    }

    /// <summary>
    /// 합이 100인 모든 정수 파티션 생성 (n개 변수, 격자 단위)
    /// </summary>
    private IEnumerable<int[]> GeneratePartitions(int n, int granularity)
    {
        int target = 100;
        int step = 100 / granularity;  // 10% 단위면 step = 10

        var current = new int[n];
        return GeneratePartitionsRecursive(current, 0, target, step);
    }

    private IEnumerable<int[]> GeneratePartitionsRecursive(int[] current, int index, int remaining, int step)
    {
        if (index == current.Length - 1)
        {
            // 마지막 변수: 나머지 전부 할당
            current[index] = remaining;
            yield return (int[])current.Clone();
            yield break;
        }

        // 현재 변수에 0부터 remaining까지 할당
        for (int value = 0; value <= remaining; value += step)
        {
            current[index] = value;
            foreach (var result in GeneratePartitionsRecursive(current, index + 1, remaining - value, step))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// 효율 높은 상위 스탯 자동 선택 (비용 기반 - fallback용)
    /// </summary>
    public string[] IdentifyTopStats(StatCostCalculator costCalculator, int count = 5)
    {
        // 비용 대비 효과가 좋은 스탯 선택
        return _allStatIds
            .Select(id => (id, efficiency: costCalculator.GetEfficiency(id, 0)))
            .OrderByDescending(x => x.efficiency)
            .Take(count)
            .Select(x => x.id)
            .ToArray();
    }

    /// <summary>
    /// 단일 스탯 시뮬레이션 결과 기반 Focus 스탯 선택
    /// - 상위 3개: 검증된 강한 스탯
    /// - 하위 1개: 숨겨진 시너지 탐색용
    /// - 랜덤 1개: 다양성 확보
    /// </summary>
    public string[] IdentifyFocusStatsByPerformance(
        IEnumerable<AllocationPattern> singleStatResults,
        Random? random = null)
    {
        random ??= new Random();

        var ranked = singleStatResults
            .Where(p => p.Result != null)
            .OrderByDescending(p => p.Result!.AverageMaxLevel)
            .ToList();

        if (ranked.Count < 5)
        {
            return ranked.Select(p => p.Allocation.Keys.First()).ToArray();
        }

        var selected = new List<string>();

        // 상위 3개 (검증된 강자)
        var top3 = ranked.Take(3).Select(p => p.Allocation.Keys.First()).ToList();
        selected.AddRange(top3);

        // 하위 1개 (숨겨진 시너지 탐색)
        var bottom1 = ranked.TakeLast(1).Select(p => p.Allocation.Keys.First()).First();
        selected.Add(bottom1);

        // 중간에서 랜덤 1개 (다양성)
        var middle = ranked
            .Skip(3)
            .SkipLast(1)
            .Select(p => p.Allocation.Keys.First())
            .Where(id => !selected.Contains(id))
            .ToList();

        if (middle.Count > 0)
        {
            var randomPick = middle[random.Next(middle.Count)];
            selected.Add(randomPick);
        }

        return selected.ToArray();
    }

    /// <summary>
    /// 카테고리 균형을 고려한 Focus 스탯 선택
    /// - 각 카테고리에서 최소 1개씩
    /// - 나머지는 성능 기반
    /// </summary>
    public string[] IdentifyFocusStatsBalanced(
        IEnumerable<AllocationPattern> singleStatResults,
        Dictionary<string, string[]> categories,
        int count = 5)
    {
        var ranked = singleStatResults
            .Where(p => p.Result != null)
            .OrderByDescending(p => p.Result!.AverageMaxLevel)
            .ToDictionary(
                p => p.Allocation.Keys.First(),
                p => p.Result!.AverageMaxLevel
            );

        var selected = new List<string>();

        // 각 카테고리에서 최고 성능 스탯 1개씩
        foreach (var (category, stats) in categories)
        {
            var bestInCategory = stats
                .Where(s => ranked.ContainsKey(s))
                .OrderByDescending(s => ranked[s])
                .FirstOrDefault();

            if (bestInCategory != null && selected.Count < count)
            {
                selected.Add(bestInCategory);
            }
        }

        // 남은 자리는 전체 성능 순으로 채움
        var remaining = ranked.Keys
            .Where(id => !selected.Contains(id))
            .OrderByDescending(id => ranked[id]);

        foreach (var id in remaining)
        {
            if (selected.Count >= count) break;
            selected.Add(id);
        }

        return selected.ToArray();
    }

    /// <summary>
    /// 현재 + 과거 분석 결과를 종합한 Focus 스탯 선택
    ///
    /// 선택 로직:
    /// - 현재 분석 상위 3개 (검증된 강자)
    /// - 과거 분석 상위 1-2개 (이전에 강했던 스탯 - 중복 시 스킵)
    /// - 현재 분석 최하위 1개 (숨겨진 시너지 탐색)
    /// </summary>
    public (string[] focusStats, FocusSelectionInfo info) IdentifyFocusStatsWithHistory(
        IEnumerable<AllocationPattern> currentSingleResults,
        string? historyTop1,
        string? historyTop2,
        string? historyBottom1,
        int count = 6)
    {
        var ranked = currentSingleResults
            .Where(p => p.Result != null)
            .OrderByDescending(p => p.Result!.AverageMaxLevel)
            .ToList();

        var rankedIds = ranked.Select(p => p.Allocation.Keys.First()).ToList();
        var selected = new List<(string id, string source, int rank)>();

        // 1. 현재 상위 3개
        for (int i = 0; i < Math.Min(3, rankedIds.Count); i++)
        {
            selected.Add((rankedIds[i], "CUR_TOP", i + 1));
        }

        // 2. 과거 상위 1위 (중복 아니면 추가)
        if (!string.IsNullOrEmpty(historyTop1) &&
            rankedIds.Contains(historyTop1) &&
            !selected.Any(s => s.id == historyTop1))
        {
            var rank = rankedIds.IndexOf(historyTop1) + 1;
            selected.Add((historyTop1, "HIST_TOP1", rank));
        }

        // 3. 과거 상위 2위 (중복 아니면 추가)
        if (!string.IsNullOrEmpty(historyTop2) &&
            rankedIds.Contains(historyTop2) &&
            !selected.Any(s => s.id == historyTop2) &&
            selected.Count < count)
        {
            var rank = rankedIds.IndexOf(historyTop2) + 1;
            selected.Add((historyTop2, "HIST_TOP2", rank));
        }

        // 4. 현재 최하위 1개 (숨겨진 시너지 탐색)
        if (rankedIds.Count > 0 && selected.Count < count)
        {
            var bottomId = rankedIds.Last();
            if (!selected.Any(s => s.id == bottomId))
            {
                selected.Add((bottomId, "CUR_BTM", rankedIds.Count));
            }
        }

        // 5. 과거 최하위 (여전히 탐색 안 된 스탯)
        if (!string.IsNullOrEmpty(historyBottom1) &&
            rankedIds.Contains(historyBottom1) &&
            !selected.Any(s => s.id == historyBottom1) &&
            selected.Count < count)
        {
            var rank = rankedIds.IndexOf(historyBottom1) + 1;
            selected.Add((historyBottom1, "HIST_BTM", rank));
        }

        // 6. 부족하면 중간에서 랜덤 추가
        var random = new Random();
        var middle = rankedIds
            .Skip(3)
            .SkipLast(1)
            .Where(id => !selected.Any(s => s.id == id))
            .ToList();

        while (selected.Count < count && middle.Count > 0)
        {
            var pick = middle[random.Next(middle.Count)];
            var rank = rankedIds.IndexOf(pick) + 1;
            selected.Add((pick, "RANDOM", rank));
            middle.Remove(pick);
        }

        var info = new FocusSelectionInfo
        {
            Selections = selected.Select(s => new FocusStatSelection
            {
                StatId = s.id,
                Source = s.source,
                CurrentRank = s.rank
            }).ToList(),
            TotalStats = rankedIds.Count
        };

        return (selected.Select(s => s.id).ToArray(), info);
    }
}

/// <summary>
/// Focus 스탯 선택 정보 (로깅/디버그용)
/// </summary>
public class FocusSelectionInfo
{
    public List<FocusStatSelection> Selections { get; set; } = new();
    public int TotalStats { get; set; }

    public override string ToString()
    {
        return string.Join(", ", Selections.Select(s => $"{s.StatId}(#{s.CurrentRank}/{s.Source})"));
    }
}

public class FocusStatSelection
{
    public string StatId { get; set; } = "";
    public string Source { get; set; } = "";  // CUR_TOP, HIST_TOP1, HIST_TOP2, CUR_BTM, HIST_BTM, RANDOM
    public int CurrentRank { get; set; }
}
