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
using Jarvis.DataContracts.WeatherData;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Jarvis.Utilities
{
    public static class Utility
    {
        /// <summary>
        /// configuration settings
        /// </summary>

        public static string debugMode = ConfigurationManager.AppSettings["DebugMode"];
        public static string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
        public static string queryApiKey = ConfigurationManager.AppSettings["SearchServiceQueryApiKey"];
        public static string searchIndexName = ConfigurationManager.AppSettings["SearchIndexName"];
        public static string analyticsTelemetryKey = ConfigurationManager.AppSettings["SearchAnalyticsTelemetryKey"];
        public static string msAppId = ConfigurationManager.AppSettings["MicrosoftAppId"];
        public static string msAppPwd = ConfigurationManager.AppSettings["MicrosoftAppPassword"];
        //LUIS Info
        public static string luisSubscriptionKey = ConfigurationManager.AppSettings["LUIS_SubscriptionKey"];
        public static string luisModelId = ConfigurationManager.AppSettings["LUIS_ModelId"];
        public static string isLuisStaging = ConfigurationManager.AppSettings["LUIS_Staging"];
        //bing map info 
        public static string bingMapsKey = ConfigurationManager.AppSettings["BingMapsKey"];
        public static string trafficApiUriBase = ConfigurationManager.AppSettings["TrafficApiUriBase"];
        public static string fireDrillUriBase = ConfigurationManager.AppSettings["FireDrillUriBase"];
        //Search info
        public static string googleSearchBase = ConfigurationManager.AppSettings["GoogleSearchBase"];
        public static string googleSearchSuffix = ConfigurationManager.AppSettings["GoogleSearchSuffix"];
        //Weather Api
        public static string weatherApiKey = ConfigurationManager.AppSettings["WeatherApiKey"];
        public static string weatherApiUriBase = ConfigurationManager.AppSettings["WeatherApiUriBase"];
        //email info
        public static string jarvisEmailAddress = ConfigurationManager.AppSettings["JarvisEmailAddress"];
        public static string errorEmailAddress = ConfigurationManager.AppSettings["ErrorEmailAddress"];
        public static string botFrameworkUrl = ConfigurationManager.AppSettings["BotFrameworkUrl"];
        //Azure storage info
        public static string azureStorageAccountName = ConfigurationManager.AppSettings["AzureStorageAccountName"];
        public static string azureStorageAccountKey = ConfigurationManager.AppSettings["AzureStorageAccountKey"];
        public static string azureStorageConnectionString = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
        public static string dataAccessUrl = ConfigurationManager.AppSettings["DataAccessUrl"];

        /// <summary>
        /// Intents that require specific functionality, such as follow up questions, in order to provide answers.
        /// An example would be asking for a destination when determining traffic
        /// </summary>
        public enum Intent
        {
            [field: IODescription("None")]
            None,
            [field: IODescription("Traffic")]
            Traffic,
            [field: IODescription("Weather")]
            Weather,
            [field: IODescription("HelpdeskTicket")]
            HelpdeskTicket,
            [field: IODescription("WhoAmI")]
            WhoAmI,
            [field: IODescription("WeatherWeekend")]
            WeatherWeekend,
            [field: IODescription("FindLunchPlaces")]
            FindLunchPlaces,
            [field: IODescription("FireDrillLocation")]
            FireDrillLocation,                
            [field: IODescription("FindBusiness")]
            FindBusiness
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        
        /// <summary>
        /// Get the Answer(s) based on the intent name. In this situation, the intent will have one or more entities that we will do a search on
        /// </summary>
        /// <param name="intent"></param>
        /// <returns></returns>
        public static async Task<List<AnswerModel>> GetAndFormatAnswer(LuisResponse intent)
        {
            //List<Answer> chatAnswers = new List<Answer>();

            var chatAnswers = new List<AnswerModel>();

            var questionInfo = await DataAccess.GetQuestion(GetHighScoringIntent(intent));

            if (questionInfo.QuestionText != null)
            {
                chatAnswers = DataAccess.GetAnswers(questionInfo.QuestionId).Result;

                //if we get back answers with AnswerTypeId = 2, then we need to do a web search. Format those answers.
                if (chatAnswers.Count > 0 && chatAnswers[0].AnswerTypeId == 2)
                {
                    if (intent.entities.Length > 0)
                    {
                        var searchString = intent.entities[0].entity;

                        for (int i = 1; i < intent.entities.Length; i++)
                        {
                            searchString += $" {intent.entities[i].entity}";
                        }

                        chatAnswers[0].AnswerText = GetGoogleSearchMessage(searchString);
                    }
                }
            }

            return chatAnswers;
        }

        public static string GetHighScoringIntent(LuisResponse intent)
        {
            //by default set intent to None. Check response to verify that the intent is a good match
            var intentName = "None";
            if (intent.topScoringIntent.score > 0.6)
            {
                intentName = intent.topScoringIntent.intent;
            }

            return intentName;
        }
        
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


        public static async Task<LuisResponse> GetLuisIntent(string questionText)
        {
            var luisObj = new LuisResponse();

            using (var client = new HttpClient())
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                var luisAppId = luisModelId;
                var endpointKey = luisSubscriptionKey;

                // The request header contains your subscription key
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", endpointKey);

                // The "q" parameter contains the utterance to send to LUIS
                queryString["q"] = questionText;

                // These optional request parameters are set to their default values
                //queryString["timezoneOffset"] = "0";
                queryString["verbose"] = "true";
                queryString["spellCheck"] = "true";
                queryString["staging"] = isLuisStaging;
                queryString["bing-spell-check-subscription-key"] = "ff8ba62777f84410abdd0b6202e290a6";

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
            }

            return luisObj;
        }

        public static string GetGoogleSearchMessage(string searchInfo)
        {
            return String.Format("I did a Google search for you and found some results. Go {0} to see what I found", "<a href='" + googleSearchBase + Uri.EscapeDataString(searchInfo) + " near 33 Arch St Boston Ma'>here</a>");
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

            using (var wc = new WebClient())
            {
                var mapResponse = await wc.OpenReadTaskAsync(geocodeRequest);

                //Transform web client response to BingApi Response object
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Response));
                var data = ser.ReadObject(mapResponse) as Response;

                //Just get Route data, which has the drive time
                if (data.ResourceSets.Length > 0 && data.ResourceSets[0].Resources.Length > 0)
                {
                    var route = data.ResourceSets[0].Resources[0] as Route;
                    duration = Math.Ceiling(route.TravelDurationTraffic / 60);
                }
            }

            return duration;
        }

        public static async Task<WeatherInfo> CheckWeather(string location, bool getWeekendWeather)
        {
            var weatherInfo = new WeatherInfo();
            var days = "1";

            if (getWeekendWeather)
            {
                days = "7";
            }

            Uri weatherUri = new Uri(string.Format(weatherApiUriBase, weatherApiKey, location, days));

            using (var wc = new WebClient())
            {
                var weatherResponse = await wc.DownloadStringTaskAsync(weatherUri);

                weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(weatherResponse);
            }

            return weatherInfo;

        }

        /// <summary>
        /// Gets image from Bing Maps API
        /// </summary>
        /// <param name="imageUri"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<byte[]> GetImageByUrl(string address)
        {
            byte[] imageArray = new byte[] { };

            using (var wc = new WebClient())
            {
                try
                {
                    imageArray = await wc.DownloadDataTaskAsync(address);
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }

            return imageArray;
        }

        public static async Task SendEmail(string recipientAddress, string emailSubject, string emailBody, string ccEmail = null)
        {
            var botAccount = new ChannelAccount(name: "Jarvis", id: Utility.jarvisEmailAddress);
            var userAccount = new ChannelAccount(id: recipientAddress); //the email account you are sending your messages to
            MicrosoftAppCredentials.TrustServiceUrl(Utility.botFrameworkUrl, DateTime.MaxValue);

            try
            {
                using (var connector = new ConnectorClient(new Uri(Utility.botFrameworkUrl)))
                {
                    var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);

                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = botAccount;
                    message.Recipient = userAccount;
                    message.ChannelData = JsonConvert.SerializeObject(new { subject = emailSubject, ccRecipients = ccEmail });
                    message.Conversation = new ConversationAccount(id: conversationId.Id);
                    message.Text = emailBody;
                    message.Locale = "en-Us";

                    await connector.Conversations.SendToConversationAsync((Activity)message);
                }
            }

            catch (Exception ex)
            {
                throw new Exception($"There was an issue creating your ticket:{ex.InnerException.Message}");
            }
        }

        public static async Task UploadFileToCloud(string fileName, byte[] file)
        {
            var cloudBlobContainer = GetBlobContainer("images");

            var blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            //if the file does not exist, upload it
            if (!CheckCloudFileExists(fileName))
            {
                await blockBlob.UploadFromByteArrayAsync(file, 0, file.Length);
            }
        }

        private static CloudBlobContainer GetBlobContainer(string containerName)
        {
            var connectionString = string.Format(Utility.azureStorageConnectionString, Utility.azureStorageAccountName, Utility.azureStorageAccountKey);
            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            cloudBlobContainer.CreateIfNotExists();

            return cloudBlobContainer;
        }

        public static byte[] GetImageFromDatastore(string fileName)
        {
            var image = new byte[] { };

            var cloudBlobContainer = GetBlobContainer("images");

            var blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);

            try
            {
                //Blob needs to be downloaded as a stream and then converted to a byte array
                //https://stackoverflow.com/questions/43271267/azure-blob-download-as-byte-array-error-memory-stream-is-not-expandable
                using (var ms = new MemoryStream())
                {
                    blockBlob.DownloadToStream(ms);
                    image = ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }            

            return image;
        }

        public static bool CheckCloudFileExists(string fileName)
        {
            var cloudBlobContainer = GetBlobContainer("images");

            var blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);

            return blockBlob.Exists();
        }


    }
}