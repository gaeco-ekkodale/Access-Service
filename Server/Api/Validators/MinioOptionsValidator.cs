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
/// Represents a validator for the <see cref="MinioOptions"/>.
/// </summary>
public class MinioOptionsValidator : AbstractValidator<MinioOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinioOptionsValidator"/> class
    /// and sets up the validation rules.
    /// </summary>
    public MinioOptionsValidator()
    {
        RuleFor(x => x.Address).NotEmpty().WithMessage("Minio-Adresse ist erforderlich");
        RuleFor(x => x.AccessKey).NotEmpty().WithMessage("Minio Access Key ist erforderlich");
        RuleFor(x => x.SecretKey).NotEmpty().WithMessage("Minio Secret Key ist erforderlich");
    }
}
