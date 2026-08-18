// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AccessService.Domain.Models.Enums;

/// <summary>
/// Enum the access rights of a classification.
/// </summary>
public enum ClassificationRight
{
    None = 0,
    Write = 1,
    Read = 2,
    Mixed = 3
}
