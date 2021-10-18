using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epias.Recive.Config
{

    public class OsosDataTypeList
    {
        [JsonProperty("eic")]
        public string Eic;

        [JsonProperty("period")]
        public int Period;

        [JsonProperty("meterType")]
        public string MeterType;

        [JsonProperty("balanceType")]
        public string BalanceType;
    }

    public class Body
    {

        [JsonProperty("ososDataTypeList")]
        public OsosDataTypeList[] OsosDataTypeList;
    }

    public class EpiasOsosConfig
    {

        [JsonProperty("resultCode")]
        public string ResultCode;

        [JsonProperty("resultDescription")]
        public string ResultDescription;

        [JsonProperty("body")]
        public Body Body;
    }

}
