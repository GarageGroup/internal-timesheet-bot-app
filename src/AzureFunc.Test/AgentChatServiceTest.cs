using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentChatServiceTest
{
    private static readonly AgentUserContext User = new(TelegramUserId: 15, TelegramChatId: 27);

    private static readonly AgentMessageOption Option = new(TimeZoneInfo.Utc, MaxTextLength: 20, MaxHistoryMessageCount: 4);

    [Fact]
    public static async Task SendAsync_ModelReturnsText_ExpectReplyAndSavedTurn()
    {
        var chatCompletion = new FakeChatCompletionService("  <b>8 ч</b>  ");
        var historyStore = new FakeHistoryStore([new(true, "привет"), new(false, "здравствуйте")]);
        var service = CreateService(chatCompletion, historyStore);

        var actual = await service.SendAsync(new(User, " сколько сегодня? ", "ru"), TestContext.Current.CancellationToken);

        Assert.Equal("<b>8 ч</b>", actual.Text);

        var history = Assert.Single(chatCompletion.Histories);
        Assert.Equal(
            [
                (AuthorRole.System, "date=2026-11-27 day=Friday zone=UTC locale=ru language=русский"),
                (AuthorRole.User, "привет"),
                (AuthorRole.Assistant, "здравствуйте"),
                (AuthorRole.User, "сколько сегодня?")
            ],
            history);

        Assert.Equal([(User, "сколько сегодня?", "<b>8 ч</b>", 4)], historyStore.Appended);
        Assert.Equal(4, historyStore.RequestedMaxCount);
    }

    [Fact]
    public static async Task SendAsync_MessageIsTooLong_ExpectMessageTooLongFailureAndModelIsNotCalled()
    {
        var chatCompletion = new FakeChatCompletionService("ok");
        var service = CreateService(chatCompletion, new FakeHistoryStore([]));

        var exception = await Assert.ThrowsAsync<AgentChatException>(
            async () => await service.SendAsync(new(User, new string('a', 21), "ru"), TestContext.Current.CancellationToken));

        Assert.Equal(AgentChatFailureCode.MessageTooLong, exception.FailureCode);
        Assert.Empty(chatCompletion.Histories);
    }

    [Fact]
    public static async Task SendAsync_ModelReturnsEmptyText_ExpectEmptyResponseFailureAndTurnIsNotSaved()
    {
        var historyStore = new FakeHistoryStore([]);
        var service = CreateService(new FakeChatCompletionService("   "), historyStore);

        var exception = await Assert.ThrowsAsync<AgentChatException>(
            async () => await service.SendAsync(new(User, "привет", "ru"), TestContext.Current.CancellationToken));

        Assert.Equal(AgentChatFailureCode.EmptyResponse, exception.FailureCode);
        Assert.Empty(historyStore.Appended);
    }

    [Fact]
    public static async Task SendAsync_ModelFails_ExpectUnknownFailureAndTurnIsNotSaved()
    {
        var historyStore = new FakeHistoryStore([]);
        var service = CreateService(new FakeChatCompletionService(new InvalidOperationException("model failure")), historyStore);

        var exception = await Assert.ThrowsAsync<AgentChatException>(
            async () => await service.SendAsync(new(User, "привет", "ru"), TestContext.Current.CancellationToken));

        Assert.Equal(AgentChatFailureCode.Unknown, exception.FailureCode);
        Assert.Empty(historyStore.Appended);
    }

    [Fact]
    public static async Task SendAsync_UserIsNotSignedIn_ExpectNotSignedInExceptionAndModelIsNotCalled()
    {
        var chatCompletion = new FakeChatCompletionService("ok");
        var service = CreateService(
            chatCompletion, new FakeHistoryStore([]), new FakeToolLoader(new AgentUserNotSignedInException("api://timesheet")));

        _ = await Assert.ThrowsAsync<AgentUserNotSignedInException>(
            async () => await service.SendAsync(new(User, "привет", "ru"), TestContext.Current.CancellationToken));

        Assert.Empty(chatCompletion.Histories);
    }

    private static AgentChatService CreateService(
        FakeChatCompletionService chatCompletion, FakeHistoryStore historyStore, FakeToolLoader? toolLoader = null)
        =>
        new(
            new FakeKernelFactory(chatCompletion),
            toolLoader ?? new FakeToolLoader(null),
            historyStore,
            new InMemoryAgentActionStore(),
            new AgentEntityCache(),
            CreatePromptProvider(),
            new(Option, new HashSet<string>(), TimeSpan.FromMinutes(10)),
            TimeProvider.System,
            NullLogger.Instance);

    private static AgentPromptProvider CreatePromptProvider()
        =>
        new(
            "date={{Today}} day={{DayOfWeek}} zone={{TimeZone}} locale={{Locale}} language={{Language}}",
            TimeZoneInfo.Utc,
            new FixedTimeProvider(new(2026, 11, 27, 16, 30, 0, TimeSpan.Zero)));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            =>
            now;
    }

    private sealed class FakeKernelFactory(IChatCompletionService chatCompletion) : IAgentKernelFactory
    {
        public Kernel Create()
        {
            var builder = Kernel.CreateBuilder();
            _ = builder.Services.AddSingleton(chatCompletion);

            return builder.Build();
        }
    }

    private sealed class FakeToolLoader(Exception? failure) : IAgentToolLoader
    {
        public ValueTask<AgentToolSet> LoadAsync(AgentUserContext user, CancellationToken cancellationToken)
            =>
            failure is null ? new(new AgentToolSet([], [])) : throw failure;
    }

    private sealed class FakeHistoryStore(IReadOnlyList<AgentHistoryMessage> messages) : IAgentHistoryStore
    {
        public List<(AgentUserContext User, string UserText, string AssistantText, int KeepCount)> Appended { get; } = [];

        public int RequestedMaxCount { get; private set; }

        public ValueTask<IReadOnlyList<AgentHistoryMessage>> GetAsync(
            AgentUserContext user, int maxCount, CancellationToken cancellationToken)
        {
            RequestedMaxCount = maxCount;
            return new(messages);
        }

        public ValueTask AppendAsync(
            AgentUserContext user, string userText, string assistantText, int keepCount, CancellationToken cancellationToken)
        {
            Appended.Add((user, userText, assistantText, keepCount));
            return default;
        }
    }

    private sealed class FakeChatCompletionService : IChatCompletionService
    {
        private readonly string? content;

        private readonly Exception? failure;

        internal FakeChatCompletionService(string content)
            =>
            this.content = content;

        internal FakeChatCompletionService(Exception failure)
            =>
            this.failure = failure;

        public List<(AuthorRole Role, string? Content)[]> Histories { get; } = [];

        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            Histories.Add([.. chatHistory.Select(static message => (message.Role, message.Content))]);

            return failure is null
                ? Task.FromResult<IReadOnlyList<ChatMessageContent>>([new(AuthorRole.Assistant, content)])
                : Task.FromException<IReadOnlyList<ChatMessageContent>>(failure);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
            =>
            throw new NotSupportedException();
    }
}
