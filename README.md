handlebarrit
---

Azure Functions implementation that accepts a JSON request and transforms the template/data properties into a string result via Handlebars.

Ex:

POST https://handlebarrit.azurewebsites.net/api/process/

``` JSON
{
	"template": "{{message}}",
	"data": {
		"message": "Hello, World!"
	}
}
```

Response (text/html):
"Hello, World!"

It's also possible to change the response content-type via an aptly named contentType property

``` JSON
{
	"template": "<?xml version=\"1.0\" encoding=\"utf-8\"?><message>{{message}}</message>",
	"data": {
		"message": "Hello, World!"
	},
	"contentType": "text/xml"
}
```

Response (text/xml):
<?xml version="1.0" encoding="utf-8"?>
<message>Hello, World!</message>
```
--- 

In addition to the Handlebars built-in helpers, the following helpers are available:

 - eachBySort: Sorts the specified data by the specified property and applies the template for each.

 ``` JSON
 [{
  "template": "<div class=\"entry\">
  <ul>
    {{#eachBySort tasks 'orderHint' 'desc'}}
    <li>{{title}}</li>
    {{/eachBySort}}
  </ul>
</div>",
  "data": {
  	"tasks": [
    {
      "title": "something or other",
      "orderHint": 12345
    },
    {
      "title": "the first one",
      "orderHint": 1000
    }
  ]},
  "contentType": "text/html"
}]
```

 - currentTime: Writes the current time in the specified time zone and date/time format. Optionally: culture.

 ``` JSON
 {
  "template": "<div class=\"entry\">
  {{currentTime 'Eastern Standard Time' 'MM-dd-yyyy h:m tt'}}
</div>",
  "data": {}
}
```

 - addHours: Same as currentTime but adds the specified number of hours

 ``` JSON
 {
  "template": "<div class=\"entry\">
  {{addHours 'Eastern Standard Time' '12' 'MM-dd-yyyy hh:mm tt'}}
</div>",
  "data": {}
}
 ```

 - format: For the given string, attempts to parse as (1) a date time (2) a number and formats the result

 ``` JSON
 {
  "template": "<div class=\"entry\">
  {{format startWorkTime 'h:mm tt'}}
</div>",
  "data": { "startWorkTime": "2009/03/01T10:00:00-3:00"}
}
```

 - humanize - Turn an otherwise computerized string into a more readable human-friendly one.
 - dehumanize - Dehumanize a human friendly string into a computer friendly one:
