using System.Diagnostics;
using DeskWarrior.Core.Balance;
using DeskWarrior.Core.Models;
using DeskWarrior.Core.Simulation;

namespace DeskWarrior.Simulator;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== DeskWarrior Simulator ===\n");

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return;
        }

        if (args.Contains("--debug"))
        {
            DebugRunner.Run(FindConfigPath());
            return;
        }

        if (args.Contains("--progress"))
        {
            try
            {
                var options = ParseArgs(args);
                RunProgressionSimulation(options);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            return;
        }

        if (args.Contains("--analyze"))
        {
            try
            {
                var options = ParseArgs(args);
                RunBalanceAnalysis(options);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            return;
        }

        try
        {
            var options = ParseArgs(args);
            RunSimulation(options);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine(@"Usage: DeskWarrior.Simulator [options]

Modes:
  (default)            Single session batch simulation
  --progress           Multi-session progression simulation
  --analyze            Balance route diversity analysis
  --debug              Debug single session step-by-step

Options:
  --target <level>     Target level to analyze (default: 50)
  --cps <value>        Clicks per second (default: 5.0)
  --runs <count>       Number of simulations (default: 1000)
  --combo <level>      Combo skill: none/beginner/intermediate/expert/perfect (default: none)
  --parallel <count>   Parallel threads (-1 = all cores, default: -1)

Progression Options (--progress mode):
  --strategy <name>    Upgrade strategy: greedy/damage/survival/crystal/balanced/none (default: greedy)
  --max-attempts <n>   Maximum session attempts (default: 1000)
  --game-hours <n>     Game time to simulate in hours (default: 0 = use target level instead)
                       When set, simulates until game time reached instead of target level

Analysis Options (--analyze mode):
  --crystals <n>       Crystal budget for pattern testing (default: 1000)
  --quick              Quick analysis (single-stat patterns only)
  --output <path>      Output report path (auto-generates .json and .md files)
  --raw-data           Include raw pattern data in report

Permanent Stats (level):
  --base-attack <n>    Base Attack level
  --crit-chance <n>    Critical Chance level
  --crit-damage <n>    Critical Damage level
  --multi-hit <n>      Multi-Hit level
  --time-extend <n>    Time Extension level
  --gold-flat <n>      Gold Flat level
  --gold-multi <n>     Gold Multi level
  --start-level <n>    Start Level bonus
  --start-gold <n>     Start Gold level

Output:
  --verbose            Show detailed output
  --json               Output as JSON
  --help, -h           Show this help

Examples:
  simulate --target 50 --cps 5 --runs 10000
  simulate --target 100 --cps 7 --combo expert --base-attack 10
  simulate --progress --target 100 --cps 5 --strategy greedy
  simulate --progress --game-hours 10 --cps 5 --strategy greedy
  simulate --analyze --cps 5 --crystals 500 --target 100
  simulate --analyze --cps 5 --crystals 1000 --output balanceDoc/report
");
    }

    static SimulationOptions ParseArgs(string[] args)
    {
        var options = new SimulationOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var next = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "--target":
                    options.TargetLevel = int.Parse(next!);
                    i++;
                    break;
                case "--cps":
                    options.Cps = double.Parse(next!);
                    i++;
                    break;
                case "--runs":
                    options.NumRuns = int.Parse(next!);
                    i++;
                    break;
                case "--combo":
                    options.ComboSkill = ParseComboSkill(next!);
                    i++;
                    break;
                case "--parallel":
                    options.Parallelism = int.Parse(next!);
                    i++;
                    break;
                case "--base-attack":
                    options.PermanentStats.BaseAttackLevel = int.Parse(next!);
                    i++;
                    break;
                case "--crit-chance":
                    options.PermanentStats.CritChanceLevel = int.Parse(next!);
                    i++;
                    break;
                case "--crit-damage":
                    options.PermanentStats.CritDamageLevel = int.Parse(next!);
                    i++;
                    break;
                case "--multi-hit":
                    options.PermanentStats.MultiHitLevel = int.Parse(next!);
                    i++;
                    break;
                case "--time-extend":
                    options.PermanentStats.TimeExtendLevel = int.Parse(next!);
                    i++;
                    break;
                case "--gold-flat":
                    options.PermanentStats.GoldFlatPermLevel = int.Parse(next!);
                    i++;
                    break;
                case "--gold-multi":
                    options.PermanentStats.GoldMultiPermLevel = int.Parse(next!);
                    i++;
                    break;
                case "--start-level":
                    options.PermanentStats.StartLevelLevel = int.Parse(next!);
                    i++;
                    break;
                case "--start-gold":
                    options.PermanentStats.StartGoldLevel = int.Parse(next!);
                    i++;
                    break;
                case "--verbose":
                    options.Verbose = true;
                    break;
                case "--json":
                    options.OutputJson = true;
                    break;
                case "--strategy":
                    options.Strategy = ParseStrategy(next!);
                    i++;
                    break;
                case "--max-attempts":
                    options.MaxAttempts = int.Parse(next!);
                    i++;
                    break;
                case "--game-hours":
                    options.GameHours = double.Parse(next!);
                    i++;
                    break;
                case "--crystals":
                    options.CrystalBudget = int.Parse(next!);
                    i++;
                    break;
                case "--quick":
                    options.QuickAnalysis = true;
                    break;
                case "--output":
                    options.OutputPath = next!;
                    i++;
                    break;
                case "--raw-data":
                    options.IncludeRawData = true;
                    break;
            }
        }

        return options;
    }

    static UpgradeStrategy ParseStrategy(string value)
    {
        return value.ToLower() switch
        {
            "greedy" => UpgradeStrategy.Greedy,
            "damage" => UpgradeStrategy.DamageFirst,
            "survival" => UpgradeStrategy.SurvivalFirst,
            "crystal" => UpgradeStrategy.CrystalFarm,
            "balanced" => UpgradeStrategy.Balanced,
            "none" => UpgradeStrategy.None,
            _ => UpgradeStrategy.Greedy
        };
    }

    static ComboSkillLevel ParseComboSkill(string value)
    {
        return value.ToLower() switch
        {
            "none" => ComboSkillLevel.None,
            "beginner" => ComboSkillLevel.Beginner,
            "intermediate" => ComboSkillLevel.Intermediate,
            "expert" => ComboSkillLevel.Expert,
            "perfect" => ComboSkillLevel.Perfect,
            _ => ComboSkillLevel.None
        };
    }

    static void RunSimulation(SimulationOptions options)
    {
        // config 폴더 경로 찾기
        var configPath = FindConfigPath();
        Console.WriteLine($"Config path: {configPath}\n");

        // 시뮬레이터 생성
        var simulator = SimulatorFactory.Create(configPath);

        // 입력 프로파일 설정
        var profile = new InputProfile
        {
            AverageCps = options.Cps,
            CpsVariance = 0.2,
            ComboSkill = options.ComboSkill,
            AutoUpgrade = true
        };

        // 옵션 출력
        PrintOptions(options, profile);

        // 시뮬레이션 실행
        Console.WriteLine($"Running {options.NumRuns:N0} simulations...");
        var sw = Stopwatch.StartNew();

        var result = simulator.RunSimulations(
            options.PermanentStats,
            profile,
            options.NumRuns,
            options.TargetLevel,
            options.Parallelism,
            (current, total) =>
            {
                if (!options.OutputJson)
                {
                    Console.Write($"\rProgress: {current:N0}/{total:N0} ({100.0 * current / total:F1}%)");
                }
            }
        );

        sw.Stop();
        Console.WriteLine($"\rCompleted in {sw.Elapsed.TotalSeconds:F2}s                    \n");

        // 결과 출력
        if (options.OutputJson)
        {
            PrintJsonResult(result);
        }
        else
        {
            PrintResult(result, options);
        }
    }

    static string FindConfigPath()
    {
        // 현재 디렉토리에서 config 폴더 찾기
        var current = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(current, "config");

        if (Directory.Exists(configPath))
        {
            return configPath;
        }

        // 상위 디렉토리 탐색
        var parent = Directory.GetParent(current);
        while (parent != null)
        {
            configPath = Path.Combine(parent.FullName, "config");
            if (Directory.Exists(configPath))
            {
                return configPath;
            }
            parent = parent.Parent;
        }

        throw new DirectoryNotFoundException("Could not find 'config' folder");
    }

    static string FindBalanceDocPath(string configPath)
    {
        // config 폴더가 bin 안에 있으면 프로젝트 루트 찾기
        var current = Directory.GetParent(configPath);
        while (current != null)
        {
            // 이미 존재하는 balanceDoc 폴더 우선
            var balanceDoc = Path.Combine(current.FullName, "balanceDoc");
            if (Directory.Exists(balanceDoc))
            {
                return balanceDoc;
            }

            // .slnx 파일이 있으면 솔루션 루트로 간주 (프로젝트 전체 루트)
            if (Directory.GetFiles(current.FullName, "*.slnx").Length > 0 ||
                Directory.GetFiles(current.FullName, "*.sln").Length > 0)
            {
                // 여기에 balanceDoc 생성
                return Path.Combine(current.FullName, "balanceDoc");
            }

            current = current.Parent;
        }

        // 못 찾으면 config 폴더 옆에 생성
        return Path.Combine(Directory.GetParent(configPath)?.FullName ?? configPath, "balanceDoc");
    }

    static void PrintOptions(SimulationOptions options, InputProfile profile)
    {
        Console.WriteLine("=== Simulation Settings ===");
        Console.WriteLine($"  Target Level: {options.TargetLevel}");
        Console.WriteLine($"  CPS: {options.Cps:F1}");
        Console.WriteLine($"  Combo Skill: {profile.ComboSkill}");
        Console.WriteLine($"  Simulations: {options.NumRuns:N0}");
        Console.WriteLine();

        if (HasPermanentStats(options.PermanentStats))
        {
            Console.WriteLine("=== Permanent Stats ===");
            var stats = options.PermanentStats;
            if (stats.BaseAttackLevel > 0) Console.WriteLine($"  Base Attack: Lv.{stats.BaseAttackLevel}");
            if (stats.CritChanceLevel > 0) Console.WriteLine($"  Crit Chance: Lv.{stats.CritChanceLevel}");
            if (stats.CritDamageLevel > 0) Console.WriteLine($"  Crit Damage: Lv.{stats.CritDamageLevel}");
            if (stats.MultiHitLevel > 0) Console.WriteLine($"  Multi-Hit: Lv.{stats.MultiHitLevel}");
            if (stats.TimeExtendLevel > 0) Console.WriteLine($"  Time Extend: Lv.{stats.TimeExtendLevel}");
            if (stats.GoldFlatPermLevel > 0) Console.WriteLine($"  Gold Flat: Lv.{stats.GoldFlatPermLevel}");
            if (stats.GoldMultiPermLevel > 0) Console.WriteLine($"  Gold Multi: Lv.{stats.GoldMultiPermLevel}");
            if (stats.StartLevelLevel > 0) Console.WriteLine($"  Start Level: Lv.{stats.StartLevelLevel}");
            if (stats.StartGoldLevel > 0) Console.WriteLine($"  Start Gold: Lv.{stats.StartGoldLevel}");
            Console.WriteLine();
        }
    }

    static bool HasPermanentStats(SimPermanentStats stats)
    {
        return stats.BaseAttackLevel > 0 ||
               stats.CritChanceLevel > 0 ||
               stats.CritDamageLevel > 0 ||
               stats.MultiHitLevel > 0 ||
               stats.TimeExtendLevel > 0 ||
               stats.GoldFlatPermLevel > 0 ||
               stats.GoldMultiPermLevel > 0 ||
               stats.StartLevelLevel > 0 ||
               stats.StartGoldLevel > 0;
    }

    static void PrintResult(BatchResult result, SimulationOptions options)
    {
        Console.WriteLine("=== Results ===");
        Console.WriteLine($"  Average Level: {result.AverageLevel:F1}");
        Console.WriteLine($"  Median Level: {result.MedianLevel:F1}");
        Console.WriteLine($"  Min Level: {result.MinLevel:F0}");
        Console.WriteLine($"  Max Level: {result.MaxLevel:F0}");
        Console.WriteLine($"  Std Deviation: {result.StandardDeviation:F2}");
        Console.WriteLine();

        // 크리스털 통계
        Console.WriteLine("=== Crystal Statistics ===");
        Console.WriteLine($"  Avg Total Crystals: {result.AverageCrystals:F1}");
        Console.WriteLine($"    From Bosses: {result.AverageCrystalsFromBosses:F1}");
        Console.WriteLine($"    From Stages: {result.AverageCrystalsFromStages:F1}");
        Console.WriteLine($"    From Gold Convert: {result.AverageCrystalsFromGoldConvert:F1}");
        Console.WriteLine();

        if (result.TargetLevel > 0)
        {
            Console.WriteLine($"=== Target Level {result.TargetLevel} Analysis ===");
            Console.WriteLine($"  Success Rate: {result.SuccessRate:P1}");
            if (result.SuccessRate > 0 && result.SuccessRate < 1)
            {
                Console.WriteLine($"  Median Attempts: {result.MedianAttemptsToTarget:F1}");
                Console.WriteLine($"  Expected Attempts (90% confidence): {CalcAttemptsForConfidence(result.SuccessRate, 0.9):F1}");
            }
            else if (result.SuccessRate == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Target not reached in any simulation!");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        if (options.Verbose)
        {
            PrintLevelDistribution(result);
        }
    }

    static void RunProgressionSimulation(SimulationOptions options)
    {
        var configPath = FindConfigPath();
        Console.WriteLine($"Config path: {configPath}\n");

        var progressionSim = SimulatorFactory.CreateProgressionSimulator(configPath);

        var profile = new InputProfile
        {
            AverageCps = options.Cps,
            CpsVariance = 0.2,
            ComboSkill = options.ComboSkill,
            AutoUpgrade = true
        };

        // Game-time based simulation
        if (options.GameHours > 0)
        {
            Console.WriteLine("=== Game Time Based Progression ===");
            Console.WriteLine($"  Target Game Time: {options.GameHours} hours");
            Console.WriteLine($"  CPS: {options.Cps:F1}");
            Console.WriteLine($"  Strategy: {options.Strategy}");
            Console.WriteLine();

            Console.WriteLine("Running game-time simulation...");
            var sw = Stopwatch.StartNew();

            var result = progressionSim.SimulateByGameTime(
                options.PermanentStats,
                profile,
                options.GameHours,
                options.Strategy,
                (currentTime, targetTime) =>
                {
                    var percent = currentTime / targetTime * 100;
                    var hoursPlayed = currentTime / 3600;
                    Console.Write($"\rGame time: {hoursPlayed:F2}h / {options.GameHours}h ({percent:F1}%)");
                }
            );

            sw.Stop();
            Console.WriteLine($"\rCompleted in {sw.Elapsed.TotalSeconds:F2}s                              \n");

            PrintGameTimeResult(result, options);
        }
        else
        {
            // Target level based simulation (original behavior)
            Console.WriteLine("=== Progression Simulation Settings ===");
            Console.WriteLine($"  Target Level: {options.TargetLevel}");
            Console.WriteLine($"  CPS: {options.Cps:F1}");
            Console.WriteLine($"  Strategy: {options.Strategy}");
            Console.WriteLine($"  Max Attempts: {options.MaxAttempts}");
            Console.WriteLine();

            Console.WriteLine("Running progression simulation...");
            var sw = Stopwatch.StartNew();

            var result = progressionSim.SimulateProgression(
                options.PermanentStats,
                profile,
                options.TargetLevel,
                options.Strategy,
                options.MaxAttempts,
                (current, max) =>
                {
                    if (current % 10 == 0)
                        Console.Write($"\rSession {current}/{max}...");
                }
            );

            sw.Stop();
            Console.WriteLine($"\rCompleted in {sw.Elapsed.TotalSeconds:F2}s                    \n");

            PrintProgressionResult(result, options);
        }
    }

    static void PrintGameTimeResult(ProgressionResult result, SimulationOptions options)
    {
        Console.WriteLine("=== Game Time Simulation Result ===");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  Best Level Ever Reached: {result.BestLevelEver}");
        Console.ResetColor();
        Console.WriteLine($"  Final Session Level: {result.FinalMaxLevel}");
        Console.WriteLine($"  Total Sessions: {result.AttemptsNeeded}");
        Console.WriteLine($"  Total Game Time: {result.TotalGameTimeSeconds / 3600:F2} hours");
        Console.WriteLine();

        // Average session duration
        if (result.SessionHistory.Count > 0)
        {
            var avgDuration = result.SessionHistory.Average(s => s.SessionDurationSeconds);
            Console.WriteLine("=== Session Statistics ===");
            Console.WriteLine($"  Average Session Duration: {avgDuration:F1} seconds");
            Console.WriteLine($"  Total Sessions Played: {result.SessionHistory.Count}");

            // Level progression summary
            var levelGroups = result.SessionHistory
                .GroupBy(s => s.MaxLevel / 10 * 10)  // Group by 10 levels
                .OrderBy(g => g.Key)
                .ToList();

            if (levelGroups.Count > 0)
            {
                Console.WriteLine("\n=== Level Progression (sessions per level range) ===");
                foreach (var group in levelGroups.TakeLast(5))
                {
                    Console.WriteLine($"  Lv.{group.Key}-{group.Key + 9}: {group.Count()} sessions");
                }
            }
        }
        Console.WriteLine();

        Console.WriteLine("=== Crystal Economy ===");
        Console.WriteLine($"  Total Earned: {result.TotalCrystalsEarned:N0}");
        Console.WriteLine($"  Total Spent: {result.TotalCrystalsSpent:N0}");
        Console.WriteLine($"  Remaining: {result.TotalCrystalsEarned - result.TotalCrystalsSpent:N0}");
        Console.WriteLine();

        if (options.Verbose)
        {
            Console.WriteLine("=== Final Stats ===");
            PrintFinalStats(result.FinalStats);

            // Show upgrade history summary
            if (result.UpgradeHistory.Count > 0)
            {
                Console.WriteLine("=== Upgrade Summary ===");
                var upgradeGroups = result.UpgradeHistory
                    .GroupBy(u => u.StatId)
                    .OrderByDescending(g => g.Count())
                    .Take(5);

                foreach (var group in upgradeGroups)
                {
                    var maxLevel = group.Max(u => u.ToLevel);
                    Console.WriteLine($"  {group.Key}: {group.Count()} upgrades (max Lv.{maxLevel})");
                }
            }
        }
    }

    static void PrintProgressionResult(ProgressionResult result, SimulationOptions options)
    {
        Console.WriteLine("=== Progression Result ===");
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  SUCCESS! Target level {options.TargetLevel} reached!");
            Console.ResetColor();
            Console.WriteLine($"  Sessions needed: {result.AttemptsNeeded}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Target level {options.TargetLevel} NOT reached in {result.AttemptsNeeded} sessions");
            Console.ResetColor();
            Console.WriteLine($"  Best level achieved: {result.FinalMaxLevel}");
        }
        Console.WriteLine();

        Console.WriteLine("=== Crystal Economy ===");
        Console.WriteLine($"  Total Earned: {result.TotalCrystalsEarned:N0}");
        Console.WriteLine($"  Total Spent: {result.TotalCrystalsSpent:N0}");
        Console.WriteLine();

        if (options.Verbose && result.SessionHistory.Count > 0)
        {
            Console.WriteLine("=== Session History (Last 10) ===");
            foreach (var session in result.SessionHistory.TakeLast(10))
            {
                Console.WriteLine($"  #{session.SessionNumber}: Level {session.MaxLevel}, +{session.CrystalsEarned} crystals");
            }
            Console.WriteLine();

            Console.WriteLine("=== Final Stats ===");
            PrintFinalStats(result.FinalStats);
        }
    }

    static void PrintFinalStats(SimPermanentStats stats)
    {
        if (stats.BaseAttackLevel > 0) Console.WriteLine($"  Base Attack: Lv.{stats.BaseAttackLevel}");
        if (stats.AttackPercentLevel > 0) Console.WriteLine($"  Attack %: Lv.{stats.AttackPercentLevel}");
        if (stats.CritChanceLevel > 0) Console.WriteLine($"  Crit Chance: Lv.{stats.CritChanceLevel}");
        if (stats.CritDamageLevel > 0) Console.WriteLine($"  Crit Damage: Lv.{stats.CritDamageLevel}");
        if (stats.MultiHitLevel > 0) Console.WriteLine($"  Multi-Hit: Lv.{stats.MultiHitLevel}");
        if (stats.GoldFlatPermLevel > 0) Console.WriteLine($"  Gold Flat: Lv.{stats.GoldFlatPermLevel}");
        if (stats.GoldMultiPermLevel > 0) Console.WriteLine($"  Gold Multi: Lv.{stats.GoldMultiPermLevel}");
        if (stats.TimeExtendLevel > 0) Console.WriteLine($"  Time Extend: Lv.{stats.TimeExtendLevel}");
        Console.WriteLine();
    }

    static void RunBalanceAnalysis(SimulationOptions options)
    {
        var configPath = FindConfigPath();
        Console.WriteLine($"Config path: {configPath}\n");

        // balanceDoc 경로 결정 (프로젝트 루트 찾기)
        var balanceDocPath = FindBalanceDocPath(configPath);

        // 크리스탈 0 + game-hours 조합이면 전략 비교 모드로 전환
        if (options.CrystalBudget == 0 && options.GameHours > 0)
        {
            RunStrategyComparisonAnalysis(options, configPath, balanceDocPath);
            return;
        }

        Console.WriteLine("=== Balance Route Diversity Analysis ===\n");
        Console.WriteLine($"  Target Level: {options.TargetLevel}");
        Console.WriteLine($"  CPS: {options.Cps:F1}");
        Console.WriteLine($"  Crystal Budget: {options.CrystalBudget:N0}");
        Console.WriteLine($"  Mode: {(options.QuickAnalysis ? "Quick (single-stat only)" : "Full (Grid + GA)")}");
        Console.WriteLine();

        // 과거 분석 히스토리 로드
        var history = AnalysisHistory.Load(balanceDocPath);
        var latestRecord = history.GetLatest();
        if (latestRecord != null)
        {
            Console.WriteLine($"  Previous analysis: {latestRecord.AnalyzedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"    Top stats: {string.Join(", ", latestRecord.TopStats.Take(3))}");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("  No previous analysis history found.\n");
        }

        var profile = new InputProfile
        {
            AverageCps = options.Cps,
            CpsVariance = 0.2,
            ComboSkill = options.ComboSkill,
            AutoUpgrade = true
        };

        // Create components
        var batchSimulator = SimulatorFactory.Create(configPath);
        var costCalculator = SimulatorFactory.CreateCostCalculator(configPath);

        // Get all stat IDs
        var statIds = costCalculator.AllStatIds.ToArray();
        Console.WriteLine($"Analyzing {statIds.Length} permanent stats...\n");

        var explorer = new HybridPatternExplorer(batchSimulator, costCalculator, statIds);
        explorer.SimulationsPerPattern = Math.Max(20, options.NumRuns / 50); // Fewer sims per pattern for speed

        var sw = Stopwatch.StartNew();
        PatternRepository repository;

        if (options.QuickAnalysis)
        {
            Console.WriteLine("Running quick analysis...");
            repository = explorer.ExploreQuick(
                options.PermanentStats,
                profile,
                options.CrystalBudget,
                options.TargetLevel,
                (current, total, msg) =>
                {
                    Console.Write($"\r{msg}                    ");
                }
            );
        }
        else
        {
            Console.WriteLine("Running full analysis (Grid + GA)...");
            repository = explorer.ExploreAll(
                options.PermanentStats,
                profile,
                options.CrystalBudget,
                options.TargetLevel,
                history,  // 과거 히스토리 전달
                (phase, current, total, msg) =>
                {
                    Console.Write($"\rPhase {phase}: {msg}                    ");
                }
            );
        }

        sw.Stop();
        var durationSeconds = sw.Elapsed.TotalSeconds;
        Console.WriteLine($"\rCompleted in {durationSeconds:F1}s                              \n");

        // Analyze diversity
        var analyzer = new RouteDiversityAnalyzer();
        var analysisResult = analyzer.Analyze(repository);

        // Print to console
        PrintBalanceResult(analysisResult);

        // 분석 기록 저장
        SaveAnalysisRecord(history, analysisResult, repository, options, balanceDocPath);

        // 보고서 항상 저장 (날짜 폴더 + 회차 번호)
        ExportBalanceReport(
            repository,
            analysisResult,
            options,
            profile,
            costCalculator,
            explorer,
            durationSeconds,
            balanceDocPath
        );
    }

    static void SaveAnalysisRecord(
        AnalysisHistory history,
        BalanceQualityResult analysisResult,
        PatternRepository repository,
        SimulationOptions options,
        string balanceDocPath)
    {
        // 단일 스탯 패턴에서 순위 추출
        var singlePatterns = repository.All
            .Where(p => p.PatternId?.StartsWith("single_") == true && p.Result != null)
            .OrderByDescending(p => p.Result!.AverageMaxLevel)
            .ToList();

        var topStats = singlePatterns
            .Take(5)
            .Select(p => p.Allocation.Keys.First())
            .ToList();

        var bottomStats = singlePatterns
            .TakeLast(5)
            .Select(p => p.Allocation.Keys.First())
            .Reverse()
            .ToList();

        // 최고 패턴
        var bestPattern = repository.TopByLevel(1).FirstOrDefault();

        var record = new AnalysisRecord
        {
            RecordId = Guid.NewGuid().ToString()[..8],
            AnalyzedAt = DateTime.Now,
            BalanceGrade = analysisResult.BalanceGrade.ToString(),
            TargetLevel = options.TargetLevel,
            Cps = options.Cps,
            CrystalBudget = options.CrystalBudget,
            TopStats = topStats,
            BottomStats = bottomStats,
            BestPatternId = bestPattern?.PatternId ?? "",
            BestPatternLevel = bestPattern?.Result?.AverageMaxLevel ?? 0
        };

        // 히스토리에 추가 및 저장
        history.AddRecord(record);
        history.Save(balanceDocPath);

        Console.WriteLine($"Analysis record saved: {record.RecordId}");
        Console.WriteLine($"  History now contains {history.Records.Count} records");
        Console.WriteLine();
    }

    static void ExportBalanceReport(
        PatternRepository repository,
        BalanceQualityResult analysisResult,
        SimulationOptions options,
        InputProfile profile,
        StatCostCalculator costCalculator,
        HybridPatternExplorer explorer,
        double durationSeconds,
        string balanceDocPath)
    {
        Console.WriteLine("Generating report...");

        // Create analysis config
        var config = new AnalysisConfig
        {
            TargetLevel = options.TargetLevel,
            Cps = options.Cps,
            CrystalBudget = options.CrystalBudget,
            AnalysisMode = options.QuickAnalysis ? "Quick" : "Full",
            SimulationsPerPattern = explorer.SimulationsPerPattern,
            GaGenerations = options.QuickAnalysis ? 0 : 100,
            GaPopulationSize = options.QuickAnalysis ? 0 : 50
        };

        // Generate report
        var reportGenerator = new BalanceReportGenerator(costCalculator);
        var report = reportGenerator.Generate(
            repository,
            analysisResult,
            config,
            durationSeconds,
            options.IncludeRawData
        );

        // Export
        var exporter = new BalanceReportExporter();

        // 새 규칙: 날짜 폴더 + 회차 번호
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var todayFolder = Path.Combine(balanceDocPath, today);
        if (!Directory.Exists(todayFolder))
        {
            Directory.CreateDirectory(todayFolder);
        }

        // 회차 번호 결정
        var existingReports = Directory.GetFiles(todayFolder, "*.md")
            .Select(f => Path.GetFileName(f))
            .Where(f => f.Length >= 2 && char.IsDigit(f[0]) && char.IsDigit(f[1]))
            .OrderByDescending(f => f)
            .ToList();

        int nextNumber = 1;
        if (existingReports.Count > 0)
        {
            var lastReport = existingReports[0];
            if (int.TryParse(lastReport[..2], out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        var reportType = options.QuickAnalysis ? "quick_analysis" : "full_analysis";
        var basePath = Path.Combine(todayFolder, $"{nextNumber:D2}_{reportType}");

        var jsonPath = $"{basePath}.json";
        var mdPath = $"{basePath}.md";

        // Export both formats
        exporter.ExportJson(report, jsonPath);
        exporter.ExportMarkdown(report, mdPath);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nReport exported:");
        Console.WriteLine($"  JSON: {jsonPath}");
        Console.WriteLine($"  Markdown: {mdPath}");
        Console.ResetColor();
    }

    static void PrintBalanceResult(BalanceQualityResult result)
    {
        // Top patterns
        Console.WriteLine("=== Top 5 Patterns ===");
        foreach (var pattern in result.TopPatterns.Take(5))
        {
            var mainStats = string.Join(", ", pattern.MainStats
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => $"{kvp.Key}:{kvp.Value:P0}"));
            Console.WriteLine($"  {pattern.Rank}. {pattern.PatternId,-25} Avg.Lv: {pattern.AverageLevel:F1}  Rate: {pattern.SuccessRate:P0}");
            Console.WriteLine($"     [{mainStats}]");
        }
        Console.WriteLine();

        // Analysis metrics
        Console.WriteLine("=== Analysis ===");
        var dominanceStatus = result.HasDominantRoute ? "❌ Dominant route detected!" : "✅ No dominant route";
        Console.WriteLine($"  Dominance Ratio: {result.DominanceRatio:F2} (1st/2nd)  {dominanceStatus}");

        var diversityStatus = result.DiversityScore >= 0.5 ? "✅ High variety" : (result.DiversityScore >= 0.3 ? "⚠️ Moderate" : "❌ Low variety");
        Console.WriteLine($"  Diversity Score: {result.DiversityScore:F2}              {diversityStatus}");

        Console.WriteLine($"  Top Pattern Similarity: {result.TopPatternSimilarity:F2}");
        Console.WriteLine();

        // Category usage
        Console.WriteLine("=== Category Usage ===");
        foreach (var (category, usage) in result.CategoryUsage.OrderByDescending(kvp => kvp.Value))
        {
            var status = usage >= 0.5 ? "✅ Active" : (usage >= 0.3 ? "⚠️ Moderate" : "❌ Underused");
            Console.WriteLine($"  {category,-18} {usage:P0}  {status}");
        }
        Console.WriteLine();

        // Under/over used stats
        if (result.UnderusedStats.Count > 0)
        {
            Console.WriteLine($"  Underused stats: {string.Join(", ", result.UnderusedStats)}");
        }
        if (result.OverusedStats.Count > 0)
        {
            Console.WriteLine($"  Overused stats: {string.Join(", ", result.OverusedStats)}");
        }
        if (result.UnderusedStats.Count > 0 || result.OverusedStats.Count > 0)
        {
            Console.WriteLine();
        }

        // Balance grade
        Console.ForegroundColor = result.BalanceGrade switch
        {
            BalanceGrade.A => ConsoleColor.Green,
            BalanceGrade.B => ConsoleColor.Cyan,
            BalanceGrade.C => ConsoleColor.Yellow,
            BalanceGrade.D => ConsoleColor.DarkYellow,
            BalanceGrade.F => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        Console.WriteLine($"=== Balance Grade: {result.BalanceGrade} ===");
        Console.ResetColor();
        Console.WriteLine($"  {result.Summary}");
        Console.WriteLine();

        // Recommendations
        if (result.Recommendations.Count > 0)
        {
            Console.WriteLine("=== Recommendations ===");
            foreach (var rec in result.Recommendations)
            {
                Console.WriteLine($"  • {rec}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 전략 비교 모드 분석
    /// 크리스탈 0에서 시작하여 각 전략별로 게임 시간 동안 시뮬레이션
    /// 시간별 추이 기록 (1시간 단위)
    /// </summary>
    static void RunStrategyComparisonAnalysis(SimulationOptions options, string configPath, string balanceDocPath)
    {
        int runsPerStrategy = Math.Max(1, options.NumRuns / 100);  // 기본 10회, --runs로 조절
        if (runsPerStrategy > 20) runsPerStrategy = 20;  // 최대 20회

        int totalHours = (int)Math.Ceiling(options.GameHours);
        int checkpointInterval = 1;  // 1시간 단위 체크포인트

        Console.WriteLine("=== Strategy Comparison Analysis (Zero-Start) ===\n");
        Console.WriteLine($"  Mode: Zero-Crystal Start (Real Progression)");
        Console.WriteLine($"  Game Time: {options.GameHours} hours");
        Console.WriteLine($"  CPS: {options.Cps:F1}");
        Console.WriteLine($"  Runs per Strategy: {runsPerStrategy}");
        Console.WriteLine($"  Checkpoints: Every {checkpointInterval} hour(s)");
        Console.WriteLine();

        var profile = new InputProfile
        {
            AverageCps = options.Cps,
            CpsVariance = 0.2,
            ComboSkill = options.ComboSkill,
            AutoUpgrade = true
        };

        // 테스트할 전략들
        var strategies = new[]
        {
            UpgradeStrategy.Greedy,
            UpgradeStrategy.DamageFirst,
            UpgradeStrategy.SurvivalFirst,
            UpgradeStrategy.CrystalFarm,
            UpgradeStrategy.Balanced
        };

        var progressionSim = SimulatorFactory.CreateProgressionSimulator(configPath);
        var aggregatedResults = new List<StrategyAggregatedResult>();

        // 시간별 추이 데이터: [전략][시간] = 평균 레벨
        var hourlyData = new Dictionary<UpgradeStrategy, Dictionary<int, double>>();

        var sw = Stopwatch.StartNew();

        Console.WriteLine("Running strategy simulations...\n");

        foreach (var strategy in strategies)
        {
            hourlyData[strategy] = new Dictionary<int, double>();
            var runResults = new List<ProgressionResult>();

            // 시간별 레벨 수집: [시간][실행번호] = 레벨
            var hourlyLevels = new Dictionary<int, List<long>>();
            for (int h = 1; h <= totalHours; h++)
            {
                hourlyLevels[h] = new List<long>();
            }

            for (int run = 1; run <= runsPerStrategy; run++)
            {
                Console.Write($"\r  {strategy,-15}: Run {run}/{runsPerStrategy}");

                // 1시간씩 시뮬레이션하면서 체크포인트 기록
                var currentStats = new SimPermanentStats();
                long crystals = 0;
                long bestLevelEver = 0;
                double totalGameTime = 0;
                int totalSessions = 0;
                long totalCrystalsEarned = 0;

                for (int hour = 1; hour <= totalHours; hour++)
                {
                    // 1시간 시뮬레이션
                    var hourResult = progressionSim.SimulateByGameTime(
                        currentStats,
                        profile,
                        1.0,  // 1시간씩
                        strategy,
                        null
                    );

                    // 상태 업데이트
                    currentStats = hourResult.FinalStats;
                    bestLevelEver = Math.Max(bestLevelEver, hourResult.BestLevelEver);
                    totalGameTime += hourResult.TotalGameTimeSeconds;
                    totalSessions += hourResult.AttemptsNeeded;
                    totalCrystalsEarned += hourResult.TotalCrystalsEarned;

                    // 시간별 레벨 기록
                    hourlyLevels[hour].Add(bestLevelEver);
                }

                // 최종 결과 저장
                runResults.Add(new ProgressionResult
                {
                    BestLevelEver = bestLevelEver,
                    AttemptsNeeded = totalSessions,
                    TotalCrystalsEarned = totalCrystalsEarned,
                    FinalStats = currentStats
                });
            }

            // 시간별 평균 계산
            for (int h = 1; h <= totalHours; h++)
            {
                hourlyData[strategy][h] = hourlyLevels[h].Average();
            }

            // 최종 통계 계산
            var levels = runResults.Select(r => r.BestLevelEver).ToList();
            var sessions = runResults.Select(r => r.AttemptsNeeded).ToList();
            var crystalsEarned = runResults.Select(r => r.TotalCrystalsEarned).ToList();

            var aggregated = new StrategyAggregatedResult
            {
                Strategy = strategy,
                AvgLevel = levels.Average(),
                MinLevel = levels.Min(),
                MaxLevel = levels.Max(),
                StdDevLevel = CalculateStdDev(levels),
                AvgSessions = sessions.Average(),
                AvgCrystals = crystalsEarned.Average(),
                RepresentativeFinalStats = runResults.LastOrDefault()?.FinalStats
            };

            aggregatedResults.Add(aggregated);
            Console.WriteLine($"\r  {strategy,-15}: Avg Lv.{aggregated.AvgLevel,7:F0} (±{aggregated.StdDevLevel:F0})  Range: {aggregated.MinLevel}-{aggregated.MaxLevel}");
        }

        sw.Stop();
        Console.WriteLine($"\nCompleted in {sw.Elapsed.TotalSeconds:F1}s\n");

        // 시간별 추이 출력
        Console.WriteLine("=== Hourly Progression ===");
        Console.Write("  Hour |");
        foreach (var strategy in strategies)
        {
            Console.Write($" {strategy.ToString().Substring(0, Math.Min(8, strategy.ToString().Length)),8} |");
        }
        Console.WriteLine();
        Console.WriteLine("  " + new string('-', 7 + strategies.Length * 11));

        for (int h = 1; h <= totalHours; h++)
        {
            Console.Write($"  {h,4}h |");
            foreach (var strategy in strategies)
            {
                Console.Write($" {hourlyData[strategy][h],8:F0} |");
            }
            Console.WriteLine();
        }
        Console.WriteLine();

        // 결과 정렬 및 분석
        var sortedResults = aggregatedResults.OrderByDescending(r => r.AvgLevel).ToList();
        var best = sortedResults[0];
        var second = sortedResults.Count > 1 ? sortedResults[1] : sortedResults[0];

        // Dominance Ratio 계산 (평균 기준)
        double dominanceRatio = second.AvgLevel > 0
            ? best.AvgLevel / second.AvgLevel
            : 1.0;

        // 결과 출력
        Console.WriteLine("=== Final Results (Ranked by Average Level) ===");
        int rank = 1;
        foreach (var result in sortedResults)
        {
            var diffPercent = best.AvgLevel > 0
                ? (best.AvgLevel - result.AvgLevel) / best.AvgLevel * 100
                : 0;

            Console.WriteLine($"  {rank}. {result.Strategy,-15} Avg:{result.AvgLevel,6:F0} (±{result.StdDevLevel,4:F0})  " +
                $"Sessions:{result.AvgSessions,5:F0}  " +
                $"Crystals:{result.AvgCrystals,8:F0}  " +
                $"{(rank == 1 ? "" : $"(-{diffPercent:F1}%)")}");
            rank++;
        }
        Console.WriteLine();

        // 밸런스 지표
        Console.WriteLine("=== Balance Metrics ===");
        var dominanceStatus = dominanceRatio > 1.5 ? "❌ Dominant" : (dominanceRatio > 1.3 ? "⚠️ Moderate" : "✅ Balanced");
        Console.WriteLine($"  Dominance Ratio: {dominanceRatio:F2} (1st/2nd)  {dominanceStatus}");

        // 등급 판정
        var grade = dominanceRatio switch
        {
            <= 1.1 => "A",
            <= 1.3 => "B",
            <= 1.5 => "C",
            <= 2.0 => "D",
            _ => "F"
        };

        Console.ForegroundColor = grade switch
        {
            "A" => ConsoleColor.Green,
            "B" => ConsoleColor.Cyan,
            "C" => ConsoleColor.Yellow,
            "D" => ConsoleColor.DarkYellow,
            _ => ConsoleColor.Red
        };
        Console.WriteLine($"\n=== Balance Grade: {grade} ===");
        Console.ResetColor();
        Console.WriteLine();

        // 최종 스탯 출력
        Console.WriteLine("=== Final Permanent Stats (Representative Run) ===");
        foreach (var result in sortedResults)
        {
            var stats = result.RepresentativeFinalStats;
            if (stats != null)
            {
                Console.WriteLine($"\n{result.Strategy}:");
                Console.WriteLine($"  base_attack: {stats.BaseAttackLevel,3} → +{stats.BaseAttack:F0} dmg");
                Console.WriteLine($"  attack_percent: {stats.AttackPercentLevel,3} → +{stats.AttackPercentBonus * 100:F1}%");
                Console.WriteLine($"  crit_damage: {stats.CritDamageLevel,3} → x{stats.CriticalDamageBonus:F2}");
                Console.WriteLine($"  time_extend: {stats.TimeExtendLevel,3} → +{stats.TimeExtend:F1}s");
                Console.WriteLine($"  upgrade_discount: {stats.UpgradeDiscountLevel,3} → -{stats.UpgradeCostReduction * 100:F1}%");
            }
        }
        Console.WriteLine();

        // 보고서 저장 (시간별 추이 포함)
        SaveStrategyComparisonReportWithHourly(sortedResults, hourlyData, strategies, totalHours, options, runsPerStrategy, dominanceRatio, grade, balanceDocPath, configPath);
    }

    static double CalculateStdDev(List<long> values)
    {
        if (values.Count <= 1) return 0;
        double avg = values.Average();
        double sumSquares = values.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    class StrategyAggregatedResult
    {
        public UpgradeStrategy Strategy { get; set; }
        public double AvgLevel { get; set; }
        public long MinLevel { get; set; }
        public long MaxLevel { get; set; }
        public double StdDevLevel { get; set; }
        public double AvgSessions { get; set; }
        public double AvgCrystals { get; set; }
        public SimPermanentStats? RepresentativeFinalStats { get; set; }
    }

    static void SaveStrategyComparisonReport(
        List<(UpgradeStrategy Strategy, ProgressionResult Result)> results,
        SimulationOptions options,
        double dominanceRatio,
        string grade,
        string balanceDocPath)
    {
        // 오늘 날짜 폴더 생성
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var todayFolder = Path.Combine(balanceDocPath, today);
        if (!Directory.Exists(todayFolder))
        {
            Directory.CreateDirectory(todayFolder);
        }

        // 회차 번호 결정
        var existingReports = Directory.GetFiles(todayFolder, "*.md")
            .Select(f => Path.GetFileName(f))
            .Where(f => f.Length >= 2 && char.IsDigit(f[0]) && char.IsDigit(f[1]))
            .OrderByDescending(f => f)
            .ToList();

        int nextNumber = 1;
        if (existingReports.Count > 0)
        {
            var lastReport = existingReports[0];
            if (int.TryParse(lastReport[..2], out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        var reportFileName = $"{nextNumber:D2}_strategy_comparison.md";
        var reportPath = Path.Combine(todayFolder, reportFileName);

        // 보고서 내용 생성
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# 밸런스 테스트 보고서: 전략 비교 (#{nextNumber:D2})");
        sb.AppendLine();
        sb.AppendLine($"**테스트 일시:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**테스트 조건:**");
        sb.AppendLine($"- 시작 크리스탈: **0** (Zero-Start)");
        sb.AppendLine($"- 게임 시간: {options.GameHours}시간");
        sb.AppendLine($"- CPS: {options.Cps:F1}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 테스트 결과");
        sb.AppendLine();
        sb.AppendLine("### 핵심 지표");
        sb.AppendLine();
        sb.AppendLine("| 지표 | 결과 | 판정 |");
        sb.AppendLine("|------|------|------|");
        sb.AppendLine($"| Dominance Ratio | **{dominanceRatio:F2}** | {(dominanceRatio <= 1.3 ? "✅" : "❌")} |");
        sb.AppendLine($"| Balance Grade | **{grade}** | |");
        sb.AppendLine();
        sb.AppendLine("### 전략별 성과");
        sb.AppendLine();
        sb.AppendLine("| 순위 | 전략 | 최고 레벨 | 세션 수 | 총 크리스탈 |");
        sb.AppendLine("|------|------|-----------|---------|-------------|");

        int rank = 1;
        var best = results[0];
        foreach (var (strategy, result) in results)
        {
            var diffPercent = best.Result.BestLevelEver > 0
                ? (double)(best.Result.BestLevelEver - result.BestLevelEver) / best.Result.BestLevelEver * 100
                : 0;
            var diffStr = rank == 1 ? "" : $" (-{diffPercent:F1}%)";
            sb.AppendLine($"| {rank} | {strategy} | **{result.BestLevelEver}**{diffStr} | {result.AttemptsNeeded} | {result.TotalCrystalsEarned:N0} |");
            rank++;
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 분석");
        sb.AppendLine();

        if (dominanceRatio > 1.5)
        {
            sb.AppendLine($"**{best.Strategy}** 전략이 다른 전략을 압도하고 있습니다.");
            sb.AppendLine($"- 1위와 2위의 격차: {dominanceRatio:F2}배");
            sb.AppendLine("- 전략 다양성 부족");
        }
        else if (dominanceRatio > 1.3)
        {
            sb.AppendLine("전략 간 격차가 존재하지만 심각한 수준은 아닙니다.");
        }
        else
        {
            sb.AppendLine("전략 간 밸런스가 양호합니다.");
        }

        // 파일 저장
        File.WriteAllText(reportPath, sb.ToString());

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Report saved: {reportPath}");
        Console.ResetColor();
    }

    static void SaveStrategyComparisonReportAggregated(
        List<StrategyAggregatedResult> results,
        SimulationOptions options,
        int runsPerStrategy,
        double dominanceRatio,
        string grade,
        string balanceDocPath)
    {
        // 오늘 날짜 폴더 생성
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var todayFolder = Path.Combine(balanceDocPath, today);
        if (!Directory.Exists(todayFolder))
        {
            Directory.CreateDirectory(todayFolder);
        }

        // 회차 번호 결정
        var existingReports = Directory.GetFiles(todayFolder, "*.md")
            .Select(f => Path.GetFileName(f))
            .Where(f => f.Length >= 2 && char.IsDigit(f[0]) && char.IsDigit(f[1]))
            .OrderByDescending(f => f)
            .ToList();

        int nextNumber = 1;
        if (existingReports.Count > 0)
        {
            var lastReport = existingReports[0];
            if (int.TryParse(lastReport[..2], out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        var reportFileName = $"{nextNumber:D2}_strategy_comparison.md";
        var reportPath = Path.Combine(todayFolder, reportFileName);

        // 보고서 내용 생성
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# 밸런스 테스트 보고서: 전략 비교 (#{nextNumber:D2})");
        sb.AppendLine();
        sb.AppendLine($"**테스트 일시:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**테스트 조건:**");
        sb.AppendLine($"- 시작 크리스탈: **0** (Zero-Start)");
        sb.AppendLine($"- 게임 시간: {options.GameHours}시간");
        sb.AppendLine($"- CPS: {options.Cps:F1}");
        sb.AppendLine($"- **전략당 실행 횟수: {runsPerStrategy}회**");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 테스트 결과");
        sb.AppendLine();
        sb.AppendLine("### 핵심 지표");
        sb.AppendLine();
        sb.AppendLine("| 지표 | 결과 | 판정 |");
        sb.AppendLine("|------|------|------|");
        sb.AppendLine($"| Dominance Ratio | **{dominanceRatio:F2}** | {(dominanceRatio <= 1.3 ? "✅" : "❌")} |");
        sb.AppendLine($"| Balance Grade | **{grade}** | |");
        sb.AppendLine();
        sb.AppendLine("### 전략별 성과 (평균)");
        sb.AppendLine();
        sb.AppendLine("| 순위 | 전략 | 평균 레벨 | 편차 | 범위 | 평균 세션 |");
        sb.AppendLine("|------|------|-----------|------|------|-----------|");

        int rank = 1;
        var best = results[0];
        foreach (var result in results)
        {
            var diffPercent = best.AvgLevel > 0
                ? (best.AvgLevel - result.AvgLevel) / best.AvgLevel * 100
                : 0;
            var diffStr = rank == 1 ? "" : $" (-{diffPercent:F1}%)";
            sb.AppendLine($"| {rank} | {result.Strategy} | **{result.AvgLevel:F0}**{diffStr} | ±{result.StdDevLevel:F0} | {result.MinLevel}-{result.MaxLevel} | {result.AvgSessions:F0} |");
            rank++;
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 분석");
        sb.AppendLine();

        if (dominanceRatio > 1.5)
        {
            sb.AppendLine($"**{best.Strategy}** 전략이 다른 전략을 압도하고 있습니다.");
            sb.AppendLine($"- 1위와 2위의 격차: {dominanceRatio:F2}배");
            sb.AppendLine("- 전략 다양성 부족");
        }
        else if (dominanceRatio > 1.3)
        {
            sb.AppendLine("전략 간 격차가 존재하지만 심각한 수준은 아닙니다.");
        }
        else
        {
            sb.AppendLine("전략 간 밸런스가 양호합니다.");
        }

        // 파일 저장
        File.WriteAllText(reportPath, sb.ToString());

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Report saved: {reportPath}");
        Console.ResetColor();
    }

    static void SaveStrategyComparisonReportWithHourly(
        List<StrategyAggregatedResult> results,
        Dictionary<UpgradeStrategy, Dictionary<int, double>> hourlyData,
        UpgradeStrategy[] strategies,
        int totalHours,
        SimulationOptions options,
        int runsPerStrategy,
        double dominanceRatio,
        string grade,
        string balanceDocPath,
        string configPath)
    {
        // 오늘 날짜 폴더 생성
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var todayFolder = Path.Combine(balanceDocPath, today);
        if (!Directory.Exists(todayFolder))
        {
            Directory.CreateDirectory(todayFolder);
        }

        // 회차 번호 결정
        var existingReports = Directory.GetFiles(todayFolder, "*.md")
            .Select(f => Path.GetFileName(f))
            .Where(f => f.Length >= 2 && char.IsDigit(f[0]) && char.IsDigit(f[1]))
            .OrderByDescending(f => f)
            .ToList();

        int nextNumber = 1;
        if (existingReports.Count > 0)
        {
            var lastReport = existingReports[0];
            if (int.TryParse(lastReport[..2], out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        var reportFileName = $"{nextNumber:D2}_strategy_comparison.md";
        var reportPath = Path.Combine(todayFolder, reportFileName);

        // 보고서 내용 생성
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# 밸런스 테스트 보고서: 전략 비교 (#{nextNumber:D2})");
        sb.AppendLine();
        sb.AppendLine($"**테스트 일시:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**테스트 조건:**");
        sb.AppendLine($"- 시작 크리스탈: **0** (Zero-Start)");
        sb.AppendLine($"- 게임 시간: {options.GameHours}시간");
        sb.AppendLine($"- CPS: {options.Cps:F1}");
        sb.AppendLine($"- 전략당 실행 횟수: {runsPerStrategy}회");
        sb.AppendLine();

        // Config 파라미터 기록 (재현성 보장)
        sb.AppendLine("**핵심 파라미터:**");
        try
        {
            var permanentStatsPath = Path.Combine(configPath, "PermanentStats.json");
            if (File.Exists(permanentStatsPath))
            {
                var json = File.ReadAllText(permanentStatsPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var stats = doc.RootElement.GetProperty("stats");

                // 핵심 스탯의 effect_per_level 기록
                var keyStats = new[] { "base_attack", "attack_percent", "crit_damage", "time_extend", "crystal_bonus" };
                foreach (var statKey in keyStats)
                {
                    if (stats.TryGetProperty(statKey, out var stat) && stat.TryGetProperty("effect_per_level", out var effect))
                    {
                        sb.AppendLine($"- {statKey}: effect_per_level = {effect}");
                    }
                }
            }
        }
        catch
        {
            sb.AppendLine("- (config 로드 실패)");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // 핵심 지표
        sb.AppendLine("## 핵심 지표");
        sb.AppendLine();
        sb.AppendLine("| 지표 | 결과 | 판정 |");
        sb.AppendLine("|------|------|------|");
        sb.AppendLine($"| Dominance Ratio | **{dominanceRatio:F2}** | {(dominanceRatio <= 1.3 ? "✅" : "❌")} |");
        sb.AppendLine($"| Balance Grade | **{grade}** | |");
        sb.AppendLine();

        // 최종 결과
        sb.AppendLine("## 최종 결과 (평균)");
        sb.AppendLine();
        sb.AppendLine("| 순위 | 전략 | 평균 레벨 | 편차 | 범위 | 평균 세션 |");
        sb.AppendLine("|------|------|-----------|------|------|-----------|");

        int rank = 1;
        var best = results[0];
        foreach (var result in results)
        {
            var diffPercent = best.AvgLevel > 0
                ? (best.AvgLevel - result.AvgLevel) / best.AvgLevel * 100
                : 0;
            var diffStr = rank == 1 ? "" : $" (-{diffPercent:F1}%)";
            sb.AppendLine($"| {rank} | {result.Strategy} | **{result.AvgLevel:F0}**{diffStr} | ±{result.StdDevLevel:F0} | {result.MinLevel}-{result.MaxLevel} | {result.AvgSessions:F0} |");
            rank++;
        }
        sb.AppendLine();

        // 시간별 추이
        sb.AppendLine("## 시간별 추이");
        sb.AppendLine();
        sb.Append("| 시간 |");
        foreach (var strategy in strategies)
        {
            sb.Append($" {strategy} |");
        }
        sb.AppendLine();
        sb.Append("|------|");
        foreach (var _ in strategies)
        {
            sb.Append("------|");
        }
        sb.AppendLine();

        for (int h = 1; h <= totalHours; h++)
        {
            sb.Append($"| {h}h |");

            // 해당 시간의 순위 계산
            var hourRanking = strategies
                .Select(s => (Strategy: s, Level: hourlyData[s][h]))
                .OrderByDescending(x => x.Level)
                .ToList();

            foreach (var strategy in strategies)
            {
                var level = hourlyData[strategy][h];
                var hourRank = hourRanking.FindIndex(x => x.Strategy == strategy) + 1;
                var rankMark = hourRank == 1 ? " **1위**" : "";
                sb.Append($" {level:F0}{rankMark} |");
            }
            sb.AppendLine();
        }
        sb.AppendLine();

        // 시간별 1위 변화
        sb.AppendLine("### 시간별 1위 변화");
        sb.AppendLine();
        var leaderChanges = new List<string>();
        UpgradeStrategy? prevLeader = null;
        for (int h = 1; h <= totalHours; h++)
        {
            var leader = strategies.OrderByDescending(s => hourlyData[s][h]).First();
            if (leader != prevLeader)
            {
                leaderChanges.Add($"- **{h}시간**: {leader}");
                prevLeader = leader;
            }
        }
        foreach (var change in leaderChanges)
        {
            sb.AppendLine(change);
        }
        sb.AppendLine();

        // 분석
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 분석");
        sb.AppendLine();

        if (dominanceRatio > 1.5)
        {
            sb.AppendLine($"**{best.Strategy}** 전략이 다른 전략을 압도하고 있습니다.");
            sb.AppendLine($"- 1위와 2위의 격차: {dominanceRatio:F2}배");
            sb.AppendLine("- 전략 다양성 부족");
        }
        else if (dominanceRatio > 1.3)
        {
            sb.AppendLine("전략 간 격차가 존재하지만 심각한 수준은 아닙니다.");
        }
        else
        {
            sb.AppendLine("전략 간 밸런스가 양호합니다.");
        }

        // 파일 저장
        File.WriteAllText(reportPath, sb.ToString());

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Report saved: {reportPath}");
        Console.ResetColor();
    }

    static void PrintLevelDistribution(BatchResult result)
    {
        Console.WriteLine("=== Level Distribution (Top 10) ===");

        var distribution = result.LevelDistribution
            .Select((percent, index) => (Level: index + 1, Percent: percent))
            .Where(x => x.Percent > 0)
            .OrderByDescending(x => x.Percent)
            .Take(10);

        foreach (var (level, percent) in distribution)
        {
            var bar = new string('#', (int)(percent * 50));
            Console.WriteLine($"  Lv.{level,3}: {percent:P1} {bar}");
        }
        Console.WriteLine();
    }

    static double CalcAttemptsForConfidence(double successRate, double confidence)
    {
        // P(success in n attempts) = 1 - (1-p)^n >= confidence
        // n >= log(1-confidence) / log(1-p)
        return Math.Ceiling(Math.Log(1 - confidence) / Math.Log(1 - successRate));
    }

    static void PrintJsonResult(BatchResult result)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            result.NumSimulations,
            result.AverageLevel,
            result.MedianLevel,
            result.MinLevel,
            result.MaxLevel,
            result.StandardDeviation,
            result.TargetLevel,
            result.SuccessRate,
            result.MedianAttemptsToTarget,
            result.AverageDuration,
            result.AverageCrystals,
            result.AverageCrystalsFromBosses,
            result.AverageCrystalsFromStages,
            result.AverageCrystalsFromGoldConvert
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine(json);
    }
}

class SimulationOptions
{
    public int TargetLevel { get; set; } = 50;
    public double Cps { get; set; } = 5.0;
    public int NumRuns { get; set; } = 1000;
    public ComboSkillLevel ComboSkill { get; set; } = ComboSkillLevel.None;
    public int Parallelism { get; set; } = -1;
    public bool Verbose { get; set; } = false;
    public bool OutputJson { get; set; } = false;
    public SimPermanentStats PermanentStats { get; set; } = new();

    // Progression options
    public UpgradeStrategy Strategy { get; set; } = UpgradeStrategy.Greedy;
    public int MaxAttempts { get; set; } = 1000;
    public double GameHours { get; set; } = 0;  // 0 = use target level mode

    // Analysis options
    public int CrystalBudget { get; set; } = 1000;
    public bool QuickAnalysis { get; set; } = false;
    public string? OutputPath { get; set; }
    public bool IncludeRawData { get; set; } = false;
}
