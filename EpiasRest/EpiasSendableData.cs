using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Epias
{
    namespace Send
    {
        public class Header
        {

            [JsonProperty("key")]
            public string Key;

            [JsonProperty("value")]
            public string Value;
        }

        public class OsosDataTypeList
        {

            [JsonProperty("eic")]
            public string Eic;

            [JsonProperty("meteringTime")]
            public string MeteringTime;

            [JsonProperty("period")]
            public string Period;

            [JsonProperty("meteringType")]
            public int MeteringType;

            [JsonProperty("consumptionAmount")]
            public double ConsumptionAmount;

            [JsonProperty("generationAmount")]
            public double GenerationAmount;
        }

        public class Body
        {

            [JsonProperty("ososDataTypeList")]
            public OsosDataTypeList[] OsosDataTypeList;
        }

        public class EpiasSendableData
        {

            [JsonProperty("header")]
            public Header[] Header;

            [JsonProperty("body")]
            public Body Body;
        }
    }
}
