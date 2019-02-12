using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Jarvis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace Jarvis.Utilities
{
    public class DataAccess
    {
        /// <summary>
        /// Log User Activity in DB
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static async Task<int> LogActivity(Activity activity)
        {
            using (var client = GetHttpClient())
            {
                var userlog = new UserLogModel
                {
                    Channel = activity.ChannelId,
                    UserId = activity.From.Id,
                    UserName = activity.From.Name,
                    ActivityDate = DateTime.UtcNow,
                    Message = activity.Text.Truncate(500)
                };

                HttpResponseMessage response = await client.PostAsJsonAsync("api/userlogs", userlog).ConfigureAwait(false);

                var result = response.Content.ReadAsAsync<UserLogModel>();
                //return new log's ID
                return result.Result.Id;
            }
        }

        /// <summary>
        /// Log Error info in DB
        /// </summary>
        /// <param name="userLogId"></param>
        /// <param name="activityId"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static async Task LogError(int userLogId, string activityId, string errorMessage)
        {
            // Create a new ErrorModel object
            var errorInfo = new ErrorModel
            {
                UserLogId = userLogId,
                ActivityId = activityId,
                ErrorMessage = errorMessage,
                CreatedDate = DateTime.UtcNow
            };

            using (var client = GetHttpClient())
            {
                HttpResponseMessage response = await client.PostAsJsonAsync("api/errors", errorInfo).ConfigureAwait(false);

                var result = response.Content.ReadAsAsync<ErrorModel>();
            }
        }

        /// <summary>
        /// Get question information from DB based on intent name
        /// </summary>
        /// <param name="intent"></param>
        /// <returns></returns>
        public static async Task<QuestionModel> GetQuestion(string intent)
        {
            using (var client = GetHttpClient())
            {
                var response = new HttpResponseMessage();

                response = await client.GetAsync($"api/questions/{intent}").ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error getting question information for intent {intent}");
                }

                return response.Content.ReadAsAsync<QuestionModel>().Result;
            }
        }


        public static async Task<List<AnswerModel>> GetAnswers(int questionId)
        {
            using (var client = GetHttpClient())
            {
                var response = new HttpResponseMessage();

                response = await client.GetAsync($"api/answers/{questionId}").ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error getting answer information for question ID {questionId}");
                }

                var result = response.Content.ReadAsAsync<List<AnswerModel>>().Result;

                return result;
            }

        }

        /// <summary>
        /// Generic method to create an HttpClient for communicating with web services
        /// </summary>
        /// <returns></returns>
        private static HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(Utility.dataAccessUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }


    }
}