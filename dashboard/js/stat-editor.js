/**
 * Stat Editor - Modal-based stat editing functionality
 */

const StatEditor = {
    currentStat: null,
    currentType: null,

    /**
     * Open edit modal for a stat
     */
    openModal(statId, statType) {
        if (!ConfigLoader.canSave()) {
            Dashboard.showNotification('Editing is only available in desktop app mode', 'error');
            return;
        }

        const config = statType === 'permanent'
            ? Dashboard.config.permanentStats
            : Dashboard.config.inGameStats;

        const stat = config?.stats?.[statId];
        if (!stat) {
            Dashboard.showNotification('Stat not found', 'error');
            return;
        }

        this.currentStat = stat;
        this.currentType = statType;

        // Populate form
        document.getElementById('edit-stat-id').value = statId;
        document.getElementById('edit-stat-type').value = statType;
        document.getElementById('modal-stat-name').textContent = `Edit: ${stat.name}`;
        document.getElementById('edit-name').value = stat.name || '';
        document.getElementById('edit-base-cost').value = stat.base_cost || 100;
        document.getElementById('edit-growth-rate').value = stat.growth_rate || 0.5;
        document.getElementById('edit-multiplier').value = stat.multiplier || 1.5;
        document.getElementById('edit-softcap').value = stat.softcap_interval || 10;
        document.getElementById('edit-effect').value = stat.effect_per_level || 1;
        document.getElementById('edit-max-level').value = stat.max_level || 0;

        // Show preview
        this.updatePreview();

        // Show modal
        document.getElementById('stat-edit-modal').classList.add('show');
    },

    /**
     * Close edit modal
     */
    closeModal() {
        document.getElementById('stat-edit-modal').classList.remove('show');
        this.currentStat = null;
        this.currentType = null;
    },

    /**
     * Update cost preview table
     */
    updatePreview() {
        const baseCost = parseFloat(document.getElementById('edit-base-cost').value) || 100;
        const growthRate = parseFloat(document.getElementById('edit-growth-rate').value) || 0.5;
        const multiplier = parseFloat(document.getElementById('edit-multiplier').value) || 1.5;
        const softcap = parseInt(document.getElementById('edit-softcap').value) || 10;
        const effect = parseFloat(document.getElementById('edit-effect').value) || 1;

        const levels = [1, 5, 10, 20, 30, 50];
        let html = '<table class="preview-table"><thead><tr><th>Lv</th><th>Cost</th><th>Total</th><th>Effect</th></tr></thead><tbody>';

        for (const lv of levels) {
            const cost = FormulaEngine.calcUpgradeCost(baseCost, growthRate, multiplier, softcap, lv);
            const total = FormulaEngine.calcTotalCost(baseCost, growthRate, multiplier, softcap, 1, lv + 1);
            const eff = effect * lv;

            html += `<tr>
                <td>${lv}</td>
                <td>${ChartUtils.formatNumber(cost)}</td>
                <td>${ChartUtils.formatNumber(total)}</td>
                <td>${eff.toFixed(effect < 1 ? 2 : 0)}</td>
            </tr>`;
        }

        html += '</tbody></table>';
        document.getElementById('cost-preview-table').innerHTML = html;
    },

    /**
     * Save stat changes
     */
    async save() {
        const statId = document.getElementById('edit-stat-id').value;
        const statType = document.getElementById('edit-stat-type').value;

        const config = statType === 'permanent'
            ? Dashboard.config.permanentStats
            : Dashboard.config.inGameStats;

        if (!config?.stats?.[statId]) {
            Dashboard.showNotification('Stat not found', 'error');
            return;
        }

        // Update stat data
        const updatedStat = {
            ...config.stats[statId],
            name: document.getElementById('edit-name').value,
            base_cost: parseFloat(document.getElementById('edit-base-cost').value),
            growth_rate: parseFloat(document.getElementById('edit-growth-rate').value),
            multiplier: parseFloat(document.getElementById('edit-multiplier').value),
            softcap_interval: parseInt(document.getElementById('edit-softcap').value),
            effect_per_level: parseFloat(document.getElementById('edit-effect').value),
            max_level: parseInt(document.getElementById('edit-max-level').value) || 0
        };

        config.stats[statId] = updatedStat;

        // Save to file
        let result;
        if (statType === 'permanent') {
            result = await ConfigLoader.savePermanentStats(config);
        } else {
            result = await ConfigLoader.saveInGameStats(config);
        }

        if (result.success) {
            Dashboard.showNotification(`${updatedStat.name} saved successfully!`, 'success');
            this.closeModal();

            // Refresh the UI
            await Dashboard.reloadConfig();
        } else {
            Dashboard.showNotification(`Save failed: ${result.error}`, 'error');
        }
    },

    /**
     * Initialize event listeners
     */
    init() {
        // Form submit
        const form = document.getElementById('stat-edit-form');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.save();
            });
        }

        // Preview update on input change
        const inputs = ['edit-base-cost', 'edit-growth-rate', 'edit-multiplier', 'edit-softcap', 'edit-effect'];
        inputs.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.addEventListener('input', () => this.updatePreview());
            }
        });

        // Close modal on outside click
        const modal = document.getElementById('stat-edit-modal');
        if (modal) {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    this.closeModal();
                }
            });
        }

        // Close on Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeModal();
            }
        });
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    StatEditor.init();
});

// Export
window.StatEditor = StatEditor;
