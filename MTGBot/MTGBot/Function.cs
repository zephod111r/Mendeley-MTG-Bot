using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;

//using System.IdentityModel.Tokens.Jwt;
//@""
namespace MTGBot
{
    public static class Function
    {
        [FunctionName("Function")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.User, "get", Route = "card/{name}")]HttpRequestMessage req, string name, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
        }
    }

    public static class Descriptor
    {
        [FunctionName("Descriptor")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.User, "get", Route = "install/descriptor")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string capabilityString = 

            capabilityString = capabilityString.Replace("${host}", req.Url.Scheme.ToString() + "://" + req.Url.Authority);

            CapabilityDescriptor desc = JsonConvert.DeserializeObject<CapabilityDescriptor>(capabilityString);

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            JsonSerializer serialize = new JsonSerializer();
            serialize.Serialize(writer, desc);

            // Fetching the name from the path parameter in the request URL
            return req.CreateResponse(HttpStatusCode.OK, sb.ToString(), "application/json");
        }
    }
}
