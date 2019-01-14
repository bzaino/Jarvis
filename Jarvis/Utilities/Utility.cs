using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Linq;
using System.Web;

using Jarvis.Models;
using Microsoft.Azure.Search;
using System.Configuration;
using Microsoft.Azure.Search.Models;
//using Microsoft.ApplicationInsights;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel.Web;
using System.Runtime.Serialization.Json;
using Jarvis.DataContracts;

namespace Jarvis.Utilities
{
    public static class Utility
    {
        /// <summary>
        /// configuration settings
        /// </summary>
        public static string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
        public static string queryApiKey = ConfigurationManager.AppSettings["SearchServiceQueryApiKey"];
        public static string searchIndexName = ConfigurationManager.AppSettings["SearchIndexName"];
        public static string analyticsTelemetryKey = ConfigurationManager.AppSettings["SearchAnalyticsTelemetryKey"];
        public static string msAppId = ConfigurationManager.AppSettings["MicrosoftAppId"];
        public static string msAppPwd = ConfigurationManager.AppSettings["MicrosoftAppPassword"];
        public static string luisSubscriptionKey = ConfigurationManager.AppSettings["LUIS_SubscriptionKey"];
        public static string luisModelId = ConfigurationManager.AppSettings["LUIS_ModelId"];
        public static string isLuisStaging = ConfigurationManager.AppSettings["LUIS_Staging"];
        public static string debugMode = ConfigurationManager.AppSettings["DebugMode"];
        public static string bingAccessKey = ConfigurationManager.AppSettings["BingSearchKey"];
        public static string bingMapsKey = ConfigurationManager.AppSettings["BingMapsKey"];
        public static string weatherApiKey = ConfigurationManager.AppSettings["WeatherApiKey"];
        public static string weatherApiUriBase = ConfigurationManager.AppSettings["WeatherApiUriBase"];
        public static string trafficApiUriBase = ConfigurationManager.AppSettings["TrafficApiUriBase"];
        public static string googleSearchBase = ConfigurationManager.AppSettings["GoogleSearchBase"];
        public static string googleSearchSuffix = ConfigurationManager.AppSettings["GoogleSearchSuffix"];

        /// <summary>
        /// Intents that require specific functionality, such as follow up questions, in order to provide answers.
        /// An example would be asking for a destination when determining traffic
        /// </summary>
        public enum Intent
        {
            [field:IODescription("None")]
            None,
            [field: IODescription("Traffic")]
            Traffic,
            [field: IODescription("Weather")]
            Weather,
            [field: IODescription("HelpdeskTicket")]
            HelpdeskTicket
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Gets the answer based on the question ID returned back from the Azure search service
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static async Task<List<Answer>> GetAnswer(string messageText)
        {
            List<Answer> chatAnswers = new List<Answer>();

            var intent = await Utility.GetLuisIntent(messageText);

            var questionInfo = GetQuestion(intent);

            if (questionInfo.QuestionText != null)
            {
                using (var db = new ASAChatdataEntities())
                {
                    try
                    {

                        var answers = (from Answer in db.Answers
                                       where Answer.QuestionId == questionInfo.QuestionId
                                       orderby Answer.AnswerOrder ascending
                                       select Answer)
                                    .ToList();

                        chatAnswers = answers;
                    }

                    catch (Exception ex)
                    {
                        var foo = ex.Message;
                    }
                }

                //if we get back answers with AnswerTypeId = 2, then we need to do a web search. Format those answers.
                if (chatAnswers.Count > 0 && chatAnswers[0].AnswerTypeId == 2)
                {
                    FormatWebResponseTypeAnswers(chatAnswers);
                }

            }
            return chatAnswers;
        }

        public static Question GetQuestion(LuisResponse intent)
        {
            var questionList = new List<Question>();
            var questionToReturn = new Question();

            //by default set intent to None. Check response to verify that the intent is a good match (
            var intentName = "None";
            if (intent.topScoringIntent.score > 0.7 )
            {
                intentName = intent.topScoringIntent.intent;
            }

            using (var db = new ASAChatdataEntities())
            {
                try
                {
                    questionList = (from Question in db.Questions
                                   where Question.LuisIntent == intentName
                                select Question)
                                .ToList();
                }

                catch (Exception ex)
                {
                    var foo = ex.Message;
                }
            }

            if (questionList.Count > 0)
            {
                questionToReturn = questionList[0];
            }

            return questionToReturn;
        }

        /// <summary>
        /// Make call to Azure search service to determine most likely question match based on the question asked by the user
        /// </summary>
        /// <param name="questionText"></param>
        /// <returns></returns>
        //private static Question GetQuestion(string questionText)
        //{
        //    DocumentSearchResult<Question> results;

        //    ISearchIndexClient indexClientForQueries = CreateSearchIndexClient();

        //    results = indexClientForQueries.Documents.Search<Question>(questionText);

        //    //LogSearchAnalytics(questionText, results.Results.Count);

        //    return results.Results[0].Document;
        //}

