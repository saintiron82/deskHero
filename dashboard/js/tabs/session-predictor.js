/**
 * Tab 3: Session Predictor
 * Simulates multiple play sessions and predicts progression
 */

const SessionPredictorTab = {
    config: null,
    progressChart: null,
    statLevelChart: null,

    // Investment strategies
    strategies: {
        damage: {
            name: 'Damage Focus',
            priority: ['base_attack', 'attack_percent', 'crit_chance', 'crit_damage', 'multi_hit']
        },
        economy: {
            name: 'Economy Focus',
            priority: ['gold_flat_perm', 'gold_multi_perm', 'crystal_flat', 'crystal_multi', 'start_gold']
        },
        hybrid: {
            name: 'Hybrid',
            priority: ['base_attack', 'gold_multi_perm', 'attack_percent', 'crystal_flat', 'start_gold']
        }
    },

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
        const runBtn = document.getElementById('run-simulation');
        if (runBtn) {
            runBtn.addEventListener('click', () => this.runSimulation());
        }
    },

    /**
     * Run simulation
     */
    runSimulation() {
        const numSessions = parseInt(document.getElementById('num-sessions')?.value) || 10;
        const avgLevel = parseInt(document.getElementById('avg-level')?.value) || 20;
        const strategyName = document.querySelector('input[name="strategy"]:checked')?.value || 'damage';

        const strategy = this.strategies[strategyName];
        const results = this.simulate(numSessions, avgLevel, strategy);

        this.renderProgressChart(results);
        this.renderStatLevelChart(results);
        this.renderTable(results);
    },

    /**
     * Simulate multiple sessions
     */
    simulate(numSessions, avgLevel, strategy) {
        const permanentStats = this.config.permanentStats?.stats || {};
        const results = [];
        let totalCrystals = 0;
        let permLevels = {};  // Track permanent stat levels

        // Initialize all stat levels to 0
        for (const statId of Object.keys(permanentStats)) {
            permLevels[statId] = 0;
        }

        for (let session = 1; session <= numSessions; session++) {
            // Simulate session
            const levelVariance = Math.floor(Math.random() * 5) - 2;  // -2 to +2
            const actualLevel = Math.max(1, avgLevel + levelVariance);

            // Calculate crystals earned
            const bossesKilled = Math.floor(actualLevel / 10);
            const crystalFlat = permLevels['crystal_flat'] || 0;
            const crystalsEarned = bossesKilled * (10 + crystalFlat);

            totalCrystals += crystalsEarned;

            // Try to upgrade based on strategy
            const upgrades = [];
            let remainingCrystals = totalCrystals;

            for (const statId of strategy.priority) {
                const stat = permanentStats[statId];
                if (!stat) continue;

                while (remainingCrystals > 0) {
                    const currentLevel = permLevels[statId];
                    const cost = FormulaEngine.calcUpgradeCost(
                        stat.base_cost,
                        stat.growth_rate,
                        stat.multiplier,
                        stat.softcap_interval,
                        currentLevel + 1
                    );

                    if (cost <= remainingCrystals) {
                        remainingCrystals -= cost;
                        permLevels[statId]++;
                        upgrades.push(`${stat.name} Lv${permLevels[statId]}`);
                    } else {
                        break;
                    }
                }
            }

            totalCrystals = remainingCrystals;

            results.push({
                session,
                level: actualLevel,
                crystalsEarned,
                totalCrystals,
                upgrades: upgrades.length > 0 ? upgrades.join(', ') : '-',
                permLevels: { ...permLevels }
            });
        }

        return results;
    },

    /**
     * Render progress chart (crystals over time)
     */
    renderProgressChart(results) {
        const ctx = document.getElementById('session-progress-chart');
        if (!ctx) return;

        if (this.progressChart) {
            this.progressChart.destroy();
        }

        const labels = results.map(r => `Session ${r.session}`);
        const datasets = [
            {
                label: 'Crystals Earned',
                data: results.map(r => r.crystalsEarned),
                color: '#4a90d9'
            },
            {
                label: 'Total Crystals',
                data: results.map(r => r.totalCrystals),
                color: '#6bcb77',
                fill: false
            }
        ];

        this.progressChart = ChartUtils.createLineChart(ctx, labels, datasets, {
            plugins: {
                title: {
                    display: true,
                    text: 'Crystal Progression',
                    color: '#e8e8e8'
                }
            },
            scales: {
                y: {
                    title: {
                        display: true,
                        text: 'Crystals',
                        color: '#a0a0a0'
                    }
                }
            }
        });
    },

    /**
     * Render stat level chart (stat levels over time)
     */
    renderStatLevelChart(results) {
        const ctx = document.getElementById('stat-level-chart');
        if (!ctx) return;

        if (this.statLevelChart) {
            this.statLevelChart.destroy();
        }

        const labels = results.map(r => `S${r.session}`);

        // Get stats that were upgraded
        const lastResult = results[results.length - 1];
        const upgradedStats = Object.entries(lastResult.permLevels)
            .filter(([_, level]) => level > 0)
            .slice(0, 6);  // Show max 6 stats

        const permanentStats = this.config.permanentStats?.stats || {};
        const datasets = upgradedStats.map(([statId, _], index) => {
            const stat = permanentStats[statId];
            return {
                label: stat?.name || statId,
                data: results.map(r => r.permLevels[statId] || 0),
                color: ChartUtils.colors[index % ChartUtils.colors.length],
                fill: false
            };
        });

        if (datasets.length === 0) {
            ctx.parentElement.innerHTML = '<p style="color: #a0a0a0; text-align: center; padding: 50px;">No upgrades made</p>';
            return;
        }

        this.statLevelChart = ChartUtils.createLineChart(ctx, labels, datasets, {
            plugins: {
                title: {
                    display: true,
                    text: 'Stat Level Progression',
                    color: '#e8e8e8'
                }
            },
            scales: {
                y: {
                    title: {
                        display: true,
                        text: 'Level',
                        color: '#a0a0a0'
                    },
                    beginAtZero: true
                }
            }
        });
    },

    /**
     * Render session details table
     */
    renderTable(results) {
        const tbody = document.querySelector('#session-table tbody');
        if (!tbody) return;

        let html = '';
        for (const r of results) {
            html += `
                <tr>
                    <td>${r.session}</td>
                    <td>${r.level}</td>
                    <td>${ChartUtils.formatNumber(r.crystalsEarned)}</td>
                    <td>${ChartUtils.formatNumber(r.totalCrystals)}</td>
                    <td>${r.upgrades}</td>
                </tr>
            `;
        }
        tbody.innerHTML = html;
    }
};

// Export
window.SessionPredictorTab = SessionPredictorTab;
