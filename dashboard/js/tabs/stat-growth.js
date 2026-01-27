/**
 * Tab 1: Stat Growth Curves
 * Visualizes stat upgrade costs and effects over levels
 */

const StatGrowthTab = {
    config: null,
    chart: null,
    selectedStats: new Set(),

    /**
     * Initialize the tab
     */
    async init(config) {
        this.config = config;
        this.renderStatCheckboxes();
        this.bindEvents();
    },

    /**
     * Render stat selection checkboxes
     */
    renderStatCheckboxes() {
        // Permanent stats
        const permanentContainer = document.getElementById('permanent-stat-checkboxes');
        if (permanentContainer && this.config.permanentStats?.stats) {
            const categories = this.config.permanentStats.categories || {};
            const stats = this.config.permanentStats.stats;

            // Group by category
            const grouped = {};
            for (const [statId, statData] of Object.entries(stats)) {
                const cat = statData.category || 'other';
                if (!grouped[cat]) grouped[cat] = [];
                grouped[cat].push({ id: statId, ...statData });
            }

            let html = '';
            const categoryOrder = ['base', 'currency', 'utility', 'starting'];
            for (const cat of categoryOrder) {
                if (!grouped[cat]) continue;
                const catInfo = categories[cat] || { name: cat };
                html += `<div class="stat-subcategory category-${cat}">`;
                html += `<strong>${catInfo.name}</strong>`;
                for (const stat of grouped[cat]) {
                    html += `
                        <div class="stat-item">
                            <label>
                                <input type="checkbox" data-stat="${stat.id}" data-type="permanent">
                                ${stat.name}
                            </label>
                            <button class="edit-btn" onclick="StatEditor.openModal('${stat.id}', 'permanent')" title="Edit">&#9998;</button>
                        </div>
                    `;
                }
                html += `</div>`;
            }
            permanentContainer.innerHTML = html;
        }

        // In-game stats
        const inGameContainer = document.getElementById('ingame-stat-checkboxes');
        if (inGameContainer && this.config.inGameStats?.stats) {
            let html = '';
            for (const [statId, statData] of Object.entries(this.config.inGameStats.stats)) {
                html += `
                    <div class="stat-item">
                        <label>
                            <input type="checkbox" data-stat="${statId}" data-type="ingame">
                            ${statData.name}
                        </label>
                        <button class="edit-btn" onclick="StatEditor.openModal('${statId}', 'ingame')" title="Edit">&#9998;</button>
                    </div>
                `;
            }
            inGameContainer.innerHTML = html;
        }
    },

    /**
     * Bind event handlers
     */
    bindEvents() {
        // Update button
        const updateBtn = document.getElementById('update-growth-chart');
        if (updateBtn) {
            updateBtn.addEventListener('click', () => this.updateChart());
        }

        // Stat checkboxes
        document.querySelectorAll('#permanent-stat-checkboxes input, #ingame-stat-checkboxes input').forEach(cb => {
            cb.addEventListener('change', (e) => {
                const statId = e.target.dataset.stat;
                const statType = e.target.dataset.type;
                const key = `${statType}:${statId}`;

                if (e.target.checked) {
                    this.selectedStats.add(key);
                } else {
                    this.selectedStats.delete(key);
                }
            });
        });

        // Pre-select first stat
        const firstCheckbox = document.querySelector('#permanent-stat-checkboxes input');
        if (firstCheckbox) {
            firstCheckbox.checked = true;
            this.selectedStats.add(`permanent:${firstCheckbox.dataset.stat}`);
        }
    },

    /**
     * Update the chart
     */
    updateChart() {
        const minLevel = parseInt(document.getElementById('level-min')?.value) || 1;
        const maxLevel = parseInt(document.getElementById('level-max')?.value) || 50;
        const showCost = document.getElementById('show-cost')?.checked;
        const showCumulative = document.getElementById('show-cumulative')?.checked;
        const showEffect = document.getElementById('show-effect')?.checked;

        if (this.selectedStats.size === 0) {
            alert('Please select at least one stat');
            return;
        }

        const labels = ChartUtils.generateLevelLabels(minLevel, maxLevel);
        const datasets = [];
        const milestoneData = [];

        let colorIndex = 0;
        for (const key of this.selectedStats) {
            const [type, statId] = key.split(':');
            const stats = type === 'permanent'
                ? this.config.permanentStats.stats
                : this.config.inGameStats.stats;
            const stat = stats?.[statId];

            if (!stat) continue;

            const color = ChartUtils.colors[colorIndex % ChartUtils.colors.length];
            const { costs, cumulativeCosts, effects } = this.calculateGrowthData(stat, minLevel, maxLevel);

            if (showCost) {
                datasets.push({
                    label: `${stat.name} - Cost`,
                    data: costs,
                    color: color,
                    fill: false
                });
            }

            if (showCumulative) {
                datasets.push({
                    label: `${stat.name} - Cumulative`,
                    data: cumulativeCosts,
                    color: color,
                    borderDash: [5, 5],
                    fill: false
                });
            }

            if (showEffect) {
                datasets.push({
                    label: `${stat.name} - Effect`,
                    data: effects,
                    color: color,
                    borderDash: [2, 2],
                    fill: false,
                    yAxisID: 'y1'
                });
            }

            // Milestone data
            milestoneData.push({
                name: stat.name,
                lv10: this.getMilestoneData(stat, 10),
                lv25: this.getMilestoneData(stat, 25),
                lv50: this.getMilestoneData(stat, 50),
                lv100: this.getMilestoneData(stat, 100)
            });

            colorIndex++;
        }

        this.renderChart(labels, datasets, showEffect);
        this.renderMilestoneTable(milestoneData);
    },

    /**
     * Calculate growth data for a stat
     */
    calculateGrowthData(stat, minLevel, maxLevel) {
        const costs = [];
        const cumulativeCosts = [];
        const effects = [];
        let cumulative = 0;

        for (let lv = minLevel; lv <= maxLevel; lv++) {
            const cost = FormulaEngine.calcUpgradeCost(
                stat.base_cost,
                stat.growth_rate,
                stat.multiplier,
                stat.softcap_interval,
                lv
            );
            const effect = FormulaEngine.calcStatEffect(stat.effect_per_level, lv);

            cumulative += cost;
            costs.push(cost);
            cumulativeCosts.push(cumulative);
            effects.push(effect);
        }

        return { costs, cumulativeCosts, effects };
    },

    /**
     * Get milestone data for a specific level
     */
    getMilestoneData(stat, level) {
        const cost = FormulaEngine.calcUpgradeCost(
            stat.base_cost,
            stat.growth_rate,
            stat.multiplier,
            stat.softcap_interval,
            level
        );
        const totalCost = FormulaEngine.calcTotalCost(
            stat.base_cost,
            stat.growth_rate,
            stat.multiplier,
            stat.softcap_interval,
            1,
            level + 1
        );
        const effect = FormulaEngine.calcStatEffect(stat.effect_per_level, level);

        return {
            cost: ChartUtils.formatNumber(cost),
            totalCost: ChartUtils.formatNumber(totalCost),
            effect: effect.toFixed(stat.effect_per_level < 1 ? 1 : 0)
        };
    },

    /**
     * Render the growth chart
     */
    renderChart(labels, datasets, hasEffect) {
        const ctx = document.getElementById('growth-chart');
        if (!ctx) return;

        // Destroy existing chart
        if (this.chart) {
            this.chart.destroy();
        }

        const scales = {
            x: {
                title: {
                    display: true,
                    text: 'Level',
                    color: '#a0a0a0'
                },
                ticks: { color: '#a0a0a0' },
                grid: { color: '#2d4a7c33' }
            },
            y: {
                title: {
                    display: true,
                    text: 'Cost',
                    color: '#a0a0a0'
                },
                ticks: {
                    color: '#a0a0a0',
                    callback: (value) => ChartUtils.formatNumber(value)
                },
                grid: { color: '#2d4a7c33' }
            }
        };

        if (hasEffect) {
            scales.y1 = {
                position: 'right',
                title: {
                    display: true,
                    text: 'Effect',
                    color: '#a0a0a0'
                },
                ticks: { color: '#a0a0a0' },
                grid: { display: false }
            };
        }

        this.chart = ChartUtils.createLineChart(ctx, labels, datasets, {
            scales,
            plugins: {
                legend: {
                    labels: {
                        color: '#e8e8e8',
                        font: { size: 11 }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: (context) => {
                            const value = context.raw;
                            return `${context.dataset.label}: ${ChartUtils.formatNumber(value)}`;
                        }
                    }
                }
            }
        });
    },

    /**
     * Render milestone table
     */
    renderMilestoneTable(data) {
        const tbody = document.querySelector('#milestone-table tbody');
        if (!tbody) return;

        let html = '';
        for (const item of data) {
            html += `
                <tr>
                    <td><strong>${item.name}</strong></td>
                    <td>
                        <div>${item.lv10.cost} / ${item.lv10.totalCost}</div>
                        <small>Effect: ${item.lv10.effect}</small>
                    </td>
                    <td>
                        <div>${item.lv25.cost} / ${item.lv25.totalCost}</div>
                        <small>Effect: ${item.lv25.effect}</small>
                    </td>
                    <td>
                        <div>${item.lv50.cost} / ${item.lv50.totalCost}</div>
                        <small>Effect: ${item.lv50.effect}</small>
                    </td>
                    <td>
                        <div>${item.lv100.cost} / ${item.lv100.totalCost}</div>
                        <small>Effect: ${item.lv100.effect}</small>
                    </td>
                </tr>
            `;
        }
        tbody.innerHTML = html;
    }
};

// Export
window.StatGrowthTab = StatGrowthTab;
