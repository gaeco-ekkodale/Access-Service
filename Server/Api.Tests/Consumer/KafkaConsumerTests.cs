// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Consumer;
using AccessService.Api.Options;
using AccessService.Domain.Models;
using AccessService.Infrastructure;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AccessService.Api.Tests.Consumer;

/// <summary>
/// Simple DTO used as the message type in tests.
/// </summary>
public class TestMessage
{
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Concrete test consumer that overrides <see cref="KafkaConsumer{TMessage}.BuildConsumer"/>
/// to inject a mock <see cref="IConsumer{TKey,TValue}"/> and exposes hooks for assertions.
/// </summary>
public class TestableKafkaConsumer : KafkaConsumer<TestMessage>
{
    private readonly IConsumer<string, string> _mockConsumer;
    private readonly Func<TestMessage, IServiceScope, CancellationToken, Task>? _processAction;

    public List<TestMessage> ProcessedMessages { get; } = [];

    public TestableKafkaConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<TestableKafkaConsumer> logger,
        IConsumer<string, string> mockConsumer,
        Func<TestMessage, IServiceScope, CancellationToken, Task>? processAction = null)
        : base(scopeFactory, kafkaOptions, logger)
    {
        _mockConsumer = mockConsumer;
        _processAction = processAction;
    }

    protected override string Topic => "test-topic";
    protected override string ConsumerName => "Test consumer";

    protected override IConsumer<string, string> BuildConsumer() => _mockConsumer;

    protected override async Task ProcessMessageAsync(TestMessage message, string rawJson, IReadOnlyDictionary<string, string> headers, IServiceScope scope, CancellationToken stoppingToken)
    {
        if (_processAction is not null)
        {
            await _processAction(message, scope, stoppingToken);
        }
        ProcessedMessages.Add(message);
    }
}

