// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.DTOs;
using AccessService.Domain.Models;

namespace AccessService.Api.Services;

public interface IClassificationsService
{
    Task<ClassificationsListSet?> GetClassificationsAsync(CancellationToken cancellationToken = default);
    Task<Classification?> GetClassificationAsync(string id, CancellationToken cancellationToken = default);
    Task<List<ClassificationPropertyDTO>> GetPropertiesByClassificationIdAsync(string classificationId, CancellationToken cancellationToken = default);
    Task<List<GuidelineDTO>> GetGuidelinesAsync(CancellationToken cancellationToken = default);
    Task<ClassificationsListSet?> GetClassificationsByGuidelineAsync(string guidelineId, CancellationToken cancellationToken = default);
    Task<ClassificationDetailDTO?> GetClassificationDetailByGuidelineAsync(string guidelineId, string classificationId, CancellationToken cancellationToken = default);
}
