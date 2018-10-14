<login>
    <button type="button" onclick={checkLogin}>login</button>

    <script>
        this.checkLogin = function (e) {
            console.log('routing to loglist')
            route('loglist');
        }
    </script>
</login>