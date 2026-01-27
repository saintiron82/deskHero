/**
 * DeskWarrior Chart Utilities
 * Chart.js wrapper functions for consistent styling
 */

const ChartUtils = {
    // Default color palette
    colors: [
        '#4a90d9',  // Primary blue
        '#ff6b6b',  // Red
        '#6bcb77',  // Green
        '#ffd93d',  // Yellow
        '#4d96ff',  // Light blue
        '#9b59b6',  // Purple
        '#e67e22',  // Orange
        '#1abc9c',  // Teal
        '#f39c12',  // Gold
        '#e74c3c',  // Dark red
    ],

    // Category colors
    categoryColors: {
        base: '#ff6b6b',
        currency: '#ffd93d',
        utility: '#6bcb77',
        starting: '#4d96ff'
    },

    // Default chart options
    defaultOptions: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                labels: {
                    color: '#e8e8e8',
                    font: { size: 12 }
                }
            },
            tooltip: {
                backgroundColor: '#1a1a2e',
                titleColor: '#e8e8e8',
                bodyColor: '#a0a0a0',
                borderColor: '#2d4a7c',
                borderWidth: 1
            }
        },
        scales: {
            x: {
                ticks: { color: '#a0a0a0' },
                grid: { color: '#2d4a7c33' }
            },
            y: {
                ticks: { color: '#a0a0a0' },
                grid: { color: '#2d4a7c33' }
            }
        }
    },

    /**
     * Create a line chart
     */
    createLineChart(ctx, labels, datasets, options = {}) {
        const chartOptions = this.mergeOptions(options);

        return new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: datasets.map((ds, i) => ({
                    borderColor: ds.color || this.colors[i % this.colors.length],
                    backgroundColor: (ds.color || this.colors[i % this.colors.length]) + '20',
                    fill: ds.fill !== false,
                    tension: 0.3,
                    pointRadius: 2,
                    pointHoverRadius: 5,
                    ...ds
                }))
            },
            options: chartOptions
        });
    },

    /**
     * Create a bar chart
     */
    createBarChart(ctx, labels, datasets, options = {}) {
        const chartOptions = this.mergeOptions(options);

        return new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: datasets.map((ds, i) => ({
                    backgroundColor: ds.color || this.colors[i % this.colors.length],
                    borderColor: ds.borderColor || (ds.color || this.colors[i % this.colors.length]),
                    borderWidth: 1,
                    ...ds
                }))
            },
            options: chartOptions
        });
    },

    /**
     * Create a horizontal bar chart
     */
    createHorizontalBarChart(ctx, labels, data, options = {}) {
        const chartOptions = this.mergeOptions({
            indexAxis: 'y',
            ...options
        });

        return new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: data.map((_, i) => this.colors[i % this.colors.length] + 'cc'),
                    borderColor: data.map((_, i) => this.colors[i % this.colors.length]),
                    borderWidth: 1
                }]
            },
            options: chartOptions
        });
    },

    /**
     * Create a scatter chart
     */
    createScatterChart(ctx, datasets, options = {}) {
        const chartOptions = this.mergeOptions(options);

        return new Chart(ctx, {
            type: 'scatter',
            data: {
                datasets: datasets.map((ds, i) => ({
                    backgroundColor: ds.color || this.colors[i % this.colors.length],
                    borderColor: ds.color || this.colors[i % this.colors.length],
                    pointRadius: 8,
                    pointHoverRadius: 12,
                    ...ds
                }))
            },
            options: chartOptions
        });
    },

    /**
     * Create a radar chart
     */
    createRadarChart(ctx, labels, datasets, options = {}) {
        const chartOptions = {
            ...this.defaultOptions,
            scales: {
                r: {
                    angleLines: { color: '#2d4a7c' },
                    grid: { color: '#2d4a7c' },
                    pointLabels: { color: '#e8e8e8' },
                    ticks: {
                        color: '#a0a0a0',
                        backdropColor: 'transparent'
                    }
                }
            },
            ...options
        };

        return new Chart(ctx, {
            type: 'radar',
            data: {
                labels,
                datasets: datasets.map((ds, i) => ({
                    borderColor: ds.color || this.colors[i % this.colors.length],
                    backgroundColor: (ds.color || this.colors[i % this.colors.length]) + '40',
                    pointBackgroundColor: ds.color || this.colors[i % this.colors.length],
                    ...ds
                }))
            },
            options: chartOptions
        });
    },

    /**
     * Merge options with defaults
     */
    mergeOptions(options) {
        return {
            ...this.defaultOptions,
            ...options,
            plugins: {
                ...this.defaultOptions.plugins,
                ...options.plugins
            },
            scales: {
                ...this.defaultOptions.scales,
                ...options.scales
            }
        };
    },

    /**
     * Format number for display
     */
    formatNumber(num) {
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        } else if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toLocaleString();
    },

    /**
     * Generate level labels
     */
    generateLevelLabels(minLevel, maxLevel) {
        const labels = [];
        for (let i = minLevel; i <= maxLevel; i++) {
            labels.push(i);
        }
        return labels;
    },

    /**
     * Destroy chart if exists
     */
    destroyChart(chart) {
        if (chart) {
            chart.destroy();
        }
    }
};

// Export for use in other modules
window.ChartUtils = ChartUtils;
