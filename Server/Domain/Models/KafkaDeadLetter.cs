// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessService.Domain.Models;

/// <summary>
/// Represents a Kafka message that failed processing after all retry attempts.
/// Persisted for manual investigation and replay.
/// </summary>
[Table("kafka_dead_letter")]
public class KafkaDeadLetter
{
    [Key]
    [Column("id")]
    public Guid Id
    {
        get; set;
    }

    [Required]
    [MaxLength(500)]
    [Column("topic")]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [Column("partition")]
    public int Partition
    {
        get; set;
    }

    [Required]
    [Column("offset")]
    public long Offset
    {
        get; set;
    }

    [MaxLength(1000)]
    [Column("key")]
    public string? Key
    {
        get; set;
    }

    [Required]
    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("consumer_name")]
    public string ConsumerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    [Column("error_message")]
    public string ErrorMessage { get; set; } = string.Empty;

    [Required]
    [Column("failed_at")]
    public DateTimeOffset FailedAt
    {
        get; set;
    }

    [Required]
    [Column("retry_count")]
    public int RetryCount
    {
        get; set;
    }
}
