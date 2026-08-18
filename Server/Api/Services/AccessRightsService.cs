// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Domain.Models;
using AccessService.Domain.Repositories;

namespace AccessService.Api.Services;

public class AccessRightsService : IAccessRightsService
{
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly IUseCaseGuidelineService _useCaseGuidelineService;
    private readonly ILogger<AccessRightsService> _logger;

    public AccessRightsService(
        IAccessRightsRepository accessRightsRepository,
        IUseCaseGuidelineService useCaseGuidelineService,
        ILogger<AccessRightsService> logger)
    {
        _accessRightsRepository = accessRightsRepository;
        _useCaseGuidelineService = useCaseGuidelineService;
        _logger = logger;
    }

    public async Task CreateAccessRightAsync(AccessRight accessRight)
    {
        var newId = Guid.NewGuid().ToString();

        var newAccessRight = new AccessRight(
            newId,
            accessRight.Name,
            accessRight.GuidelineClassificationId,
            accessRight.UserGroupId,
            accessRight.UseCaseId,
            accessRight.GuidlineClassificationPropertyId,
            accessRight.Right
        );

        await _accessRightsRepository.CreateAccessRightAsync(newAccessRight);

        _logger.LogInformation("CreatedAccessRight with id: {Id}", newId);
    }

    public async Task UpdateAccessRightAsync(AccessRight accessRight)
    {
        await _accessRightsRepository.UpdateAccessRightAsync(accessRight);

        _logger.LogInformation("UpdatedAccessRight with id: {Id}", accessRight.Id);
    }

    public async Task DeleteAccessRightAsync(string id)
    {
        await _accessRightsRepository.DeleteAccessRightAsync(id);

        _logger.LogInformation("DeletedAccessRight with id: {Id}", id);
    }

    public async Task<IReadOnlyCollection<AccessRight>> CommitAccessRightsAsync(Guid useCaseId, Guid userGroupId, IEnumerable<AccessRight> accessRights, CancellationToken cancellationToken = default)
    {
        var accessRightsList = accessRights.ToList();

        var committedAccessRights = await _accessRightsRepository.CommitAccessRightsAsync(
            useCaseId,
            userGroupId,
            accessRightsList,
            cancellationToken);

        _logger.LogInformation(
            "Committed {Count} access rights for use case {UseCaseId} and user group {UserGroupId}",
            committedAccessRights.Count,
            useCaseId,
            userGroupId);

        try
        {
            await _useCaseGuidelineService.GenerateForUserGroupAsync(useCaseId, userGroupId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate UseCase-Guideline after committing access rights for use case {UseCaseId} and user group {UserGroupId}",
                useCaseId,
                userGroupId);
        }

        return committedAccessRights;
    }

    public async Task DeleteAccessRightsByUseCaseAsync(string useCaseId)
    {
        var affectedAccessRights = await _accessRightsRepository.GetAccessRightsByUseCaseAsync(useCaseId);

        foreach (var accessRight in affectedAccessRights)
        {
            await _accessRightsRepository.DeleteAccessRightAsync(accessRight.Id);

            _logger.LogInformation("DeletedAccessRight with id: {Id} (use case deleted)", accessRight.Id);
        }
    }
}
