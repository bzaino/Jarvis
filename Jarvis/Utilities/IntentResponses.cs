using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Jarvis.Utilities;
using System.Collections.Generic;
using System.Text;
using Atlassian.Jira;
using System.Net.Mail;

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
                    await HandleWeatherIntent(context, intent, false);
                    break;

                case nameof(Utility.Intent.WeatherWeekend):
                    await HandleWeatherIntent(context, intent, true);
                    break;

                case nameof(Utility.Intent.WhoAmI):
                    ///await HandleWhoAmI(context);
                    break;

                case nameof(Utility.Intent.HelpdeskTicket):
                    await HandleHelpdeskIntent(context);
                    break;

                default:
                    var chatAnswer = await Utility.GetAnswer(activity.Text);
                    await DisplayAnswersAsync(context, chatAnswer);
                    context.Wait(HandleResponses);
                    break;
            }

        }

        private async Task HandleHelpdeskIntent(IDialogContext context)
        {
            await context.PostAsync("Let me open a Help Desk ticket for you. First I need to get some information.");

            //this will set the ticket's summary and description to the same value. We can also prompt the user for a brief summary and detailed description if needed
            PromptDialog.Text(
                context: context,
                resume: CreateHelpdeskTicket,
                prompt: "Please give me a brief description of your issue.",
                retry: "Sorry, I didn't understand that. Please try again."
            );
        }

        /// <summary>
        /// Using info gathered from user, create a helpdesk ticket
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task CreateHelpdeskTicket(IDialogContext context, IAwaitable<string> result)
        {
            var ticketDescription = await result;

            try
            {
                //send an email to JSD to create the ticket
                var sender = await GetEmail(context);
                                    
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(sender);
                message.To.Add(new MailAddress(Utility.debugMode == "true" ? "mlemieux@asa.org" : "jiraservicedesk@asa.org"));
                message.Subject = ticketDescription.Length > 50 ? ticketDescription.Substring(0,50) : ticketDescription;
                message.IsBodyHtml = true;
                message.Body = ticketDescription;
                smtp.Port = 25;
                smtp.Host = "mailhost";
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);

                await context.PostAsync("I opened up ticket for you and you should be getting an email about it soon. Best of luck.");
            }

            catch (Exception ex)
            {
                await context.PostAsync($"There was an issue creating your ticket:{ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Using the address given to us by the user, get the traffic time and display it to the user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task GetTrafficInfo(IDialogContext context, IAwaitable<string> result)
        {
            var location = await result;
            await context.PostAsync($"I see you want to go to {location}.");

            var driveTime = await Utility.CheckTraffic(location);

            var chatAnswer = await Utility.GetAnswer("Traffic");
            chatAnswer[0].AnswerText += $"there is {driveTime} minutes.";

            await DisplayAnswersAsync(context, chatAnswer);

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
                resume: GetTrafficInfo,
                prompt: "Where would you like to go?",
                retry: "Sorry, I didn't understand that. Please try again."
            );            
        }

        private async Task HandleWeatherIntent(IDialogContext context, LuisResponse intent, bool showWeekendInfo)
        {
            if (intent.entities.Length > 0)
            {
                var location = string.Empty;
                //check to see if we just have a city, or a city and a state.
                if (intent.entities.Length == 2)
                {
                    location = intent.entities[0].entity + "," + intent.entities[1].entity;
                }
                else
                {
                    location = intent.entities[0].entity;
                }

                await GetDisplayWeatherInfo(context, location, showWeekendInfo);
            }
            else
            {
                context.UserData.SetValue("intentName", intent.topScoringIntent.intent);

                PromptDialog.Text(
                    context: context,
                    resume: CheckWeatherUserInfo,
                    prompt: "What city would you like the weather for? Enter in the City/State or Zip Code for the intended area.",
                    retry: "Sorry, I didn't understand that. Please try again."
                );
            }
        }

        private async Task CheckWeatherUserInfo(IDialogContext context, IAwaitable<string> result)
        {
            var location = await result;

            var showWeekendInfo = false;

            if (context.UserData.ContainsKey("intentName"))
            {
                if (context.UserData.GetValue<string>("intentName") == nameof(Utility.Intent.WeatherWeekend))
                {
                    showWeekendInfo = true;
                }
            }

            await GetDisplayWeatherInfo(context, location, showWeekendInfo);
        }

        /// <summary>
        /// Make Api call to get the weather info and display the response to the user
        /// </summary>
        /// <param name="context"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private async Task GetDisplayWeatherInfo(IDialogContext context, string location, bool showWeekendInfo)
        {
            var weatherInfo = await Utility.CheckWeather(location, showWeekendInfo);

            if (weatherInfo.current == null)
            {
                //Location not found
                await context.PostAsync(string.Format("Sorry, we could not find a weather report for the location you entered: {0}", location));
            }
            else if (weatherInfo.forecast.forecastday.Length > 0)
            {
                var responseLocation = weatherInfo.location.name;
                var currentTemp = Math.Ceiling(weatherInfo.current.temp_f);
                var lowTemp = Math.Ceiling(weatherInfo.forecast.forecastday[0].day.mintemp_f);
                var highTemp = Math.Ceiling(weatherInfo.forecast.forecastday[0].day.maxtemp_f);
                //description of sky (Clear, cloudy, etc.)
                var skies = weatherInfo.current.condition.text;

                await context.PostAsync(string.Format("In {0}, it is currently {1} degrees and it is {2}. The low today will be {3} with a high of {4}.", responseLocation, currentTemp, skies, lowTemp, highTemp));

            }
            else
            {
                await context.PostAsync(string.Format("Sorry, we had a problem finding a weather report for the location you entered: {0}. Please try again later.", location));
            }
        }
        
        private async Task<string> GetEmail(IDialogContext context)
        {
            //default username
            var email = "Jarvis@asa.org";

            //within Teams, we have access to the user's email address, since it is linked to Microsoft. Other channels do not have this detail, so we will create the ticket under Jarvis
            if (context.Activity.ChannelId == "msteams")
            {
                var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
                var members = await connector.Conversations.GetConversationMembersAsync(context.Activity.Conversation.Id);

                foreach (var member in members)
                {
                    email = member.Properties["email"].ToString();
                }
            }

            //var email = "bzaino@asa.org";
            //username = email.Substring(0, email.IndexOf("@"));

            return email;
        }
    }
}