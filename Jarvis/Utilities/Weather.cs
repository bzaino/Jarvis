using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using AdaptiveCards;
using System.Configuration;
using Jarvis.DataContracts;
using Jarvis.DataContracts.WeatherData;


namespace Jarvis.Utilities
{
    public class Weather
    {
        public static async Task<AdaptiveCard> GetCard(string place, bool showWeekendWeather)
        {
            //WeatherModel model = new Repository().GetWeatherData(ConfigurationManager.AppSettings["APIXUKey"], GetBy.CityName, place, Days.Five);
            var model = await Utility.CheckWeather(place, showWeekendWeather);

            var card = new AdaptiveCard("1.0");
            if (model != null)
            {
                if (model.current != null)
                {
                    card.Speak = $"<s>Today the temperature is {model.current.temp_f}</s><s>Winds are {model.current.wind_mph} miles per hour from the {model.current.wind_dir}</s>";
                }

                if (model.forecast != null && model.forecast.forecastday != null)
                {
                    AddCurrentWeather(model, card);
                    AddForecast(place, model, card);
                    return card;
                }
            }
            return null;
        }

        private static void AddCurrentWeather(WeatherInfo model, AdaptiveCard card)
        {
            var current = new AdaptiveColumnSet();
            card.Body.Add(current);

            var currentColumn = new AdaptiveColumn();
            current.Columns.Add(currentColumn);
            currentColumn.Width = "35";

            var currentImage = new AdaptiveImage();
            currentColumn.Items.Add(currentImage);
            currentImage.Url = new Uri(GetIconUrl(model.current.condition.icon));

            var currentColumn2 = new AdaptiveColumn();
            current.Columns.Add(currentColumn2);
            currentColumn2.Width = "65";

            string date = DateTime.Parse(model.current.last_updated).DayOfWeek.ToString();

            AddTextBlock(currentColumn2, $"{model.location.name} ({date})", AdaptiveTextSize.Large, false);
            AddTextBlock(currentColumn2, $"{model.current.temp_f.ToString().Split('.')[0]}° F", AdaptiveTextSize.Large);
            AddTextBlock(currentColumn2, $"{model.current.condition.text}", AdaptiveTextSize.Medium);
            AddTextBlock(currentColumn2, $"Winds {model.current.wind_mph} mph {model.current.wind_dir}", AdaptiveTextSize.Medium);
        }
        private static void AddForecast(string place, WeatherInfo model, AdaptiveCard card)
        {
            var forecast = new AdaptiveColumnSet();
            card.Body.Add(forecast);

            foreach (var day in model.forecast.forecastday)
            {
                if (DateTime.Parse(day.date).DayOfWeek != DateTime.Parse(model.current.last_updated).DayOfWeek)
                {
                    var column = new AdaptiveColumn();
                    AddForcastColumn(forecast, column, place);
                    AddTextBlock(column, DateTimeOffset.Parse(day.date).DayOfWeek.ToString().Substring(0, 3), AdaptiveTextSize.Medium);
                    AddImageColumn(day, column);
                    AddTextBlock(column, $"{day.day.mintemp_f.ToString().Split('.')[0]}/{day.day.maxtemp_f.ToString().Split('.')[0]}", AdaptiveTextSize.Medium);
                }
            }
        }
        private static void AddImageColumn(Forecastday day, AdaptiveColumn column)
        {
            var image = new AdaptiveImage();
            image.Size = AdaptiveImageSize.Auto;
            image.Url = new Uri(GetIconUrl(day.day.condition.icon));
            column.Items.Add(image);
        }
        private static string GetIconUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (url.StartsWith("http"))
                return url;
            //some clients do not accept \\
            return "https:" + url;
        }
        private static void AddForcastColumn(AdaptiveColumnSet forecast, AdaptiveColumn column, string place)
        {
            forecast.Columns.Add(column);
            column.Width = "20";
            var action = new AdaptiveOpenUrlAction();
            action.Url = new Uri($"https://www.bing.com/search?q=forecast in {place}");
            column.SelectAction = action;
        }

        private static void AddTextBlock(AdaptiveColumn column, string text, AdaptiveTextSize size, bool isSubTitle = true)
        {
            column.Items.Add(new AdaptiveTextBlock()
            {
                Text = text,
                Size = size,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                IsSubtle = isSubTitle,
                Separator = false
            });
        }

    }
}