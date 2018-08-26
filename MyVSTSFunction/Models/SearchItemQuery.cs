using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyVSTSFunction
{
    public class SearchItemQuery
    {
        public string searchText { get; set; }
        [JsonProperty("$skip")]
        public int skip { get; set; }
        [JsonProperty("$top")]
        public int top { get; set; }

        public Filters filters { get; set; }

        [JsonProperty("$orderBy")]
        public List<OrderBy> orderBy { get; set; }
        public bool includeFacets { get; set; }
    }
}