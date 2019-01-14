using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Jarvis.Utilities;
using System.Collections.Generic;

namespace Jarvis.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(AnswerQuestionAsync);

            return Task.CompletedTask;
        }

        private async Task AnswerQuestionAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var IntentResponses = new IntentResponses();

            await IntentResponses.HandleResponses(context, result);
        }

    }
}