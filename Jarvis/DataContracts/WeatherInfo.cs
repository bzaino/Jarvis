using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Jarvis.DataContracts.WeatherData
{
    public class WeatherInfo
    {
        public Location location { get; set; }
        public Current current { get; set; }
        public Forecast forecast { get; set; }
    }

    public class Location
    {
        public string name { get; set; }
        public string region { get; set; }
    }

    public class Current
    {
        public float temp_f { get; set; }
        public Condition condition { get; set; }
        public float wind_mph { get; set; }
        public string wind_dir { get; set; }
        public int humidity { get; set; }
        public int cloud { get; set; }
        public float feelslike_f { get; set; }
    }

    public class Condition
    {
        public string text { get; set; }
        public string icon { get; set; }
        public int code { get; set; }
    }

    public class Forecast
    {
        public Forecastday[] forecastday { get; set; }
    }

    public class Forecastday
    {
        public string date { get; set; }
        public Day day { get; set; }
    }

    public class Day
    {
        public float maxtemp_f { get; set; }
        public float mintemp_f { get; set; }
        public float avgtemp_f { get; set; }
        public Condition condition { get; set; }
        public string uv { get; set; }
    }    

}