        //private static void LogSearchAnalytics(string questionText, int resultsCount)
        //{
        //    var telemetryClient = new TelemetryClient();
        //    telemetryClient.InstrumentationKey = analyticsTelemetryKey;

        //    var properties = new Dictionary<string, string> {
        //            {"SearchServiceName", searchServiceName},
        //            {"IndexName", searchIndexName},
        //            {"QueryTerms", questionText},
        //            {"ResultCount", resultsCount.ToString()}
        //        };
        //    telemetryClient.TrackEvent("Search", properties);
        //}

        /// <summary>
        /// Build Search Index client to query Azure Search index
        /// </summary>
        /// <returns></returns>
        private static SearchIndexClient CreateSearchIndexClient()
        {
            SearchIndexClient indexClient = new SearchIndexClient(searchServiceName, searchIndexName, new SearchCredentials(queryApiKey));
            return indexClient;
        }

        public static void LogActivity(Activity activity)
        {
            using (var db = new ASAChatdataEntities())
            {
                // Create a new UserLog object
                UserLog newUserLog = new UserLog();
                // Set the properties on the UserLog object
                newUserLog.Channel = activity.ChannelId;
                newUserLog.UserId = activity.From.Id;
                newUserLog.UserName = activity.From.Name;
                newUserLog.ActivityDate = DateTime.UtcNow;
                newUserLog.Message = activity.Text.Truncate(500);
                // Add the UserLog object to UserLogs
                db.UserLogs.Add(newUserLog);
                // Save the changes to the database
                try
                {
                    db.SaveChanges();
                }

                catch (Exception ex)
                {
                    var foo = ex.InnerException;
                }
            }
        }

        public static async Task<LuisResponse> GetLuisIntent(string questionText)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            var luisObj = new LuisResponse();

            // This app ID is for a public sample app that recognizes requests to turn on and turn off lights
            var luisAppId = luisModelId;
            var endpointKey = luisSubscriptionKey;

            // The request header contains your subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", endpointKey);

            // The "q" parameter contains the utterance to send to LUIS
            queryString["q"] = questionText;

            // These optional request parameters are set to their default values
            //queryString["timezoneOffset"] = "0";
            //queryString["verbose"] = "false";
            //queryString["spellCheck"] = "false";
            queryString["staging"] = isLuisStaging;

            try
            {
                var endpointUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" + luisAppId + "?" + queryString;
                var response = await client.GetAsync(endpointUri);

                var strResponseContent = await response.Content.ReadAsStringAsync();

                luisObj = Newtonsoft.Json.JsonConvert.DeserializeObject<LuisResponse>(strResponseContent);
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                throw;
            }


            return luisObj;
        }

        /// <summary>
        /// Display give users a link to a Google search based on the intent found
        /// </summary>
        /// <param name="answers"></param>
        private static void FormatWebResponseTypeAnswers(List<Answer> answers)
        {
            int targetIndex = answers.Count - 1;
            var searchString = answers[targetIndex].AnswerText;
            answers[targetIndex].AnswerText = String.Format("I did a Google search for you and found some results. Go {0} to see what I found", "<a href='" + googleSearchBase + searchString + " near 33 Arch St Boston Ma'>here</a>");
        }

        /// <summary>
        /// Makes a call to Bing's MapAPI to get the drive time with taffic between 2 points
        /// param wp.0 = starting point
        /// param wp.1 = destination
        /// param optmz=timeWithTraffic gives drive time in seconds with traffic taken into account
        /// </summary>
        /// <param name="destination"></param>
        /// <returns>Route time in minutes</returns>
        public static async Task<double> CheckTraffic(string destination)
        {
            string startingPoint = "33 Arch St, Boston, MA";
            double duration = 0;

            Uri geocodeRequest = new Uri(string.Format(trafficApiUriBase, startingPoint, destination, bingMapsKey));

            WebClient wc = new WebClient();
            var mapResponse = await wc.OpenReadTaskAsync(geocodeRequest);

            //Transform web client response to BingApi Response object
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Response));
            var data = ser.ReadObject(mapResponse) as Response;

            //Just get Route data, which has the drive time
            if (data.ResourceSets.Length > 0 && data.ResourceSets[0].Resources.Length > 0)
            {
                var route = data.ResourceSets[0].Resources[0] as Route;
                duration = Math.Ceiling(route.TravelDurationTraffic/60);
            }

            return duration;
        }

        public static async Task<WeatherInfo> CheckWeather (string location)
        {
            var weatherInfo = new WeatherInfo();

            var weatherUrl = new Uri(string.Format(weatherApiUriBase, location, weatherApiKey));

            using (var wc = new WebClient())
            {
                try
                {
                    var weatherResponse = await wc.OpenReadTaskAsync(weatherUrl);

                    //Transform web client response to BingApi Response object
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(WeatherInfo));
                    weatherInfo = serializer.ReadObject(weatherResponse) as WeatherInfo;
                }

                catch (Exception ex)
                {
                    var message = ex.Message;
                }
            }

            return weatherInfo;

        }

    }
}