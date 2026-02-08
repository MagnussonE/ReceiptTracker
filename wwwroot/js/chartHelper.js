window.chartHelper = {
    chart: null,

    createPieChart: function (canvasId, labels, data, backgroundColors) {
        // Destroy existing chart if it exists
        if (this.chart) {
            this.chart.destroy();
        }

        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        this.chart = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: backgroundColors,
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            padding: 15,
                            font: {
                                size: 14
                            },
                            generateLabels: function(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const dataset = data.datasets[0];
                                    const total = dataset.data.reduce((a, b) => a + b, 0);
                                    
                                    return data.labels.map((label, i) => {
                                        const value = dataset.data[i];
                                        const percentage = ((value / total) * 100).toFixed(1);
                                        
                                        return {
                                            text: `${label}: ${percentage}%`,
                                            fillStyle: dataset.backgroundColor[i],
                                            hidden: false,
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((value / total) * 100).toFixed(1);
                                return `${label}: ${value.toFixed(2)} kr (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    },

    destroyChart: function() {
        if (this.chart) {
            this.chart.destroy();
            this.chart = null;
        }
    }
};
