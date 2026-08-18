// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Options;
using AccessService.Domain.Models;
using AccessService.Infrastructure;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AccessService.Api.Consumer;

/// <summary>
/// Base background service that consumes messages of type <typeparamref name="TMessage"/>
/// from a Kafka topic and delegates processing to a derived class.
/// </summary>
/// <typeparam name="TMessage">The message type to deserialize from Kafka.</typeparam>
public abstract class KafkaConsumer<TMessage> : BackgroundService where TMessage : class
{
    private const int MaxRetries = 5;
    private const int BackoffBaseMs = 1000;
    private const int MaxBackoffMs = 5000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<(int Partition, long Offset), int> _retryCounts = new();

    /// <summary>
    /// Gets the Kafka options for derived classes to access topic configuration etc.
    /// </summary>
    protected KafkaOptions KafkaOptions
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumer{TMessage}"/> class.
    /// </summary>
    protected KafkaConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        KafkaOptions = kafkaOptions.Value;
    }

    /// <summary>
    /// Gets the Kafka topic to consume from.
    /// </summary>
    protected abstract string Topic
    {
        get;
    }

    /// <summary>
    /// Gets a display name for this consumer, used in log messages.
    /// </summary>
    protected abstract string ConsumerName
    {
        get;
    }

    /// <summary>
    /// Processes a single deserialized message within the given service scope.
    /// </summary>
    /// <param name="message">The deserialized message. For multi-type topics, may be partially populated — use <paramref name="rawJson"/> to re-deserialize if needed.</param>
    /// <param name="rawJson">The raw JSON string from the Kafka message, for consumers that handle multiple event types.</param>
    /// <param name="headers">Message headers, e.g. <c>event_type</c> set by the producer outbox.</param>
    /// <param name="scope">The service scope for resolving scoped dependencies.</param>
    /// <param name="stoppingToken">A cancellation token.</param>
    protected abstract Task ProcessMessageAsync(TMessage message, string rawJson, IReadOnlyDictionary<string, string> headers, IServiceScope scope, CancellationToken stoppingToken);

    /// <summary>
    /// Called after a message has been successfully processed and committed.
    /// Override to add post-processing logging or metrics.
    /// </summary>
    protected virtual void OnMessageProcessed(TMessage message)
    {
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ConsumerName} starting. Topic={Topic}, ConsumerGroup={ConsumerGroup}",
            ConsumerName, Topic, KafkaOptions.ConsumerGroup);

        // Run the blocking Kafka consume loop on a background thread
        // so we don't block the host startup pipeline.
        await Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);
    }

    /// <summary>
    /// Creates the Kafka consumer. Override in tests to inject a mock consumer.
    /// </summary>
    protected virtual IConsumer<string, string> BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaOptions.Address,
            GroupId = KafkaOptions.ConsumerGroup,
            AutoOffsetReset = KafkaOptions.AutoOffsetReset,
            EnableAutoCommit = false,
            SessionTimeoutMs = KafkaOptions.SessionTimeoutMs,
            MaxPollIntervalMs = KafkaOptions.MaxPollIntervalMs
        };

        if (!string.IsNullOrEmpty(KafkaOptions.Username) && !string.IsNullOrEmpty(KafkaOptions.Password))
        {
            config.SaslMechanism = SaslMechanism.Plain;
            config.SecurityProtocol = SecurityProtocol.SaslSsl;
            config.SaslUsername = KafkaOptions.Username;
            config.SaslPassword = KafkaOptions.Password;
        }

        return new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();
    }

    private static IReadOnlyDictionary<string, string> ParseHeaders(Headers? headers)
    {
        if (headers is null || headers.Count == 0)
            return new Dictionary<string, string>(0);

        var result = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
            result[header.Key] = System.Text.Encoding.UTF8.GetString(header.GetValueBytes());
        return result;
    }

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        using var consumer = BuildConsumer();

        consumer.Subscribe(Topic);
        _logger.LogInformation("Subscribed to Kafka topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? consumeResult = null;
            try
            {
                consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult?.Message?.Value is null)
                {
                    continue;
                }

                _logger.LogDebug("Received message from {Topic} partition {Partition} offset {Offset}",
                    consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

                // Kafka event payloads use System.Text.Json (simple DTOs, no polymorphism needed).
                // The guideline file the event points to is read by GuidelineReaderWriter, which owns its schema.
                var rawJson = consumeResult.Message.Value;
                var message = JsonSerializer.Deserialize<TMessage>(rawJson);

                if (message is null)
                {
                    _logger.LogWarning("Failed to deserialize {MessageType} from topic {Topic}. Skipping message at offset {Offset}.",
                        typeof(TMessage).Name, consumeResult.Topic, consumeResult.Offset.Value);
                    consumer.Commit(consumeResult);
                    continue;
                }

                var headers = ParseHeaders(consumeResult.Message.Headers);

                // Process in a scoped service to get a fresh DbContext per message
                using var scope = _scopeFactory.CreateScope();
                await ProcessMessageAsync(message, rawJson, headers, scope, stoppingToken);

                // Commit only after successful processing
                consumer.Commit(consumeResult);
                CleanupRetryCount(consumeResult.Partition.Value, consumeResult.Offset.Value);

                OnMessageProcessed(message);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error on topic {Topic}", Topic);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing {MessageType} from topic {Topic}.",
                    typeof(TMessage).Name, Topic);

                if (consumeResult is not null)
                {
                    await HandleRetryAsync(consumer, consumeResult, ex, stoppingToken);
                }
            }
        }

        consumer.Close();
        _logger.LogInformation("{ConsumerName} stopped.", ConsumerName);
    }

    private void CleanupRetryCount(int partition, long committedOffset)
    {
        // Remove retry counts for this offset and any stale earlier offsets on the same partition
        foreach (var key in _retryCounts.Keys)
        {
            if (key.Partition == partition && key.Offset <= committedOffset)
            {
                _retryCounts.TryRemove(key, out _);
            }
        }
    }

    private async Task HandleRetryAsync(IConsumer<string, string> consumer, ConsumeResult<string, string> consumeResult, Exception lastException, CancellationToken stoppingToken)
    {
        var key = (consumeResult.Partition.Value, consumeResult.Offset.Value);
        var retryCount = _retryCounts.AddOrUpdate(key, 1, (_, count) => count + 1);

        if (retryCount >= MaxRetries)
        {
            _logger.LogError(
                "Poison message at partition {Partition} offset {Offset} on topic {Topic} failed {RetryCount} times. " +
                "Persisting to dead-letter table and committing offset to skip.",
                consumeResult.Partition.Value, consumeResult.Offset.Value, consumeResult.Topic, retryCount);

            await PersistDeadLetterAsync(consumeResult, retryCount, lastException);

            consumer.Commit(consumeResult);
            CleanupRetryCount(consumeResult.Partition.Value, consumeResult.Offset.Value);
        }
        else
        {
            // Cap backoff to avoid exceeding MaxPollIntervalMs and triggering a consumer group rebalance.
            var backoffMs = Math.Min(retryCount * BackoffBaseMs, MaxBackoffMs);
            _logger.LogWarning(
                "Retry {RetryCount}/{MaxRetries} for message at partition {Partition} offset {Offset} on topic {Topic}. " +
                "Backing off {BackoffMs}ms before retry.",
                retryCount, MaxRetries, consumeResult.Partition.Value, consumeResult.Offset.Value, consumeResult.Topic, backoffMs);

            // Seek back to the failed offset so the next Consume() call re-delivers this message.
            // Seek first, then pause to call Consume() periodically to keep the consumer alive.
            consumer.Seek(consumeResult.TopicPartitionOffset);
            await BackoffWithHeartbeatAsync(consumer, backoffMs, stoppingToken);
        }
    }

    /// <summary>
    /// Performs a backoff while periodically calling Consume to maintain the Kafka heartbeat
    /// and prevent a consumer group rebalance caused by exceeding MaxPollIntervalMs.
    /// </summary>
    private async Task BackoffWithHeartbeatAsync(IConsumer<string, string> consumer, int totalBackoffMs, CancellationToken stoppingToken)
    {
        const int heartbeatIntervalMs = 500;
        var elapsed = 0;
        while (elapsed < totalBackoffMs && !stoppingToken.IsCancellationRequested)
        {
            var waitMs = Math.Min(heartbeatIntervalMs, totalBackoffMs - elapsed);
            await Task.Delay(waitMs, stoppingToken);
            elapsed += waitMs;

            // Call Consume with a zero timeout to trigger internal heartbeat/poll bookkeeping
            // without actually waiting for a new message.
            try
            {
                consumer.Consume(TimeSpan.Zero);
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning(ex,
                    "Kafka heartbeat poll failed during backoff on topic {Topic}. Continuing retry backoff.",
                    Topic);
            }
        }
    }

    private async Task PersistDeadLetterAsync(ConsumeResult<string, string> consumeResult, int retryCount, Exception lastException)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AccessRightDbContext>();

            var deadLetter = new KafkaDeadLetter
            {
                Id = Guid.NewGuid(),
                Topic = consumeResult.Topic,
                Partition = consumeResult.Partition.Value,
                Offset = consumeResult.Offset.Value,
                Key = consumeResult.Message?.Key,
                Value = consumeResult.Message?.Value ?? string.Empty,
                ConsumerName = ConsumerName,
                ErrorMessage = TruncateErrorMessage(lastException),
                FailedAt = DateTimeOffset.UtcNow,
                RetryCount = retryCount
            };

            dbContext.KafkaDeadLetters.Add(deadLetter);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Dead letter persisted: Id={DeadLetterId}, Topic={Topic}, Partition={Partition}, Offset={Offset}",
                deadLetter.Id, deadLetter.Topic, deadLetter.Partition, deadLetter.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "FAILED to persist dead letter for topic {Topic} partition {Partition} offset {Offset}. " +
                "Message data may be lost. Manual intervention required.",
                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
        }
    }

    private static string TruncateErrorMessage(Exception exception, int maxLength = 2000)
    {
        var errorText = exception.ToString();
        return errorText[..Math.Min(errorText.Length, maxLength)];
    }
}