public class KafkaConsumerTests : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _serviceScope;
    private readonly ILogger<TestableKafkaConsumer> _logger;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly IConsumer<string, string> _mockConsumer;
    private readonly AccessRightDbContext _dbContext;

    public KafkaConsumerTests()
    {
        _logger = Substitute.For<ILogger<TestableKafkaConsumer>>();
        _mockConsumer = Substitute.For<IConsumer<string, string>>();

        // In-memory DB for dead-letter persistence tests
        var dbOptions = new DbContextOptionsBuilder<AccessRightDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AccessRightDbContext(dbOptions);

        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(AccessRightDbContext)).Returns(_dbContext);
        _serviceScope = Substitute.For<IServiceScope>();
        _serviceScope.ServiceProvider.Returns(_serviceProvider);
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(_serviceScope);

        _kafkaOptions = Microsoft.Extensions.Options.Options.Create(new KafkaOptions
        {
            Address = "localhost:9092",
            ConsumerGroup = "test-group",
            Topics = new KafkaTopicsOptions
            {
                AccessRights = "access-rights",
                UserGroups = "user-groups",
                Guidelines = "guidelines",
                UseCaseGuidelines = "usecase-guidelines"
            }
        });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private ConsumeResult<string, string> CreateConsumeResult(TestMessage message, int partition = 0, long offset = 0)
    {
        return new ConsumeResult<string, string>
        {
            Topic = "test-topic",
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, string>
            {
                Key = "test-key",
                Value = JsonSerializer.Serialize(message)
            }
        };
    }

    [Fact]
    public async Task SuccessfulProcessing_CommitsOffset()
    {
        // Arrange
        var message = new TestMessage { Data = "hello" };
        var consumeResult = CreateConsumeResult(message);
        var cts = new CancellationTokenSource();
        var callCount = 0;

        _mockConsumer.Consume(Arg.Any<CancellationToken>()).Returns(ci =>
        {
            callCount++;
            if (callCount > 1)
                cts.Cancel();
            if (callCount == 1)
                return consumeResult;
            throw new OperationCanceledException();
        });

        var consumer = new TestableKafkaConsumer(_scopeFactory, _kafkaOptions, _logger, _mockConsumer);

        // Act
        await consumer.StartAsync(cts.Token);
        // Give the background task time to process
        await Task.Delay(200);
        await consumer.StopAsync(CancellationToken.None);

        // Assert
        Assert.Single(consumer.ProcessedMessages);
        Assert.Equal("hello", consumer.ProcessedMessages[0].Data);
        _mockConsumer.Received().Commit(consumeResult);
    }

    [Fact]
    public async Task NullDeserializationResult_SkipsAndCommits()
    {
        // Arrange — "null" is valid JSON that deserializes to null for reference types
        var nullJsonResult = new ConsumeResult<string, string>
        {
            Topic = "test-topic",
            Partition = new Partition(0),
            Offset = new Offset(0),
            Message = new Message<string, string>
            {
                Key = "key",
                Value = "null"
            }
        };

        var cts = new CancellationTokenSource();
        var callCount = 0;

        _mockConsumer.Consume(Arg.Any<CancellationToken>()).Returns(ci =>
        {
            callCount++;
            if (callCount > 1)
                cts.Cancel();
            if (callCount == 1)
                return nullJsonResult;
            throw new OperationCanceledException();
        });

        var consumer = new TestableKafkaConsumer(_scopeFactory, _kafkaOptions, _logger, _mockConsumer);

        // Act
        await consumer.StartAsync(cts.Token);
        await Task.Delay(200);
        await consumer.StopAsync(CancellationToken.None);

        // Assert - message was skipped but offset was committed
        Assert.Empty(consumer.ProcessedMessages);
        _mockConsumer.Received().Commit(nullJsonResult);
    }

    [Fact]
    public async Task ProcessingFailure_RetriesUpToMaxThenPersistsDeadLetter()
    {
        // Arrange
        var message = new TestMessage { Data = "poison" };
        var consumeResult = CreateConsumeResult(message);
        var cts = new CancellationTokenSource();
        var callCount = 0;

        // Return the same message repeatedly (simulating seek-back behavior)
        _mockConsumer.Consume(Arg.Any<CancellationToken>()).Returns(ci =>
        {
            callCount++;
            if (callCount > 6)
            {
                cts.Cancel();
                throw new OperationCanceledException();
            }
            return consumeResult;
        });

        // Heartbeat polls can also fail during backoff and should be logged without aborting retries.
        _mockConsumer.Consume(Arg.Any<TimeSpan>()).Returns(ci =>
        {
            throw new ConsumeException(
                new ConsumeResult<byte[], byte[]>(),
                new Error(ErrorCode.Local_AllBrokersDown, "heartbeat failed"));
        });

        var processCallCount = 0;
        var consumer = new TestableKafkaConsumer(
            _scopeFactory, _kafkaOptions, _logger, _mockConsumer,
            processAction: (_, _, _) =>
            {
                processCallCount++;
                throw new InvalidOperationException("Processing failed");
            });

        // Act
        await consumer.StartAsync(cts.Token);
        // Allow enough time for retries + backoff
        await Task.Delay(20_000);
        await consumer.StopAsync(CancellationToken.None);

        // Assert - dead letter should be persisted after MaxRetries (5) failures
        var deadLetters = await _dbContext.KafkaDeadLetters.ToListAsync();
        Assert.Single(deadLetters);
        Assert.Equal("test-topic", deadLetters[0].Topic);
        Assert.Equal("Test consumer", deadLetters[0].ConsumerName);
        Assert.Equal(5, deadLetters[0].RetryCount);
        _logger.ReceivedWithAnyArgs().LogWarning(Arg.Any<Exception>(), default!);
    }

    [Fact]
    public async Task Cancellation_ShutdownGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        _mockConsumer.Consume(Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var token = ci.Arg<CancellationToken>();
            token.ThrowIfCancellationRequested();
            throw new OperationCanceledException();
        });

        var consumer = new TestableKafkaConsumer(_scopeFactory, _kafkaOptions, _logger, _mockConsumer);

        // Act
        await consumer.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await consumer.StopAsync(CancellationToken.None);

        // Assert - consumer was closed gracefully
        _mockConsumer.Received().Close();
        Assert.Empty(consumer.ProcessedMessages);
    }

    [Fact]
    public async Task NullMessageValue_IsSkipped()
    {
        // Arrange
        var nullResult = new ConsumeResult<string, string>
        {
            Topic = "test-topic",
            Partition = new Partition(0),
            Offset = new Offset(0),
            Message = new Message<string, string>
            {
                Key = "key",
                Value = null!
            }
        };

        var cts = new CancellationTokenSource();
        var callCount = 0;

        _mockConsumer.Consume(Arg.Any<CancellationToken>()).Returns(ci =>
        {
            callCount++;
            if (callCount > 1)
                cts.Cancel();
            if (callCount == 1)
                return nullResult;
            throw new OperationCanceledException();
        });

        var consumer = new TestableKafkaConsumer(_scopeFactory, _kafkaOptions, _logger, _mockConsumer);

        // Act
        await consumer.StartAsync(cts.Token);
        await Task.Delay(200);
        await consumer.StopAsync(CancellationToken.None);

        // Assert - null message is skipped, not committed
        Assert.Empty(consumer.ProcessedMessages);
        _mockConsumer.DidNotReceive().Commit(Arg.Any<ConsumeResult<string, string>>());
    }
}
