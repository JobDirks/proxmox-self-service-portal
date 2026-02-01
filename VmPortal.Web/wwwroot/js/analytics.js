window.vmPortalAnalytics = (function () {
    let userActivityChart = null;
    let vmStatusChart = null;

    function createOrUpdateChart(canvasId, configFactory) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const context = canvas.getContext('2d');

        if (canvasId === 'userActivityChart' && userActivityChart) {
            userActivityChart.destroy();
        }

        if (canvasId === 'vmStatusChart' && vmStatusChart) {
            vmStatusChart.destroy();
        }

        const config = configFactory();
        const chart = new Chart(context, config);

        if (canvasId === 'userActivityChart') {
            userActivityChart = chart;
        } else if (canvasId === 'vmStatusChart') {
            vmStatusChart = chart;
        }
    }

    function initCharts(options) {
        // User activity bar chart
        createOrUpdateChart('userActivityChart', function () {
            return {
                type: 'bar',
                data: {
                    labels: options.userActivity.labels,
                    datasets: [
                        {
                            label: 'Active Users',
                            data: options.userActivity.data,
                            backgroundColor: [
                                'rgba(59, 130, 246, 0.8)',
                                'rgba(37, 99, 235, 0.8)',
                                'rgba(129, 140, 248, 0.8)',
                                'rgba(16, 185, 129, 0.8)'
                            ],
                            borderColor: [
                                'rgba(59, 130, 246, 1)',
                                'rgba(37, 99, 235, 1)',
                                'rgba(129, 140, 248, 1)',
                                'rgba(16, 185, 129, 1)'
                            ],
                            borderWidth: 1,
                            borderRadius: 6
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            mode: 'index',
                            intersect: false
                        }
                    },
                    scales: {
                        x: {
                            grid: {
                                display: false
                            },
                            ticks: {
                                color: '#4b5563' // neutral-600 on light bg
                            }
                        },
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: 'rgba(0, 0, 0, 0.05)'
                            },
                            ticks: {
                                color: '#4b5563',
                                precision: 0
                            }
                        }
                    }
                }
            };
        });

        // VM status donut chart
        createOrUpdateChart('vmStatusChart', function () {
            return {
                type: 'doughnut',
                data: {
                    labels: options.vmStatus.labels,
                    datasets: [
                        {
                            label: 'VMs',
                            data: options.vmStatus.data,
                            backgroundColor: [
                                'rgba(16, 185, 129, 0.8)',   // running
                                'rgba(245, 158, 11, 0.8)',   // disabled
                                'rgba(239, 68, 68, 0.8)',    // deleted
                                'rgba(156, 163, 175, 0.8)'   // other
                            ],
                            borderColor: [
                                'rgba(16, 185, 129, 1)',
                                'rgba(245, 158, 11, 1)',
                                'rgba(239, 68, 68, 1)',
                                'rgba(156, 163, 175, 1)'
                            ],
                            borderWidth: 1
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '65%',
                    plugins: {
                        legend: {
                            position: 'bottom',
                            labels: {
                                color: '#4b5563' // legend text on light bg
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const label = context.label || '';
                                    const value = context.parsed;
                                    return label + ': ' + value;
                                }
                            }
                        }
                    }
                }
            };
        });
    }

    return {
        initCharts: initCharts
    };
})();
