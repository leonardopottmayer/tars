using FluentAssertions;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Enqueue_starts_pending_and_immediately_due()
    {
        var clock = new ControllableClock(Now);

        var message = OutboxMessage.Enqueue(
            Guid.NewGuid(), "tests.thing-happened", 3, "{}", null, Now.AddMinutes(-1), clock);

        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.Attempts.Should().Be(0);
        message.NextAttemptAt.Should().Be(Now, "a fresh message is due right away");
        message.CreatedAt.Should().Be(Now);
        message.OccurredAt.Should().Be(Now.AddMinutes(-1));
        message.Version.Should().Be(3);
    }

    [Fact]
    public void MarkDispatched_is_terminal_and_stamps_processed_at()
    {
        var clock = new ControllableClock(Now);
        var message = OutboxMessage.Enqueue(Guid.NewGuid(), "tests.thing-happened", 1, "{}", null, Now, clock);

        clock.Now = Now.AddSeconds(30);
        message.MarkDispatched(clock);

        message.Status.Should().Be(OutboxMessageStatus.Dispatched);
        message.ProcessedAt.Should().Be(Now.AddSeconds(30));
        message.NextAttemptAt.Should().BeNull();
        message.Error.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_schedules_a_retry_with_backoff_while_attempts_remain()
    {
        var clock = new ControllableClock(Now);
        var message = OutboxMessage.Enqueue(Guid.NewGuid(), "tests.thing-happened", 1, "{}", null, Now, clock);

        message.MarkFailed("boom", maxAttempts: 3, backoff: _ => TimeSpan.FromSeconds(10), clock);

        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.Attempts.Should().Be(1);
        message.NextAttemptAt.Should().Be(Now.AddSeconds(10));
        message.Error.Should().Be("boom");
    }

    [Fact]
    public void MarkFailed_dead_letters_once_the_attempt_budget_is_spent()
    {
        var clock = new ControllableClock(Now);
        var message = OutboxMessage.Enqueue(Guid.NewGuid(), "tests.thing-happened", 1, "{}", null, Now, clock);

        message.MarkFailed("1", maxAttempts: 2, backoff: _ => TimeSpan.FromSeconds(10), clock);
        message.MarkFailed("2", maxAttempts: 2, backoff: _ => TimeSpan.FromSeconds(10), clock);

        message.Status.Should().Be(OutboxMessageStatus.Dead);
        message.Attempts.Should().Be(2);
        message.NextAttemptAt.Should().BeNull("a dead message is never retried");
        message.Error.Should().Be("2");
    }
}
