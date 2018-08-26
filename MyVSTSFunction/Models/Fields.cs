using Newtonsoft.Json;

namespace MyVSTSFunction
{
    public class Fields
    {
        [JsonProperty("system.id")]
        public int SystemId { get; set; }

        [JsonProperty("system.state")]
        public string SystemState { get; set; }

        [JsonProperty("system.title")]
        public string SystemTitle { get; set; }
    }
}