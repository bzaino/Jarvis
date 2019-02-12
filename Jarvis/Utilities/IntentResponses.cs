using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Jarvis.Utilities;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using AdaptiveCards;
using Jarvis.Models;
using Newtonsoft.Json;
using System.IO;

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

            var chatAnswer = new List<AnswerModel>();

            var intent = await Utility.GetLuisIntent(activity.Text);

            //get LUIS intent name
            var _intentName = Utility.GetHighScoringIntent(intent);

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
                    //await context.PostAsync("This feature is coming soon.");
                    break;

                case nameof(Utility.Intent.FindLunchPlaces):
                    await HandleLunchIntent(context, intent);
                    break;

                case nameof(Utility.Intent.FireDrillLocation):
                    await HandleFireDrillIntent(context);
                    break;

                default:
                    //get answers based on the text entered
                    chatAnswer = await Utility.GetAndFormatAnswer(intent);
                    await DisplayAnswersAsync(context, chatAnswer);
                    context.Wait(HandleResponses);
                    break;
            }

        }

        private async Task HandleFireDrillIntent(IDialogContext context)
        {
            var image = new byte[] { };
            var fileName = "FireDrillLocation.png";

            //If image image is not in the cloud, get it and save it for future use. Otherwise, pull it down from cloud
            if (!Utility.CheckCloudFileExists(fileName))
            {
                image = await Utility.GetImageByUrl(string.Format(Utility.fireDrillUriBase, Utility.bingMapsKey));

                await Utility.UploadFileToCloud(fileName, image);
            }
            else
            {
                image = Utility.GetImageFromDatastore(fileName);
            }

            await context.PostAsync("At 33 Arch St., our meeting location is in Downtown Crossing, in between the Millenium Tower and TJ Maxx.");
            await context.PostAsync("The image below shows it on a map.");
            await DisplayImage(context, image);

        }

        private async Task HandleLunchIntent(IDialogContext context, LuisResponse intent)
        {
            if (intent.entities.Length > 0)
            {
                var lunchType = string.Empty;
                {
                    lunchType = intent.entities[0].entity;
                }

                await context.PostAsync(Utility.GetGoogleSearchMessage(lunchType));
            }
            else
            {
                PromptDialog.Text(
                    context: context,
                    resume: GetUserLunchChoice,
                    prompt: "What do you want to eat today?",
                    retry: "Sorry, I didn't understand that. Please try again."
                );
            }
        }

        private async Task GetUserLunchChoice(IDialogContext context, IAwaitable<string> result)
        {
            var lunchType = await result;
            await context.PostAsync($"I see you want to get {lunchType}.");

            await context.PostAsync(Utility.GetGoogleSearchMessage(lunchType));
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
                var ccEmail = await GetEmail(context);

                var member = await GetMember(context);

                await Utility.SendEmail(Utility.debugMode == "true" ? "bzaino@asa.org" : "jiraservicedesk@asa.org", $"Chatbot Ticket for {member.Name}", ticketDescription, "mlemieux@asa.org");
                
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

            var intent = await Utility.GetLuisIntent("Traffic");

            var chatAnswer = Utility.GetAndFormatAnswer(intent).Result;
            chatAnswer[0].AnswerText += $"there is {driveTime} minutes.";

            await DisplayAnswersAsync(context, chatAnswer);

        }

        private async Task DisplayAnswersAsync(IDialogContext context, List<AnswerModel> chatAnswer)
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
            IMessageActivity message = null;
            var card = await Weather.GetCard(location, showWeekendInfo);

            if (card == null)
            {
                message = context.MakeMessage();
                message.Text = $"I couldn't find the weather for '{context.Activity.AsMessageActivity().Text}'.  Are you sure that's a real city?";
            }
            else
            {
                message = GetWeatherMessage(context, card, "Weather card");
            }

            await context.PostAsync(message);
        }

        /// <summary>
        /// Display card with weather info on it
        /// </summary>
        /// <param name="context"></param>
        /// <param name="card"></param>
        /// <param name="cardName"></param>
        /// <returns></returns>
        private IMessageActivity GetWeatherMessage(IDialogContext context, AdaptiveCard card, string cardName)
        {
            var message = context.MakeMessage();
            if (message.Attachments == null)
            {
                message.Attachments = new List<Microsoft.Bot.Connector.Attachment>();
            }
            var attachment = new Microsoft.Bot.Connector.Attachment()
            {
                Content = card,
                ContentType = AdaptiveCard.ContentType,
                Name = cardName
            };

            message.Attachments.Add(attachment);

            return message;
        }

        private async Task<string> GetEmail(IDialogContext context)
        {
            //default username
            var email = "bzaino@asa.org";

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

            return email;
        }

        private async Task<ChannelAccount> GetMember(IDialogContext context)
        {
            //default username
            var member = new ChannelAccount();
            member.Name = "Bart Zaino";

            //within Teams, we have access to the user's email address, since it is linked to Microsoft. Other channels do not have this detail, so we will create the ticket under Jarvis
            if (context.Activity.ChannelId == "msteams")
            {
                var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
                var members = await connector.Conversations.GetConversationMembersAsync(context.Activity.Conversation.Id);

                if (members.Count > 0)
                {
                    member = members[0];
                }
            }

            return member;
        }

        /// <summary>
        /// Display image as an attachment
        /// </summary>
        /// <param name="context"></param>
        /// <param name="imageBytes"></param>
        /// <returns></returns>
        private static async Task DisplayImage(IDialogContext context, byte[] imageBytes)
        {
            try
            {
                var message = context.MakeMessage();

                string url = "data:image/png;base64," + Convert.ToBase64String(imageBytes);
                message.Attachments.Add(new Microsoft.Bot.Connector.Attachment()
                {
                    ContentUrl = url, ContentType = "image/png"
                });

                await context.PostAsync(message);
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}