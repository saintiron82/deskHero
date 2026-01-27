/**
 * Tab 2: DPS Simulator
 * Calculates and visualizes damage with different stat combinations
 */

const DPSSimulatorTab = {
    config: null,
    chart: null,

    /**
     * Initialize the tab
     */
    async init(config) {
        this.config = config;
        this.bindEvents();
        this.calculate();  // Initial calculation
    },

    /**
     * Bind event handlers
     */
    bindEvents() {
        const calculateBtn = document.getElementById('calculate-damage');
        if (calculateBtn) {
            calculateBtn.addEventListener('click', () => this.calculate());
        }

        // Auto-calculate on input change
        const inputs = document.querySelectorAll('#dps-simulator input, #dps-simulator select');
        inputs.forEach(input => {
            input.addEventListener('change', () => this.calculate());
        });
    },

    /**
     * Get input values
     */
    getInputValues() {
        return {
            basePower: parseFloat(document.getElementById('base-power')?.value) || 10,
            baseAttack: parseFloat(document.getElementById('base-attack')?.value) || 0,
            attackPercent: parseFloat(document.getElementById('attack-percent')?.value) / 100 || 0,
            critChance: parseFloat(document.getElementById('crit-chance')?.value) / 100 || 0.1,
            critDamage: parseFloat(document.getElementById('crit-damage')?.value) || 2.0,
            multiHitChance: parseFloat(document.getElementById('multi-hit-chance')?.value) / 100 || 0,
            comboStack: parseInt(document.getElementById('combo-stack')?.value) || 0,
            comboDamage: parseFloat(document.getElementById('combo-damage')?.value) || 0
        };
    },

    /**
     * Calculate damage
     */
    calculate() {
        const values = this.getInputValues();

        // Calculate combo multiplier
        const comboMult = FormulaEngine.calcComboMultiplier(values.comboDamage, values.comboStack);

        // Calculate with crit (for max damage display)
        const critMult = values.critDamage;
        const multiMult = 2.0;  // Multi-hit doubles damage

        // Get damage steps
        const steps = FormulaEngine.calcDamageSteps(
            values.basePower,
            values.baseAttack,
            values.attackPercent,
            critMult,
            multiMult,
            comboMult
        );

        // Final damage (with crit and multi-hit)
        const finalDamage = steps[steps.length - 1].value;

        // Expected DPS (probability weighted)
        const expectedDPS = FormulaEngine.calcExpectedDamage(
            values.basePower,
            values.baseAttack,
            values.attackPercent,
            values.critChance,
            values.critDamage,
            values.multiHitChance
        ) * (1 + values.comboDamage / 100) * Math.pow(2, values.comboStack);

        // Update display
        this.updateDisplay(finalDamage, expectedDPS, steps);
        this.renderChart(steps);
        this.renderSteps(steps, values);
    },

    /**
     * Update result display
     */
    updateDisplay(finalDamage, expectedDPS, steps) {
        const finalDamageEl = document.getElementById('final-damage');
        const expectedDPSEl = document.getElementById('expected-dps');

        if (finalDamageEl) {
            finalDamageEl.textContent = ChartUtils.formatNumber(finalDamage);
        }
        if (expectedDPSEl) {
            expectedDPSEl.textContent = ChartUtils.formatNumber(Math.floor(expectedDPS));
        }
    },

    /**
     * Render damage breakdown chart
     */
    renderChart(steps) {
        const ctx = document.getElementById('damage-breakdown-chart');
        if (!ctx) return;

        if (this.chart) {
            this.chart.destroy();
        }

        const labels = steps.map(s => s.name);
        const data = steps.map(s => s.value);

        this.chart = ChartUtils.createHorizontalBarChart(ctx, labels, data, {
            indexAxis: 'y',
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (context) => `Damage: ${ChartUtils.formatNumber(context.raw)}`
                    }
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Damage',
                        color: '#a0a0a0'
                    },
                    ticks: {
                        color: '#a0a0a0',
                        callback: (value) => ChartUtils.formatNumber(value)
                    },
                    grid: { color: '#2d4a7c33' }
                },
                y: {
                    ticks: { color: '#e8e8e8' },
                    grid: { display: false }
                }
            }
        });
    },

    /**
     * Render damage calculation steps
     */
    renderSteps(steps, values) {
        const container = document.getElementById('damage-steps-list');
        if (!container) return;

        const stepDescriptions = [
            { name: 'Base', formula: `base_power = ${values.basePower}` },
            { name: 'Add', formula: `${values.basePower} + base_attack(${values.baseAttack})` },
            { name: 'Multiply', formula: `step2 × (1 + ${(values.attackPercent * 100).toFixed(0)}%)` },
            { name: 'Critical', formula: `step3 × ${values.critDamage}x` },
            { name: 'Multi-Hit', formula: `step4 × 2x` },
            { name: 'Combo', formula: `step5 × combo(stack ${values.comboStack})` }
        ];

        let html = '';
        steps.forEach((step, i) => {
            const desc = stepDescriptions[i] || {};
            html += `
                <div class="damage-step">
                    <span class="step-num">${i + 1}</span>
                    <span class="step-name">${step.name}</span>
                    <span class="step-formula">${step.formula}</span>
                    <span class="step-value">${ChartUtils.formatNumber(step.value)}</span>
                </div>
            `;
        });

        container.innerHTML = html;
    }
};

// Export
window.DPSSimulatorTab = DPSSimulatorTab;
