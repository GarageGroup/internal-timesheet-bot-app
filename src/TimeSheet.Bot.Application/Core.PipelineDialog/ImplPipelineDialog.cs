#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed class ImplPipelineDialog : ComponentDialog
    {
        internal ImplPipelineDialog(string dialogId, IReadOnlyCollection<Dialog> dialogs)
            : base(dialogId)
        {
            foreach(var dialog in dialogs)
            {
                AddDialog(dialog);
            }

            InitialDialogId = dialogs.First().Id;
        }
    }
}