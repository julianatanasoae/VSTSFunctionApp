using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace MyVSTSFunction
{
    public static class CreateWorkItem
    {
        [FunctionName("CreateWorkItem")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var ctx = AuthContext.GetAuthenticationContext(null);
            try
            {
                var adalCredential = new UserPasswordCredential(Settings.Username, Settings.Password);

                var result = ctx.AcquireTokenAsync(Settings.VstsResourceId, Settings.ClientId, adalCredential).Result;
                var bearerAuthHeader = new AuthenticationHeaderValue("Bearer", result.AccessToken);

                dynamic data = await req.Content.ReadAsAsync<object>();

                var workItemTitle = data?.result.parameters.workItemTitle.ToString();

                log.Info(workItemTitle);

                var speechData = CreateWI(bearerAuthHeader, workItemTitle);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(speechData, Encoding.UTF8, "application/json")
                };
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong.");
                log.Error("Message: " + ex.Message + "\n");
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }

        public static string CreateWI(AuthenticationHeaderValue authHeader, string workItemTitle)
        {
            var speechJson = "{ \"speech\": \"Sorry, an error occurred.\" }";
            var requestPath = "DefaultCollection/" + Settings.Project + "/_apis/wit/workitems/$Task?api-version=1.0";
            var workItemObject = new[]
            {
                new
                {
                    op = "add",
                    path = "/fields/System.Title",
                    value = workItemTitle
                },
                new
                {
                    op = "add",
                    path = "/fields/System.AreaPath",
                    value = Settings.Project
                },
                new
                {
                    op = "add",
                    path = "/fields/System.IterationPath",
                    value = Settings.Project + "\\Iteration 1"
                },
                new
                {
                    op = "add",
                    path = "/fields/System.AssignedTo",
                    value = "John Doe <john@jutestorg.onmicrosoft.com>"
                }
            };

            var serializedWorkItemObject = JsonConvert.SerializeObject(workItemObject);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.VstsCollectionUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "VstsRestApi");
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.Authorization = authHeader;

                var content = new StringContent(serializedWorkItemObject, Encoding.UTF8, "application/json-patch+json");

                var url = new Uri(client.BaseAddress + requestPath);
                var queryHttpResponseMessage = client.PatchAsync(url, content).Result;
                
                if (queryHttpResponseMessage.IsSuccessStatusCode)
                {
                    var response = "Work item created";
                    speechJson = "{ \"speech\": \"" + response + "\" }";
                }
            }
            return speechJson;
        }
    }
}
