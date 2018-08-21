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
    public static class GetWorkItemList
    {
        [FunctionName("GetWorkItemList")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var ctx = AuthContext.GetAuthenticationContext(null);
            try
            {
                var adalCredential = new UserPasswordCredential(Settings.Username, Settings.Password);

                var result = ctx.AcquireTokenAsync(Settings.VstsResourceId, Settings.ClientId, adalCredential).Result;
                var bearerAuthHeader = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                var speechData = GetWorkItemsByQuery(bearerAuthHeader);
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

        public static string GetWorkItemsByQuery(AuthenticationHeaderValue authHeader)
        {
            const string path = "Shared Queries/My Tasks"; //path to the query   
            var speechJson = "{ \"speech\": \"Sorry, an error occurred.\" }";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.VstsCollectionUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "VstsRestApi");
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.Authorization = authHeader;

                //if you already know the query id, then you can skip this step
                var queryHttpResponseMessage = client.GetAsync(Settings.Project + "/_apis/wit/queries/" + path + "?api-version=" + Settings.ApiVersion).Result;

                if (queryHttpResponseMessage.IsSuccessStatusCode)
                {
                    //bind the response content to the queryResult object
                    var queryResult = queryHttpResponseMessage.Content.ReadAsAsync<QueryResult>().Result;
                    var queryId = queryResult.id;

                    //using the queryId in the url, we can execute the query
                    var httpResponseMessage = client.GetAsync(Settings.Project + "/_apis/wit/wiql/" + queryId + "?api-version=" + Settings.ApiVersion).Result;

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var workItemQueryResult = httpResponseMessage.Content.ReadAsAsync<WorkItemQueryResult>().Result;

                        //now that we have a bunch of work items, build a list of id's so we can get details
                        var builder = new System.Text.StringBuilder();
                        foreach (var item in workItemQueryResult.workItems)
                        {
                            builder.Append(item.id.ToString()).Append(",");
                        }

                        //clean up string of id's
                        var ids = builder.ToString().TrimEnd(',');

                        var getWorkItemsHttpResponse = client.GetAsync("_apis/wit/workitems?ids=" + ids + "&fields=System.Id,System.Title,System.State&asOf=" + workItemQueryResult.asOf + "&api-version=" + Settings.ApiVersion).Result;

                        if (getWorkItemsHttpResponse.IsSuccessStatusCode)
                        {
                            var result = getWorkItemsHttpResponse.Content.ReadAsStringAsync().Result;

                            // the work item list is exposed as a JSON object
                            var myWorkItemList = JsonConvert.DeserializeObject<WorkItemList>(result);
                            
                            // I iterate through the list of work items and get each title and state, which I concatenate so the result can be 'spoken'
                            var response = myWorkItemList.value.Aggregate("", (current, item) => current + (item.fields.SystemTitle + " - " + item.fields.SystemState + ". "));

                            // Google Assistant-specific syntax
                            speechJson = "{ \"speech\": \"" + response + "\" }";
                        }
                    }
                }
            }
            return speechJson;
        }
    }
}
