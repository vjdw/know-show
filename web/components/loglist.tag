<loglist>

  <h3>{ opts.name }</h3>

  <ul>
    <li each={ item in items.filter(whatShow) }>
      <label class={ completed: done }>
        <input type="checkbox" checked={ done } onclick={ parent.toggle }> { item.timestamp + ' : ' + item.result }
      </label>
    </li>
  </ul>

  <form onsubmit={ add }>
    <input ref="input" onkeyup={ edit }>
    <button disabled={ !text }>Add #{ items.filter(whatShow).length + 1 }</button>

    <button type="button" disabled={ items.filter(onlyDone).length == 0 } onclick={ removeAllDone }>
    X{ items.filter(onlyDone).length } </button>
  </form>

  <!-- this script tag is optional -->
  <script>
    //this.items = opts.items

    var self = this
    this.items = []
    this.on('mount', function(){
      self.items = self.allTodos()
    })

    edit(e) {
      this.text = e.target.value
    }

    add(e) {
      if (this.text) {
        this.items.push({ title: this.text })
        this.text = this.refs.input.value = ''
      }
      e.preventDefault()
    }

    removeAllDone(e) {
      this.items = this.items.filter(function(item) {
        //return !item.done
        return false
      })
    }

    // an two example how to filter items on the list
    whatShow(item) {
      //return !item.hidden
      return true
    }

    onlyDone(item) {
      //return item.done
      return true
    }

    toggle(e) {
      var item = e.item
      item.done = !item.done
      return true
    }

    var self = this
    allTodos(){
      var url = 'https://know-show.azurewebsites.net/api/Log?code=DuEIMldhwbmHrpbMVu9fCxXnntVhlTjrQ5oM3odqPvI473o5RALXaQ==&name=Backup';
      $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        success: function(data) {
          console.log(data)
          self.items = data.logs
          self.update()
        }
      });
    }
  </script>

</loglist>
