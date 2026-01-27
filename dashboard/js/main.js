/**
 * DeskWarrior Balance Dashboard - Main Entry
 * Initializes the dashboard and handles tab navigation
 */

const Dashboard = {
    // Global state
    config: null,
    activeTab: 'stat-growth',
    initialized: false,

    /**
     * Initialize the dashboard
     */
    async init() {
        console.log('Initializing Dashboard...');

        try {
            // Initialize config loader (detect PyWebView)
            await ConfigLoader.init();

            // Load configurations
            this.config = await ConfigLoader.loadAll();
            console.log('Config loaded:', this.config);

            // Update UI based on save capability
            this.updateSaveUI();

            // Initialize tab navigation
            this.initTabNavigation();

            // Initialize all tab modules
            await this.initTabs();

            this.initialized = true;
            console.log('Dashboard initialized successfully!');
        } catch (error) {
            console.error('Failed to initialize dashboard:', error);
            this.showError('Failed to load configuration. Make sure you\'re running from a web server.');
        }
    },

    /**
     * Initialize tab navigation
     */
    initTabNavigation() {
        const tabBtns = document.querySelectorAll('.tab-btn');
        const tabPanels = document.querySelectorAll('.tab-panel');

        tabBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                const tabId = btn.dataset.tab;

                // Update button states
                tabBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');

                // Update panel visibility
                tabPanels.forEach(panel => {
                    panel.classList.remove('active');
                    if (panel.id === tabId) {
                        panel.classList.add('active');
                    }
                });

                this.activeTab = tabId;
                this.onTabChange(tabId);
            });
        });
    },

    /**
     * Handle tab change
     */
    onTabChange(tabId) {
        console.log('Tab changed to:', tabId);

        // Trigger resize event for charts to redraw properly
        window.dispatchEvent(new Event('resize'));
    },

    /**
     * Initialize all tab modules
     */
    async initTabs() {
        // Initialize stat growth tab
        if (window.StatGrowthTab) {
            await StatGrowthTab.init(this.config);
        }

        // Initialize DPS simulator tab
        if (window.DPSSimulatorTab) {
            await DPSSimulatorTab.init(this.config);
        }

        // Initialize session predictor tab
        if (window.SessionPredictorTab) {
            await SessionPredictorTab.init(this.config);
        }

        // Initialize balance analyzer tab
        if (window.BalanceAnalyzerTab) {
            await BalanceAnalyzerTab.init(this.config);
        }
    },

    /**
     * Show error message
     */
    showError(message) {
        const container = document.querySelector('.dashboard-container');
        if (container) {
            container.innerHTML = `
                <div style="text-align: center; padding: 50px; color: #dc3545;">
                    <h2>Error</h2>
                    <p>${message}</p>
                    <p style="color: #a0a0a0; margin-top: 20px;">
                        Run the dashboard with: <code>python -m http.server 8080</code><br>
                        Then open: <code>http://localhost:8080/dashboard/</code>
                    </p>
                </div>
            `;
        }
    },

    /**
     * Get config
     */
    getConfig() {
        return this.config;
    },

    /**
     * Update UI based on save capability
     */
    updateSaveUI() {
        const canSave = ConfigLoader.canSave();
        const saveIndicator = document.getElementById('save-indicator');

        if (saveIndicator) {
            if (canSave) {
                saveIndicator.innerHTML = '<span class="save-enabled">Desktop Mode - Editing Enabled</span>';
            } else {
                saveIndicator.innerHTML = '<span class="save-disabled">Browser Mode - View Only</span>';
            }
        }

        // Show/hide edit buttons based on capability
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.style.display = canSave ? 'inline-block' : 'none';
        });
    },

    /**
     * Show notification
     */
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        document.body.appendChild(notification);

        setTimeout(() => {
            notification.classList.add('show');
        }, 10);

        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    },

    /**
     * Reload config and refresh UI
     */
    async reloadConfig() {
        ConfigLoader.clearCache();
        this.config = await ConfigLoader.loadAll();
        await this.initTabs();
        this.showNotification('Configuration reloaded', 'success');
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    Dashboard.init();
});

// Export for use in other modules
window.Dashboard = Dashboard;
