<ks-loglist>
  <h3>{ opts.name }</h3>
  
  <ul>
    <li each={ item in items }>
      <ks-log successful={item.successful} timestamp={item.timestamp} log={item.result}></ks-log>
    </li>
  </ul>

  <script>
    var self = this
    this.items = []

    riot.compile('web/components/log.tag', function(){
      riot.mount( 'ks-log')
    }.bind(this))
   
    this.on('mount', function(){
      var logs = self.getLogs()
      self.update({items: logs})
    });

    getLogs(){
      var url = opts.api_url + 'Log?name=' + opts.name + opts.api_code;
      $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        xhrFields: { withCredentials: false },
        success: function(data) {
          console.log(data)
          self.items = data.logItems
          self.update()
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

</ks-loglist>