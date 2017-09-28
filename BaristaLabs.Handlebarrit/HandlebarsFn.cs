namespace BaristaLabs.Handlebarrit
{
    using HandlebarsDotNet;
    using Humanizer;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public static class HandlebarsFn
    {
        [FunctionName("HandlebarsFn")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "process/")]HttpRequestMessage req, TraceWriter log)
        {
            string template = null;
            dynamic data = null;
            string contentType = "text/html";

            if (req.Method == HttpMethod.Get)
            {
                log.Info("C# HTTP trigger function processed a get request.");

                template = @"<div class=""entry"">
  <ul>
    {{#eachBySort . 'orderHint'}}
    <li>{{title}}</li>
    {{/eachBySort}}
  </ul>
</div>";
                data = new[] {
                    new {
                        title = "Something or other",
                        orderHint = 1234
                    },
                    new {
                        title = "the first thing",
                        orderHint = 1000
                    }
                };
            }
            else
            {
                log.Info("C# HTTP trigger function processed a post request.");

                dynamic requestBody = await req.Content.ReadAsAsync<object>();
                if (requestBody is JArray bodyAsArray)
                {
                    requestBody = bodyAsArray.First();
                }

                template = requestBody?.template;
                data = requestBody.data as JToken;
                contentType = requestBody.contentType;
            }

            if (template == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please specify a template in the request body");
            }

            InitializeHandlebars();

            var fnTemplate = Handlebars.Compile(template);
            var result = fnTemplate(data);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(result, Encoding.UTF8, contentType ?? "text/html")
            };
        }

        private static void InitializeHandlebars()
        {
            Handlebars.RegisterHelper("dehumanize", (writer, context, arguments) =>
            {
                if (arguments.Length != 1)
                {
                    throw new HandlebarsException("{{humanize}} helper must have exactly one argument");
                }

                var str = arguments[0].ToString();

                //Some overrides for values that start with '-' -- this fixes two instances in Runtime.UnserializableValue
                if (str.StartsWith("-"))
                {
                    str = $"Negative{str.Dehumanize()}";
                }
                else
                {
                    str = str.Dehumanize();
                }

                writer.WriteSafeString(str.Dehumanize());
            });

            Handlebars.RegisterHelper("humanize", (writer, context, arguments) =>
            {
                if (arguments.Length != 1)
                {
                    throw new HandlebarsException("{{humanize}} helper must have exactly one argument");
                }

                var str = arguments[0].ToString();

                writer.WriteSafeString(str.Humanize());
            });

            Handlebars.RegisterHelper("format", (writer, context, arguments) =>
            {
                if (arguments.Length <= 1)
                {
                    throw new HandlebarsException("{{format}} helper must have at least one argument");
                }

                var format = "";
                if (arguments.Length > 1)
                {
                    format = arguments[1] as string ?? "";
                }

                var culture = CultureInfo.InvariantCulture;
                if (arguments.Length > 2)
                {
                    var cultureName = arguments[2] as string;
                    if (!string.IsNullOrEmpty(cultureName))
                    {
                        culture = new CultureInfo(cultureName);
                    }
                }

                var date = arguments[0] as DateTime?;
                if (date.HasValue)
                {
                    writer.WriteSafeString(date.Value.ToString(format, culture));
                }

                var number = arguments[0] as decimal?;
                if (number.HasValue)
                {
                    writer.WriteSafeString(number.Value.ToString(format, culture));
                }
            });

            Handlebars.RegisterHelper("eachBySort", (writer, options, context, arguments) =>
            {
                if (arguments.Length < 2)
                {
                    throw new HandlebarsException("{{#eachBySort}} helper must have a least two arguments");
                }

                var order = arguments[1] as string;
                if (order == null)
                {
                    throw new HandlebarsException("Sort order must be specified as the second argument. E.g. {{#eachBySort . 'propertyName'}}");
                }

                var ascending = true;
                if (arguments.Length > 2)
                {
                    if (arguments[2] is string direction && direction.ToLowerInvariant() == "desc")
                    {
                        ascending = false;
                    }
                }

                var array = JArray.FromObject(arguments[0]);
                IOrderedEnumerable<JToken> sortedArray;

                if (ascending)
                    sortedArray = array.OrderBy(obj => obj[order]);
                else
                    sortedArray = array.OrderByDescending(obj => obj[order]);

                foreach (var obj in sortedArray)
                {
                    options.Template(writer, obj);
                }
            });
        }
    }
}
