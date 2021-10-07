#nullable enable

namespace GGroupp.Internal.Timesheet.Bot
{
    public static class PipelineDialog
    {
        public static PipelineDialogBuilder<T> Start<T>(string dialogId)
            where T : new()
            =>
            new(PipelineDialogBuilder.Start(dialogId));
    }
}