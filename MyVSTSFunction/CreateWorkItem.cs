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
                var adalCredential = new UserPasswordCredential(Settings.VstsUsername, Settings.VstsPassword);

                var result = ctx.AcquireTokenAsync(Settings.VstsResourceId, Settings.VstsClientId, adalCredential).Result;
                var bearerAuthHeader = new AuthenticationHeaderValue("Bearer", result.AccessToken);

                dynamic data = await req.Content.ReadAsAsync<object>();

                var workItemTitle = data?.queryResult.parameters.workItemTitle.ToString();

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
            var speechJson = "{ \"fulfillmentText\": \"Sorry, an error occurred.\" }";
            var requestPath = "DefaultCollection/" + Settings.VstsProject + "/_apis/wit/workitems/$Task?api-version=" + Settings.VstsApiVersion;
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
                    value = Settings.VstsProject
                },
                new
                {
                    op = "add",
                    path = "/fields/System.IterationPath",
                    value = Settings.VstsProject + "\\Iteration 1"
                },
                new
                {
                    op = "add",
                    path = "/fields/System.AssignedTo",
                    value = Settings.VstsFullUsername
                }
            };

            var serializedWorkItemObject = JsonConvert.SerializeObject(workItemObject);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.VstsCollectionUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "VstsRestApi");
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.Authorization = authHeader;

                var content = new StringContent(serializedWorkItemObject, Encoding.UTF8, "application/json-patch+json");

                var url = new Uri(client.BaseAddress + requestPath);
                var queryHttpResponseMessage = client.PatchAsync(url, content).Result;
                
                if (queryHttpResponseMessage.IsSuccessStatusCode)
                {
                    var response = "Work item created";
                    speechJson = "{ \"fulfillmentText\": \"" + response + "\" }";
                }
            }
            return speechJson;
        }
    }
}
