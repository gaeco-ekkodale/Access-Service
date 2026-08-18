// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Events;
using AccessService.Api.Options;
using AccessService.Api.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AccessService.Api.Consumer;

/// <summary>
/// Kafka consumer that processes guideline events from the Guidelines topic.
/// Routes to the appropriate handler based on the <c>event_type</c> header:
/// <c>UploadedGuideline</c> triggers a full relational transformation and upsert,
/// <c>DeletedGuideline</c> removes the existing projection and regenerates UseCase-Guidelines.
/// </summary>
public class GuidelineConsumer : KafkaConsumer<UploadedGuideline>
{
    private readonly ILogger<GuidelineConsumer> _logger;

    public GuidelineConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<GuidelineConsumer> logger)
        : base(scopeFactory, kafkaOptions, logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override string Topic => KafkaOptions.Topics.Guidelines;

    /// <inheritdoc />
    protected override string ConsumerName => "Guideline consumer";

    /// <inheritdoc />
    protected override async Task ProcessMessageAsync(
        UploadedGuideline message,
        string rawJson,
        IReadOnlyDictionary<string, string> headers,
        IServiceScope scope,
        CancellationToken stoppingToken)
    {
        headers.TryGetValue("event_type", out var eventType);

        var service = scope.ServiceProvider.GetRequiredService<IGuidelineTransformationService>();

        switch (eventType)
        {
            case "UploadedGuideline":
                await service.ProcessAsync(message, stoppingToken);
                _logger.LogInformation(
                    "Successfully processed UploadedGuideline event. ObjectName={ObjectName}, Etag={Etag}, CorrelationId={CorrelationId}",
                    message.Name, message.Etag, message.CorrelationId);
                break;

            case "DeletedGuideline":
                var deletedGuideline = JsonSerializer.Deserialize<DeletedGuideline>(rawJson)
                    ?? throw new InvalidOperationException("Failed to deserialize DeletedGuideline payload.");
                await service.DeleteAsync(deletedGuideline, stoppingToken);
                _logger.LogInformation(
                    "Successfully processed DeletedGuideline event. Id={Id}, ObjectKey={ObjectKey}",
                    deletedGuideline.Id, deletedGuideline.ObjectKey);
                break;

            default:
                _logger.LogWarning(
                    "Unknown or missing event_type header '{EventType}' on Guidelines topic. Message will be skipped.",
                    eventType);
                break;
        }
    }
}
