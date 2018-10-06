<loglist>
  <h3>{ opts.name }</h3>

  <ul>
    <li each={ item in items }>
      <log successful={item.successful} timestamp={item.timestamp} log={item.result}></log>
    </li>
  </ul>

  <script>
    // xyzzy remote/local
    var logTagUrl = "web/components/log.tag"

    var self = this
    this.items = []
    this.on('mount', function(){
      self.items = self.allTodos()
    });

    var self = this
    allTodos(){
      var url = opts.apiUrl + 'Log?name=' + opts.name + opts.apiCode;
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
          riot.compile(logTagUrl, function() {
            riot.mount('log');
          });
        },
        error: function(XMLHttpRequest, textStatus, errorThrown) { 
          console.log(XMLHttpRequest.status + ' from ' + url);
         }  
      });
    }
  </script>

</loglist>