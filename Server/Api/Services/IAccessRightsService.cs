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

namespace AccessService.Api.Services;

public interface IAccessRightsService
{
    /// <summary>
    /// Creates a new access right. The Id on the input is ignored; a new Id is generated server-side.
    /// </summary>
    /// <param name="accessRight">The access right to create.</param>
    Task CreateAccessRightAsync(AccessRight accessRight);

    /// <summary>
    /// Updates an existing access right.
    /// </summary>
    /// <param name="accessRight">The access right to update.</param>
    Task UpdateAccessRightAsync(AccessRight accessRight);

    /// <summary>
    /// Deletes an access right by its ID.
    /// </summary>
    /// <param name="id">The ID of the access right to delete.</param>
    Task DeleteAccessRightAsync(string id);

    /// <summary>
    /// Commits the final access-right state for a specific use case and user group.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <param name="userGroupId">The user group ID.</param>
    /// <param name="accessRights">The final set of access rights that should remain persisted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyCollection<AccessRight>> CommitAccessRightsAsync(Guid useCaseId, Guid userGroupId, IEnumerable<AccessRight> accessRights, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all access rights associated with a specific use case.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case.</param>
    Task DeleteAccessRightsByUseCaseAsync(string useCaseId);
}
