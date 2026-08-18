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

namespace AccessService.Api.Options;

/// <summary>
/// Represents the configuration options for UseCase-Guideline generation and upload.
/// </summary>
public class UseCaseGuidelineOptions
{
    /// <summary>
    /// The name of the configuration section for UseCase-Guideline options.
    /// </summary>
    public const string SectionName = "UseCaseGuideline";

    /// <summary>
    /// Gets or sets the name of the bucket where UseCase-Guidelines are uploaded.
    /// </summary>
    [Required(ErrorMessage = "The UseCaseGuideline-BucketName is required.")]
    public required string BucketName
    {
        get; set;
    }
}
