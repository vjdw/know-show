<loglist>
  <h3>{ opts.name }</h3>

  <ul>
    <li each={ item in items }>
      <log successful={item.successful} timestamp={item.timestamp} log={item.result}></log>
    </li>
  </ul>

  <script>
    var logTagUrl = "web/components/log.tag"

    var self = this
    this.items = []
    this.on('mount', function(){
      var logs = self.getLogs()
      self.update({items: logs})
    });

    var self = this
    getLogs(){
      var url = opts.api_url + 'Log?name=' + opts.name + opts.api_code;
      console.log('going to ' + url);
      $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        xhrFields: { withCredentials: false },
        success: function(data) {
          console.log(data)
          self.items = data.logs
          self.update()
        },
        error: function(XMLHttpRequest, textStatus, errorThrown) { 
          console.log(XMLHttpRequest.status + ' from ' + url);
         }  
      });
    }
  </script>

</loglist>