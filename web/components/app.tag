<app>

    <login show={!auth} api_url="{opts.login.api_url}"></login>
    <loglist if={auth} name="{opts.loglist.name}" api_url="{opts.loglist.api_url}" api_code="{opts.loglist.api_code}"></loglist>

    <script>
        var self = this

        var r = route.create()
        r('login',   viewlogin   )
        r('loglist', viewloglist )
        r(           viewlogin   )

        function viewlogin() {
            self.auth = false;
            self.update();
        }

        function viewloglist() {
            console.log('enter loglist')
            self.auth = true;
            self.update();
        }

      //  this.on('mount', () => {
     //       riot.route((collection, action, id) => {
     //       });
     //   });

    //   route.start(true);

    </script>

</app>