using System;
using System.Collections.Generic;
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
    public static class UpdateWorkItem
    {
        [FunctionName("UpdateWorkItem")]
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

                var workItemTitle = data?.result.parameters.workItemTitle.ToString();   

                log.Info(workItemTitle);

                var speechData = UpdateWI(bearerAuthHeader, workItemTitle);
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

        public static string UpdateWI(AuthenticationHeaderValue authHeader, string workItemTitle)
        {
            var speechJson = "{ \"speech\": \"Sorry, an error occurred.\" }";
            var workItemId = FindWI(authHeader, workItemTitle);

            var requestPath = Settings.VstsProject + "/_apis/wit/workitems/" + workItemId.ToString() + "?api-version=" + Settings.VstsApiVersion;

            // mark work item as done
            var requestBodyObject = new[]
            {
                new
                {
                    op = "add",
                    path = "/fields/System.State",
                    value = "Closed"
                }
            };

            var serializedWorkItemObject = JsonConvert.SerializeObject(requestBodyObject);

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
                    var response = "Work item marked as done";
                    speechJson = "{ \"speech\": \"" + response + "\" }";
                }
            }
            
            return speechJson;
        }

        public static int FindWI(AuthenticationHeaderValue authHeader, string workItemTitle)
        {
            int workItemId = -1;

            // as of the time of this commit, most api's are on version 4.1, but this one had to be on v4.1-preview.1, so this is why I didn't put Settings.ApiVersion here.
            // api version consistency ftw
            var requestPath = Settings.VstsProject + "/_apis/search/workitemsearchresults?api-version=4.1-preview.1";

            var searchItemQuery = new SearchItemQuery();
            searchItemQuery.searchText = workItemTitle;
            searchItemQuery.skip = 0;
            searchItemQuery.top = 1;
            searchItemQuery.includeFacets = true;
            searchItemQuery.filters = new Filters
            {
                AssignedTo = new List<string>()
            };
            searchItemQuery.filters.AssignedTo.Add(Settings.VstsFullUsername);
            searchItemQuery.orderBy = new List<OrderBy>
            {
                new OrderBy { field = "system.id", sortOrder = "ASC" }
            };

            
            var serializedSearchItemQuery = JsonConvert.SerializeObject(searchItemQuery);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.VstsAlmSearchUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "VstsRestApi");
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.Authorization = authHeader;

                var content = new StringContent(serializedSearchItemQuery, Encoding.UTF8, "application/json");
                var url = new Uri(client.BaseAddress + requestPath);
                var queryHttpResponseMessage = client.PostAsync(url, content).Result;

                if (queryHttpResponseMessage.IsSuccessStatusCode)
                {
                    var queryResult = queryHttpResponseMessage.Content.ReadAsStringAsync().Result;
                    var searchResultsList = JsonConvert.DeserializeObject<SearchResultsList>(queryResult);
                    workItemId = searchResultsList.results[0].fields.SystemId;
                }
            }
            return workItemId;
        }
    }
}
