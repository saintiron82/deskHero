/**
 * DeskWarrior Config Loader
 * Loads and saves JSON configuration files for the balance dashboard
 * Supports both browser (fetch) and PyWebView (native API) environments
 */

const ConfigLoader = {
    // Cached configs
    _cache: {
        statFormulas: null,
        permanentStats: null,
        inGameStats: null
    },

    // Base path to config files (for fetch fallback)
    basePath: '../config/',

    // Check if running in PyWebView
    _isPyWebView: false,

    /**
     * Initialize - detect environment
     */
    async init() {
        // Wait for pywebview API if available
        if (window.pywebview) {
            this._isPyWebView = true;
            console.log('Running in PyWebView mode');
        } else {
            // Wait a bit for pywebview to initialize
            await new Promise(resolve => setTimeout(resolve, 500));
            if (window.pywebview) {
                this._isPyWebView = true;
                console.log('Running in PyWebView mode (delayed)');
            } else {
                console.log('Running in browser mode');
            }
        }
        return this._isPyWebView;
    },

    /**
     * Load all configuration files
     * @returns {Promise<Object>} All configs combined
     */
    async loadAll() {
        try {
            const [statFormulas, permanentStats, inGameStats] = await Promise.all([
                this.loadStatFormulas(),
                this.loadPermanentStats(),
                this.loadInGameStats()
            ]);

            return {
                statFormulas,
                permanentStats,
                inGameStats,
                constants: statFormulas.constants || {}
            };
        } catch (error) {
            console.error('Failed to load configs:', error);
            throw error;
        }
    },

    /**
     * Load a JSON file (auto-detect method)
     */
    async _loadFile(filename) {
        if (this._isPyWebView && window.pywebview?.api) {
            const result = await window.pywebview.api.load_config(filename);
            if (result.success) {
                return result.data;
            } else {
                throw new Error(result.error);
            }
        } else {
            const response = await fetch(this.basePath + filename);
            if (!response.ok) {
                throw new Error(`Failed to load ${filename}: ${response.status}`);
            }
            return response.json();
        }
    },

    /**
     * Save a JSON file (PyWebView only)
     */
    async saveFile(filename, data) {
        if (this._isPyWebView && window.pywebview?.api) {
            const result = await window.pywebview.api.save_config(filename, data);
            if (result.success) {
                // Clear cache for this file
                this.clearCache();
                return { success: true, message: result.message };
            } else {
                return { success: false, error: result.error };
            }
        } else {
            return { success: false, error: 'Save is only available in desktop app mode' };
        }
    },

    /**
     * Check if save is available
     */
    canSave() {
        return this._isPyWebView && window.pywebview?.api;
    },

    /**
     * Load StatFormulas.json
     */
    async loadStatFormulas() {
        if (this._cache.statFormulas) {
            return this._cache.statFormulas;
        }
        const data = await this._loadFile('StatFormulas.json');
        this._cache.statFormulas = data;
        return data;
    },

    /**
     * Save StatFormulas.json
     */
    async saveStatFormulas(data) {
        const result = await this.saveFile('StatFormulas.json', data);
        if (result.success) {
            this._cache.statFormulas = data;
        }
        return result;
    },

    /**
     * Load PermanentStatGrowth.json
     */
    async loadPermanentStats() {
        if (this._cache.permanentStats) {
            return this._cache.permanentStats;
        }
        const data = await this._loadFile('PermanentStatGrowth.json');
        this._cache.permanentStats = data;
        return data;
    },

    /**
     * Save PermanentStatGrowth.json
     */
    async savePermanentStats(data) {
        const result = await this.saveFile('PermanentStatGrowth.json', data);
        if (result.success) {
            this._cache.permanentStats = data;
        }
        return result;
    },

    /**
     * Load InGameStatGrowth.json
     */
    async loadInGameStats() {
        if (this._cache.inGameStats) {
            return this._cache.inGameStats;
        }
        const data = await this._loadFile('InGameStatGrowth.json');
        this._cache.inGameStats = data;
        return data;
    },

    /**
     * Save InGameStatGrowth.json
     */
    async saveInGameStats(data) {
        const result = await this.saveFile('InGameStatGrowth.json', data);
        if (result.success) {
            this._cache.inGameStats = data;
        }
        return result;
    },

    /**
     * Get permanent stats grouped by category
     */
    async getStatsByCategory() {
        const permanentStats = await this.loadPermanentStats();
        const categories = permanentStats.categories || {};
        const stats = permanentStats.stats || {};

        const grouped = {};

        for (const [catId, catInfo] of Object.entries(categories)) {
            grouped[catId] = {
                name: catInfo.name,
                order: catInfo.order,
                stats: []
            };
        }

        for (const [statId, statData] of Object.entries(stats)) {
            const category = statData.category || 'other';
            if (!grouped[category]) {
                grouped[category] = { name: category, order: 99, stats: [] };
            }
            grouped[category].stats.push({
                id: statId,
                ...statData
            });
        }

        // Sort by category order
        return Object.entries(grouped)
            .sort((a, b) => a[1].order - b[1].order)
            .reduce((obj, [key, val]) => {
                obj[key] = val;
                return obj;
            }, {});
    },

    /**
     * Get a specific stat's config
     */
    async getStat(statId, type = 'permanent') {
        const config = type === 'permanent'
            ? await this.loadPermanentStats()
            : await this.loadInGameStats();

        return config.stats?.[statId] || null;
    },

    /**
     * Update a specific stat and save
     */
    async updateStat(statId, statData, type = 'permanent') {
        const config = type === 'permanent'
            ? await this.loadPermanentStats()
            : await this.loadInGameStats();

        if (!config.stats) {
            config.stats = {};
        }
        config.stats[statId] = statData;

        if (type === 'permanent') {
            return this.savePermanentStats(config);
        } else {
            return this.saveInGameStats(config);
        }
    },

    /**
     * Clear cache (useful for refreshing data)
     */
    clearCache() {
        this._cache.statFormulas = null;
        this._cache.permanentStats = null;
        this._cache.inGameStats = null;
    }
};

// Export for use in other modules
window.ConfigLoader = ConfigLoader;
