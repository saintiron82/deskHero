/**
 * Tab 4: Balance Analyzer
 * Compares stat efficiency and provides balance insights
 */

const BalanceAnalyzerTab = {
    config: null,
    efficiencyChart: null,
    scatterChart: null,
    radarChart: null,

    /**
     * Initialize the tab
     */
    async init(config) {
        this.config = config;
        this.bindEvents();
    },

    /**
     * Bind event handlers
     */
    bindEvents() {
        const analyzeBtn = document.getElementById('run-analysis');
        if (analyzeBtn) {
            analyzeBtn.addEventListener('click', () => this.analyze());
        }

        // Category filter checkboxes
        document.querySelectorAll('.category-filter').forEach(cb => {
            cb.addEventListener('change', () => this.analyze());
        });
    },

    /**
     * Get selected categories
     */
    getSelectedCategories() {
        const categories = [];
        document.querySelectorAll('.category-filter:checked').forEach(cb => {
            categories.push(cb.value);
        });
        return categories;
    },

    /**
     * Run analysis
     */
    analyze() {
        const targetLevel = parseInt(document.getElementById('analysis-level')?.value) || 10;
        const selectedCategories = this.getSelectedCategories();

        const analysisData = this.calculateEfficiency(targetLevel, selectedCategories);

        this.renderEfficiencyChart(analysisData);
        this.renderScatterChart(analysisData, targetLevel);
        this.renderRadarChart(analysisData);
    },

    /**
     * Calculate efficiency for all stats
     */
    calculateEfficiency(targetLevel, selectedCategories) {
        const permanentStats = this.config.permanentStats?.stats || {};
        const results = [];

        for (const [statId, stat] of Object.entries(permanentStats)) {
            // Filter by category
            if (!selectedCategories.includes(stat.category)) continue;

            const totalCost = FormulaEngine.calcTotalCost(
                stat.base_cost,
                stat.growth_rate,
                stat.multiplier,
                stat.softcap_interval,
                1,
                targetLevel + 1
            );

            const effect = FormulaEngine.calcStatEffect(stat.effect_per_level, targetLevel);

            // Normalize efficiency (higher is better)
            const efficiency = totalCost > 0 ? (effect / totalCost) * 100 : 0;

            results.push({
                id: statId,
                name: stat.name,
                category: stat.category,
                totalCost,
                effect,
                efficiency,
                effectPerLevel: stat.effect_per_level,
                description: stat.description
            });
        }

        // Sort by efficiency
        results.sort((a, b) => b.efficiency - a.efficiency);

        return results;
    },

    /**
     * Render efficiency bar chart
     */
    renderEfficiencyChart(data) {
        const ctx = document.getElementById('efficiency-chart');
        if (!ctx) return;

        if (this.efficiencyChart) {
            this.efficiencyChart.destroy();
        }

        // Take top 10
        const topData = data.slice(0, 10);
        const labels = topData.map(d => d.name);
        const values = topData.map(d => d.efficiency);
        const colors = topData.map(d => ChartUtils.categoryColors[d.category] || '#4a90d9');

        this.efficiencyChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Efficiency (Effect / Cost × 100)',
                    data: values,
                    backgroundColor: colors.map(c => c + 'cc'),
                    borderColor: colors,
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (context) => {
                                const item = topData[context.dataIndex];
                                return [
                                    `Efficiency: ${item.efficiency.toFixed(2)}`,
                                    `Total Cost: ${ChartUtils.formatNumber(item.totalCost)}`,
                                    `Effect at Lv: ${item.effect.toFixed(1)}`
                                ];
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: { color: '#a0a0a0' },
                        grid: { color: '#2d4a7c33' }
                    },
                    y: {
                        ticks: { color: '#e8e8e8' },
                        grid: { display: false }
                    }
                }
            }
        });
    },

    /**
     * Render scatter chart (cost vs effect)
     */
    renderScatterChart(data, targetLevel) {
        const ctx = document.getElementById('scatter-chart');
        if (!ctx) return;

        if (this.scatterChart) {
            this.scatterChart.destroy();
        }

        // Group by category
        const categories = {};
        for (const item of data) {
            if (!categories[item.category]) {
                categories[item.category] = [];
            }
            categories[item.category].push({
                x: item.totalCost,
                y: item.effect,
                label: item.name
            });
        }

        const datasets = Object.entries(categories).map(([cat, points]) => ({
            label: this.getCategoryName(cat),
            data: points,
            backgroundColor: ChartUtils.categoryColors[cat] || '#4a90d9',
            borderColor: ChartUtils.categoryColors[cat] || '#4a90d9',
            pointRadius: 8,
            pointHoverRadius: 12
        }));

        this.scatterChart = new Chart(ctx, {
            type: 'scatter',
            data: { datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: { color: '#e8e8e8' }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => {
                                const point = context.raw;
                                return [
                                    point.label,
                                    `Cost: ${ChartUtils.formatNumber(point.x)}`,
                                    `Effect: ${point.y.toFixed(1)}`
                                ];
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        title: {
                            display: true,
                            text: `Total Cost (Lv ${targetLevel})`,
                            color: '#a0a0a0'
                        },
                        ticks: {
                            color: '#a0a0a0',
                            callback: (value) => ChartUtils.formatNumber(value)
                        },
                        grid: { color: '#2d4a7c33' }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Effect',
                            color: '#a0a0a0'
                        },
                        ticks: { color: '#a0a0a0' },
                        grid: { color: '#2d4a7c33' }
                    }
                }
            }
        });
    },

    /**
     * Render radar chart (build comparison)
     */
    renderRadarChart(data) {
        const ctx = document.getElementById('radar-chart');
        if (!ctx) return;

        if (this.radarChart) {
            this.radarChart.destroy();
        }

        // Create sample builds for comparison
        const builds = this.createSampleBuilds(data);

        const labels = ['Damage', 'Crit', 'Economy', 'Start Bonus', 'Utility'];

        const datasets = builds.map((build, i) => ({
            label: build.name,
            data: build.scores,
            borderColor: ChartUtils.colors[i],
            backgroundColor: ChartUtils.colors[i] + '40',
            pointBackgroundColor: ChartUtils.colors[i]
        }));

        this.radarChart = new Chart(ctx, {
            type: 'radar',
            data: { labels, datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: { color: '#e8e8e8' }
                    }
                },
                scales: {
                    r: {
                        angleLines: { color: '#2d4a7c' },
                        grid: { color: '#2d4a7c' },
                        pointLabels: { color: '#e8e8e8' },
                        ticks: {
                            color: '#a0a0a0',
                            backdropColor: 'transparent'
                        },
                        suggestedMin: 0,
                        suggestedMax: 100
                    }
                }
            }
        });
    },

    /**
     * Create sample builds for radar comparison
     */
    createSampleBuilds(data) {
        // Calculate category scores
        const categoryScores = {};
        for (const item of data) {
            if (!categoryScores[item.category]) {
                categoryScores[item.category] = 0;
            }
            categoryScores[item.category] += item.efficiency;
        }

        // Normalize scores
        const maxScore = Math.max(...Object.values(categoryScores), 1);
        for (const cat of Object.keys(categoryScores)) {
            categoryScores[cat] = (categoryScores[cat] / maxScore) * 100;
        }

        return [
            {
                name: 'Damage Build',
                scores: [
                    100,  // Damage
                    80,   // Crit
                    20,   // Economy
                    10,   // Start Bonus
                    30    // Utility
                ]
            },
            {
                name: 'Economy Build',
                scores: [
                    30,   // Damage
                    20,   // Crit
                    100,  // Economy
                    50,   // Start Bonus
                    40    // Utility
                ]
            },
            {
                name: 'Balanced Build',
                scores: [
                    60,   // Damage
                    50,   // Crit
                    60,   // Economy
                    50,   // Start Bonus
                    60    // Utility
                ]
            }
        ];
    },

    /**
     * Get category display name
     */
    getCategoryName(category) {
        const names = {
            base: 'Base Stats',
            currency: 'Currency',
            utility: 'Utility',
            starting: 'Starting Bonus'
        };
        return names[category] || category;
    }
};

// Export
window.BalanceAnalyzerTab = BalanceAnalyzerTab;
