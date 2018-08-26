using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyVSTSFunction
{
    public class Filters
    {
        [JsonProperty("System.AssignedTo")]
        public List<string> AssignedTo { get; set; }
    }
}