<ks-log onclick={ toggleDetail }>
    <div class={ opts.successful ? 'logContainer successful' : 'logContainer unsuccessful' }>
        <p class={ opts.successful ? 'successful' : 'unsuccessful' }
        data-is="animore" mount={{ opacity: [0,1], duration: 800, easing: 'easeInOutQuart' }}>
            { opts.timestamp } { opts.successful ? 'OK' : 'Failed' }
        </p>

        <div ref='grow' id='grow'>
            <div ref='measuringWrapper' class='measuringWrapper'>
                <div class="text">
                    <pre>{opts.log}</pre>
                    <br>
                </div>
            </div>
        </div> 
    </div>

    <script>
        toggleDetail(e) {
            var growDiv = this.refs.grow;
            if (growDiv.clientHeight) {
                growDiv.style.height = 0;
            } else {
                var wrapper = this.refs.measuringWrapper;
                growDiv.style.height = wrapper.clientHeight + "px";
            }
        }
    </script>

    <style>

        .logContainer {
            box-shadow: 0px 0px 32px 0px #52414C78;
            background: #ebf5e1;
        }

        .successful {
            color: #74a57f;
        }

        .unsuccessful {
            color: #a54657;
        }

        #grow {
            transition: height .5s;
            height: 0;
            overflow: hidden;
        }
    </style>
</ks-log>