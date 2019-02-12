using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Jarvis.Utilities;
using Microsoft.Bot.Builder.Luis;
using System.Configuration;

namespace Jarvis
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            var userLogId = 0;

            if (activity.Type == ActivityTypes.Message)
            {
                try
                {
                    userLogId = await DataAccess.LogActivity(activity);

                    await TypingActivityAsync(activity);
                    await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
                }
                catch (Exception ex)
                {
                    //log error to DB
                    await DataAccess.LogError(userLogId, activity.Id, ex.Message);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        public static async Task TypingActivityAsync(Activity activity)
        {
            ConnectorClient connector;
            if (Utility.debugMode == "false")
            {
                //Prod version uses MS Bot ID/pwd for authorization
                MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl, DateTime.Now.AddDays(7));
                var account = new MicrosoftAppCredentials(Utility.msAppId, Utility.msAppPwd);
                connector = new ConnectorClient(new Uri(activity.ServiceUrl), account);
            }
            else
            {
                connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            }

            var isTypingReply = activity.CreateReply();
            isTypingReply.Type = ActivityTypes.Typing;
            await connector.Conversations.ReplyToActivityAsync(isTypingReply);
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
        

            return null;
        }
    }
}