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


/*
 * 
 * using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace CloudWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private Dictionary<string, Installation> installationStore = new Dictionary<string, Installation>();
        private Dictionary<string, AccessToken> accessTokenStore = new Dictionary<string, AccessToken>();
        private readonly WebClient webClient = new WebClient();
        private readonly Azure azure = new Azure();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private readonly HttpListener listener = new HttpListener();
        public override void Run()
        {
            Trace.TraceInformation("CloudWorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart() 
        {
            webClient.UseDefaultCredentials = false;
            azure.StorageKey = RoleEnvironment.GetConfigurationSettingValue("AzureStorage");

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 125;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            listener.Prefixes.Add(RoleEnvironment.GetConfigurationSettingValue("ListenerConnectionPrefix").ToString());
            listener.Start();

            bool result = base.OnStart();

            Trace.TraceInformation("CloudWorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {

            Trace.TraceInformation("CloudWorkerRole is stopping");

            listener.Stop();
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("CloudWorkerRole has stopped");
        }

        private void ValidateJWT(HttpListenerRequest req)
        {
            string tokenEncoded = "";
            if (req.QueryString.HasKeys() && req.QueryString.AllKeys.Contains("signed_request", StringComparer.InvariantCultureIgnoreCase))
            {
                tokenEncoded = req.QueryString.GetValues("signed_request").ToString();
            }
            else if (req.Headers.HasKeys() && req.Headers.AllKeys.Contains("authorization", StringComparer.InvariantCultureIgnoreCase))
            {
                tokenEncoded = req.Headers.GetValues("authorization").ToString();
            }

            JwtSecurityTokenHandler jwt = new JwtSecurityTokenHandler();
            if (!jwt.CanReadToken(tokenEncoded))
            {
                throw new UnauthorizedAccessException("Invalid OAuth Credentials");
            }
        }

        private async Task<int> Install(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                throw new MissingMemberException("Invalid Post request!");
            }
            Stream stream = request.InputStream;
            System.Text.Encoding encoding = request.ContentEncoding;

            System.IO.StreamReader reader = new System.IO.StreamReader(stream, encoding);
            string body = await reader.ReadToEndAsync();

            Installation installation = JsonConvert.DeserializeObject<Installation>(body);
            string oauth = installation.oauthId;
            installationStore[oauth] = installation;
            JwtSecurityToken token = new JwtSecurityToken(oauth);

            string capabilitiesUrl = installation.capabilitiesUrl;
            string value = await webClient.DownloadStringTaskAsync(capabilitiesUrl);
            dynamic capabilities = JsonConvert.DeserializeObject(value);

            installation.tokenUrl = capabilities.capabilities.oauth2Provider.tokenUrl;
            installation.apiUrl = capabilities.capabilities.hipchatApiProvider.url;

            return 200;
        }

        private async Task Uninstall(HttpListenerRequest request,HttpListenerResponse response)
        {
            string redirectUrl = "";
            string installableUrl = "";
            if (request.QueryString.HasKeys() && request.QueryString.AllKeys.Contains("redirect_url", StringComparer.InvariantCultureIgnoreCase))
            {
                redirectUrl = request.QueryString.GetValues("redirect_url").ToString();
            }
            if (request.QueryString.HasKeys() && request.QueryString.AllKeys.Contains("installable_url", StringComparer.InvariantCultureIgnoreCase))
            {
                installableUrl = request.QueryString.GetValues("installable_url").ToString();
            }

            WebClient client = new WebClient();
            client.UseDefaultCredentials = false;
            string body = await client.DownloadStringTaskAsync(installableUrl);

            dynamic installation = JsonConvert.DeserializeObject(body);
            installationStore.Remove(installation.oauthId);
            accessTokenStore.Remove(installation.oauthId);

            response.Redirect(redirectUrl);
        }

        private bool isExpired(AccessToken accessToken)
        {
            return accessToken.expirationTimeStamp < DateTime.Now;
        }

        private async Task renewAccessToken(string oauthId)
        {
            Installation installation = installationStore[oauthId];
            string jsonParam = @"{
                    uri: ${installation.tokenUrl},  
                    auth: {
                        username: ${oauthId},
                        password: ${installation.oauthSecret},
                    },
                    form: {
                        grant_type: 'client_credentials',
                        scope: 'send_notification'
                    }
                }";
            WebClient client = new WebClient();
            client.UseDefaultCredentials = false;
            string result = await client.UploadStringTaskAsync(installation.tokenUrl, jsonParam);

            dynamic accessToken = JsonConvert.DeserializeObject(result);

            DateTime expires = DateTime.Now.Add((accessToken.expires_in - 60) * 1000);

            accessTokenStore[oauthId] = new AccessToken(expires, accessToken);
        }

        private async Task<AccessToken> GetAccessToken(string oauthId)
        {
            if (!accessTokenStore.ContainsKey(oauthId) || isExpired(accessTokenStore[oauthId]))
            {
                await renewAccessToken(oauthId);
            }

            AccessToken token = accessTokenStore[oauthId];
            return token;
        }

        private async Task Descriptor(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string capabilityString = RoleEnvironment.GetConfigurationSettingValue("CapabilityJson").ToString();

            capabilityString = capabilityString.Replace("${host}", request.Url.Scheme.ToString() + "://" + request.Url.Authority);

            CapabilityDescriptor desc = JsonConvert.DeserializeObject<CapabilityDescriptor>(capabilityString);

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            JsonSerializer serialize = new JsonSerializer();
            serialize.Serialize(writer, desc);

            string responseString = sb.ToString();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.LongLength;
            response.StatusCode = 200;
            System.IO.Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
        }

        private async Task ParseHttpRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string[] names = request.RawUrl.Remove(0,1).Split('/');

            if (names[0] == "cards")
            {
                string url = azure.IsBlobPresent(names[2], names[1]);
                if (url == "")
                {
                    throw new MissingMemberException("Card not found");
                }

                string responseString = "<HTML><BODY><img src=" + url + " height=\"311\" width=\"223\"/></BODY></HTML>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.LongLength;
                response.StatusCode = 200;
                System.IO.Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                output.Close();
            }
            else
            {
                throw new EntryPointNotFoundException("Method not found");
            }

        }

        private async Task RespondToRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.RawUrl == "/installed")
            {
                response.StatusCode = await Install(request);
            }
            else if (request.RawUrl == "/uninstall")
            {
                await Uninstall(request, response);
            }
            else if(request.RawUrl == "/descriptor" || request.RawUrl == "/capabilities.json")
            {
                await Descriptor(context);
            }
            else
            {
                ValidateJWT(request);
                await ParseHttpRequest(context);
            }
        }

        private async Task SendStringResponse(HttpListenerResponse response, int status, string body)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
            response.ContentLength64 = buffer.LongLength;
            response.StatusCode = status;
            System.IO.Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
        }
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                var context = listener.GetContext();

                try
                {
                    await RespondToRequest(context);
                }
                catch (EntryPointNotFoundException err)
                {
                    await SendStringResponse(context.Response, 503, "<HTML><BODY>" + err.Message + "</BODY></HTML>");
                }
                catch (MethodAccessException err)
                {
                    await SendStringResponse(context.Response, 501, "<HTML><BODY>" + err.Message + "</BODY></HTML>");
                }
                catch (MissingMemberException err)
                {
                    await SendStringResponse(context.Response, 404, "<HTML><BODY>" + err.Message + "</BODY></HTML>");
                }
                catch (UnauthorizedAccessException err)
                {
                    await SendStringResponse(context.Response, 403, "<HTML><BODY>" + err.Message + "</BODY></HTML>");
                }
                catch(Exception err)
                {
                    await SendStringResponse(context.Response, 500, "<HTML><BODY>" + err.Message + "</BODY></HTML>");
                }
                finally
                {
                }

            }
        }
    }
}
 */