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
        this.on('mount', function(){
            self.getState()
        });

        console.log('meter update')

        this.on('loaded', c => {
            this.on('unmount', () => {
                c.destroy()
            })
        })

        function drawChart(dataset){
            console.log(dataset)
            var ctx = document.getElementById("chart").getContext('2d');
            var myChart = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: ["Red", "Blue", "Yellow", "Green", "Purple", "Orange"],
                    datasets: [{
                        label: '# of Votes',
                        data: dataset,
                        backgroundColor: [
                            'rgba(255, 99, 132, 0.2)',
                            'rgba(54, 162, 235, 0.2)',
                            'rgba(255, 206, 86, 0.2)',
                            'rgba(75, 192, 192, 0.2)',
                            'rgba(153, 102, 255, 0.2)',
                            'rgba(255, 159, 64, 0.2)'
                        ],
                        borderColor: [
                            'rgba(255,99,132,1)',
                            'rgba(54, 162, 235, 1)',
                            'rgba(255, 206, 86, 1)',
                            'rgba(75, 192, 192, 1)',
                            'rgba(153, 102, 255, 1)',
                            'rgba(255, 159, 64, 1)'
                        ],
                        borderWidth: 1
                    }]
                },
                options: {
                    scales: {
                        yAxes: [{
                            ticks: {
                                beginAtZero:true
                            }
                        }]
                    }
                }
            }); 
            self.trigger('loaded', myChart)
        }

        getState(){
            var url = opts.api_url + 'Log?name=' + opts.name;
            $.ajax({
                url: url,
                type: "GET",
                dataType: "json",
                contentType: "application/json; charset=utf-8",
                xhrFields: { withCredentials: false },
                success: function(data) {
                    console.log(data)
                    self.data = data
                    self.total = data.logs.length
                    self.dataset = data.logs.map(x => x.result.replace(/\D+$/g, "")).reverse()
                    self.update()
                    drawChart(self.dataset)
                },
                error: function(XMLHttpRequest, textStatus, errorThrown) {
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