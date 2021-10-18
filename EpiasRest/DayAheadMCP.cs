using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epias
{
    namespace Transparency
    {
        namespace DayAheadMCP
        {
            public class DayAheadMCPList
            {
                [JsonProperty("date")]
                public string Date;

                [JsonProperty("price")]
                public double Price;
            }

            public class Body
            {
                [JsonProperty("dayAheadMCPList")]
                public DayAheadMCPList[] DayAheadMCPList;
            }

            public class DayAheadMCP
            {
                [JsonProperty("resultCode")]
                public string ResultCode;

                [JsonProperty("resultDescription")]
                public string ResultDescription;

                [JsonProperty("body")]
                public Body Body;
            }
        }
    }
}