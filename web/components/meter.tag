<ks-meter>
    <h3>{ opts.name }</h3>

    <p>{ this.current } remaining</p>

    <div class="chart-container" style="position: relative; height:40vh; width:80vw">
        <canvas id="chart"></canvas>
    </div>

    <script>
        Chart.defaults.global.responsive = true
        Chart.defaults.global.responsiveAnimationDuration = 500
        Chart.defaults.global.maintainAspectRatio = false

        var self = this
        this.current = 0
        this.total = 0
        this.dataset = []
        this.data = []
        this.on('mount', function () {
            self.getState()
        });

        console.log('meter update')

        this.on('loaded', c => {
            this.on('unmount', () => {
                c.destroy()
            })
        })

        function drawChart(dataset) {
            var ctx = document.getElementById("chart").getContext('2d');
            var myChart = new Chart(ctx, {
                type: 'line',
                data: {
                    datasets: [{
                        label: opts.name,
                        data: dataset,
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.2)'
                        ],
                        borderColor: [
                            'rgba(55,99,240,1)'
                        ],
                        borderWidth: 1
                    }]
                },
                options: {
                    scales: {
                        xAxes: [{
                            type: 'time',
                            time: {
                                //unit: "hour",
                                displayFormats: {
                                    hour: "DD/MM HH:00",
                                    minute: "HH:mm:ss",
                                    second: "HH:mm:ss"
                                }
                            },
                            display: true,
                            scaleLabel: {
                                display: true,
                                labelString: 'Time'
                            },
                            ticks: {
                                major: {
                                    fontStyle: 'bold',
                                    fontColor: '#FF0000'
                                }
                            }
                        }],
                        yAxes: [{
                            ticks: {
                                beginAtZero: true
                            }
                        }]
                    }
                }
            });
            self.trigger('loaded', myChart)
        }

        getState() {
            var url = opts.api_url + 'Log?name=' + opts.name;
            $.ajax({
                url: url,
                type: "GET",
                dataType: "json",
                contentType: "application/json; charset=utf-8",
                xhrFields: {
                    withCredentials: false
                },
                success: function (data) {
                    console.log(data)
                    self.data = data
                    self.total = data.logs.length
                    self.dataset = data.logs.map(log => ({
                        x: log.timestamp,
                        y: log.result.replace(/\D+$/g, "") // extract number value at start of string
                    })).reverse()
                    self.update()
                    drawChart(self.dataset)
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    if (XMLHttpRequest.status == 401) {
                        Cookies.remove('auth');
                        route('login');
                    }
                    console.log('loglist: ' + XMLHttpRequest.status + ' from ' + url);
                }
            });
        }
    </script>

    <style scoped>
        :scope {
            display: inline-block;
            width: 100%;
        }
    </style>

</ks-meter>