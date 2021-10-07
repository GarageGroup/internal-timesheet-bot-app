#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed class AdaptiveCardPrompt<T> : Prompt<T>
    {
        public AdaptiveCardPrompt(string dialogId, PromptValidator<T>? validator = null)
            : base(dialogId, validator)
        {
        }

        protected override async Task OnPromptAsync(
            ITurnContext turnContext,
            IDictionary<string, object> state,
            PromptOptions options,
            bool isRetry,
            CancellationToken cancellationToken = default)
        {
            _ = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
            _ = options ?? throw new ArgumentNullException(nameof(options));
            
            if (isRetry && options.RetryPrompt is not null)
            {
                _ = await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);  
            }
            if (options.Prompt is not null)
            {
                _ = await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);  
            } 
        }

        protected override async Task<PromptRecognizerResult<T>> OnRecognizeAsync(
            ITurnContext turnContext,
            IDictionary<string, object> state,
            PromptOptions options,
            CancellationToken cancellationToken = default)
        {
            _ = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
            var activity = turnContext.Activity ?? throw new InvalidOperationException("Activity must be not null.");

            await Task.Yield();

            if (activity.Type == ActivityTypes.Message)
            {
                var jsonValue = activity.Value.ToStringOrEmpty();
                if (string.IsNullOrEmpty(jsonValue) is false)
                {
                    return new()
                    {
                        Value = JsonConvert.DeserializeObject<T>(jsonValue),
                        Succeeded = true
                    };
                }
            }

            return new();
        }
    }
}