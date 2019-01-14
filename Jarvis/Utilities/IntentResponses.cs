using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Jarvis.Utilities;
using System.Collections.Generic;

namespace Jarvis.Utilities
{
    [Serializable]
    public class IntentResponses : IDialog<object>
    {
        public string IntentName { get; set; }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(HandleResponses);

            return Task.CompletedTask;
        }

        public async Task HandleResponses(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var intent = await Utility.GetLuisIntent(activity.Text);

            //set default intent first
            var _intentName = Utility.Intent.None.ToString();
            if (intent.topScoringIntent.score > 0.6)
            {
                _intentName = intent.topScoringIntent.intent;
            }

            //this.IntentName = _intentName;

            switch (_intentName)
            {
                case nameof(Utility.Intent.Traffic):
                    HandleTrafficIntent(context);
                    break;

                case nameof(Utility.Intent.Weather):
                    HandleWeatherIntent(context);
                    break;

                default:
                    var chatAnswer = await Utility.GetAnswer(activity.Text);
                    await DisplayAnswersAsync(context, chatAnswer);
                    context.Wait(HandleResponses);
                    break;
            }

        }

        /// <summary>
        /// Using the address given to us by the user, get the traffic time and display it to the user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task PostAddressAnswer(IDialogContext context, IAwaitable<string> result)
        {
            var location = await result;
            await context.PostAsync($"I see you want to go to {location}.");

            var driveTime = await Utility.CheckTraffic(location);

            var chatAnswer = await Utility.GetAnswer("Traffic");
            chatAnswer[0].AnswerText += $"there is {driveTime} minutes.";

            await DisplayAnswersAsync(context, chatAnswer);

        }

        /// <summary>
        /// Using the address given to us by the user, get the traffic time and display it to the user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task CheckWeather(IDialogContext context, IAwaitable<string> result)
        {
            var location = await result;

            var weatherInfo = await Utility.CheckWeather(location);

            if (weatherInfo.main != null)
            {
                var responseLocation = weatherInfo.name;
                var currentTemp = Math.Ceiling(weatherInfo.main.temp);
                var lowTemp = Math.Ceiling(weatherInfo.main.temp_min);
                var highTemp = Math.Ceiling(weatherInfo.main.temp_max);
                //description of sky (Clear, cloudy, etc.)
                var skies = weatherInfo.weather[0].description;

                await context.PostAsync(string.Format("In {0}, it is currently {1} degrees and the skies are {2}. The low today will be {3} with a high of {4}.", responseLocation, currentTemp, skies, lowTemp, highTemp));
            }
        }

        private async Task DisplayAnswersAsync(IDialogContext context, List<Jarvis.Models.Answer> chatAnswer)
        {
            if (chatAnswer.Count == 1)
            {
                // return our reply to the user
                await context.PostAsync(chatAnswer[0].AnswerText);
            }
            else if (chatAnswer.Count > 1)
            {
                for (int i = 0; i < chatAnswer.Count; i++)
                {
                    await context.PostAsync(chatAnswer[i].AnswerText);
                }
            }
            else
            {
                //catch all, in case no matching question found
                await context.PostAsync(@"Sorry, we don't have an answer for your question. You can add that question <a href='https://americanstudentassistance.sharepoint.com/teams/DigitalTechnologyTeam/Lists/Bot%20Q%20and%20A%20Ideas/AllItems.aspx'>here</a> and we'll add it to our knowledge base");
            }
        }
        /// <summary>
        /// Ask user where they want to go
        /// </summary>
        /// <param name="context"></param>
        private void HandleTrafficIntent(IDialogContext context)
        {
            PromptDialog.Text(
                context: context,
                resume: PostAddressAnswer,
                prompt: "Where would you like to go?",
                retry: "Sorry, I didn't understand that. Please try again."
            );            
        }

        private void HandleWeatherIntent(IDialogContext context)
        {
            PromptDialog.Text(
                context: context,
                resume: CheckWeather,
                prompt: "What city would you like the weather for? Enter in the City/State or Zip Code for the intended area.",
                retry: "Sorry, I didn't understand that. Please try again."
            );
        }
    }
}