using Newtonsoft.Json;

namespace MyVSTSFunction
{
    public class Fields
    {
        [JsonProperty("System.Id")]
        public int SystemId { get; set; }

        [JsonProperty("System.State")]
        public string SystemState { get; set; }

        [JsonProperty("System.Title")]
        public string SystemTitle { get; set; }
    }
}