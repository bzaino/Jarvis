using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Jarvis.DataContracts
{ 
    public class WeatherInfo
    {

        public Weather[] weather { get; set; }
        public string _base { get; set; }
        public Main main { get; set; }
        public int dt { get; set; }
        public string name { get; set; }
        public int cod { get; set; }
    }

    public class Main
    {
        public float temp { get; set; }
        public float temp_min { get; set; }
        public float temp_max { get; set; }
    }

    public class Weather
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }


}