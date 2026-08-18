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
using FluentValidation;

namespace AccessService.Api.Validators;

/// <summary>
/// Represents a validator for the <see cref="KafkaOptions"/>.
/// </summary>
public class KafkaOptionsValidator : AbstractValidator<KafkaOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaOptionsValidator"/> class
    /// and sets up the validation rules.
    /// </summary>
    public KafkaOptionsValidator()
    {
        RuleFor(x => x.Address).NotEmpty().WithMessage("Kafka Address is required");
        RuleFor(x => x.ConsumerGroup).NotEmpty().WithMessage("Kafka ConsumerGroup is required");
        RuleFor(x => x.Topics).NotNull().WithMessage("Kafka Topics section is required").ChildRules(topics =>
        {
            topics.RuleFor(t => t.AccessRights).NotEmpty().WithMessage("Kafka AccessRightsTopic is required");
            topics.RuleFor(t => t.UserGroups).NotEmpty().WithMessage("Kafka UserGroupsTopic is required");
            topics.RuleFor(t => t.Guidelines).NotEmpty().WithMessage("Kafka GuidelinesTopic is required");
            topics.RuleFor(t => t.UseCaseGuidelines).NotEmpty().WithMessage("Kafka UseCaseGuidelinesTopic is required");
        });
    }
}
