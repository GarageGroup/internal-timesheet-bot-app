#nullable enable

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal static class BotAppBuilderExtensions
    {
        private static Lazy<IStorage> lazyStorage;

        private static Lazy<ConversationState> lazyConversationState;

        private static Lazy<UserState> lazyUserState;

        static BotAppBuilderExtensions()
        {
            lazyStorage = new(CreateStorage, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            lazyConversationState = new(CreateConversationState, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            lazyUserState = new(CreateUserState, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public static IApplicationBuilder UseTimesheetBot(this IApplicationBuilder app)
            =>
            app.UseBot(Resolve);

        private static IBot Resolve(IServiceProvider serviceProvider)
            =>
            TimesheetBot.Create(
                conversationState: lazyConversationState.Value,
                userState: lazyUserState.Value,
                dialog: Dependency.Create(_ => lazyUserState.Value).UseTimesheetGetDialog().Resolve(serviceProvider));

        private static ConversationState CreateConversationState()
            =>
            new(lazyStorage.Value);

        private static UserState CreateUserState()
            =>
            new(lazyStorage.Value);

        private static IStorage CreateStorage()
            =>
            new MemoryStorage();
    }
}