<login>
    <form name="login" id="login" onsubmit="{submit}" action="{opts.api_url + 'login'}" method="post" enctype="multipart/form-data">
        <div class="container">
            <label for="username"><b>Username</b></label>
            <input type="text" placeholder="Enter Username" name="username" required>

            <label for="password"><b>Password</b></label>
            <input type="password" placeholder="Enter Password" name="password" required>

            <button type="submit">Login</button>
            <label>
                <input type="checkbox" checked="checked" name="remember"> Remember me
            </label>
        </div>

        <div class="container" style="background-color:#f1f1f1">
            <button type="button" class="cancelbtn">Cancel</button>
            <span class="password">Forgot <a href="#">password?</a></span>
        </div>
    </form>

    <form>
        <input id="username" <button type="button" onclick={checkLogin}>login</button>
    </form>

    <script>
        submit(e) {
            e.preventDefault();

            var formElem = $("#login");
            var formData = new FormData(formElem[0]);

            $.ajax({
                url: e.target.action,
                type: e.target.method,
                data: formData,
                contentType: false,
                processData: false,
                xhrFields: {
                    withCredentials: false
                },
                success: function (data) {
                    console.log('login data: ' + data)
                    route('loglist');
                },
                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    console.log(XMLHttpRequest.status + ' from ' + url);
                }
            });
        };
    </script>
</login>