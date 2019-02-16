<ks-app>

    <ks-login if={displayLogin} api_url="{opts.login.api_url}"></ks-login>
    <ks-meter if={displayDashboard} name="{opts.meter.name}" api_url="{opts.loglist.api_url}" api_code="{opts.loglist.api_code}"></ks-meter>
    <ks-loglist if={displayDashboard} name="{opts.loglist.name}" api_url="{opts.loglist.api_url}" api_code="{opts.loglist.api_code}"></ks-loglist>

    <script>
        var self = this;

        riot.compile('web/components/login.tag', function(){
            riot.mount( 'ks-login')
        }.bind(this))
        riot.compile('web/components/meter.tag', function(){
            riot.mount( 'ks-meter')
        }.bind(this))
        riot.compile('web/components/loglist.tag', function(){
            riot.mount( 'ks-loglist')
        }.bind(this))

        var r = route.create()
        r('login',     doDisplayLogin   )
        r('dashboard', dodisplayDashboard )
        r(             doDisplayLogin   )

        function doDisplayLogin() {
            self.displayLogin = true;
            self.displayDashboard = false;
            self.update();
        }

        function dodisplayDashboard() {
            console.log('enter dashboard')

            if (!Cookies.get('auth')) {
                route('login');
                return;
            }

            self.displayLogin = false;
                self.displayDashboard = true;
                self.update();
        }

        this.on('mount', () => {
            if (Cookies.get('auth'))
                route('dashboard');
            else
                route('login');
        });
    </script>

</ks-app>