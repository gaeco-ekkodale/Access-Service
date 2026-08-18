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

namespace AccessService.Domain.Repositories;

/// <summary>
/// Interface for accessing and managing access rights in the database.
/// </summary>
public interface IAccessRightsRepository
{

    /// <summary>
    /// Retrieves an access right by its ID.
    /// </summary>
    /// <param name="id">The ID of the access right.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the AccessRightDb object.</returns>
    Task<AccessRight> GetAccessRightAsync(string id);

    /// <summary>
    /// Retrieves all access rights.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an IEnumerable of AccessRightDb objects.</returns>
    Task<IEnumerable<AccessRight>> GetAllAccessRightsAsync();

    /// <summary>
    /// Retrieves access rights by use case ID.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an IEnumerable of AccessRightDb objects.</returns>
    Task<IEnumerable<AccessRight>> GetAccessRightsByUseCaseAsync(string useCaseId);

    /// <summary>
    /// Retrieves access rights by user group ID.
    /// </summary>
    /// <param name="userGroupId">The ID of the user group.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an IEnumerable of AccessRightDb objects.</returns>
    Task<IEnumerable<AccessRight>> GetAccessRightsByUserGroupAsync(string userGroupId);

    /// <summary>
    /// Retrieves access rights by use case ID and user group ID.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case.</param>
    /// <param name="userGroupId">The ID of the user group.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an IEnumerable of AccessRightDb objects.</returns>
    Task<IEnumerable<AccessRight>> GetAccessRightsByUseCaseUserGroupAsync(string useCaseId, string userGroupId);

    /// <summary>
    /// Retrieves access rights by use case ID, user group ID, and classification ID.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case.</param>
    /// <param name="userGroupId">The ID of the user group.</param>
    /// <param name="classificationId">The ID of the classification.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an IEnumerable of AccessRightDb objects.</returns>
    Task<IEnumerable<AccessRight>> GetAccessRightsByUseCaseUserGroupClassificationAsync(string useCaseId, string userGroupId, string classificationId);

    /// <summary>
    /// Commits the final access-right state for a specific use case and user group.
    /// </summary>
    /// <param name="useCaseId">The use case identifier.</param>
    /// <param name="userGroupId">The user group identifier.</param>
    /// <param name="accessRights">The final set of access rights that should remain persisted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains the persisted access rights after the commit.</returns>
    Task<IReadOnlyCollection<AccessRight>> CommitAccessRightsAsync(Guid useCaseId, Guid userGroupId, IEnumerable<AccessRight> accessRights, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new access right.
    /// </summary>
    /// <param name="newAccessRight">The AccessRightDb object to create.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the created AccessRightDb object.</returns>
    Task<AccessRight> CreateAccessRightAsync(AccessRight newAccessRight);

    /// <summary>
    /// Updates an existing access right.
    /// </summary>
    /// <param name="updatedAccessRight">The AccessRightDb object to update.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the updated AccessRightDb object.</returns>
    Task<AccessRight> UpdateAccessRightAsync(AccessRight updatedAccessRight);

    /// <summary>
    /// Deletes an access right by its ID.
    /// </summary>
    /// <param name="id">The ID of the access right to delete.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains the deleted AccessRightDb object.</returns>
    Task<AccessRight> DeleteAccessRightAsync(string id);

    /// <summary>
    /// Returns all distinct UseCase IDs that have at least one access right.
    /// </summary>
    Task<List<Guid>> GetDistinctUseCaseIdsAsync();

    /// <summary>
    /// Returns all distinct (UseCaseId, UserGroupId) pairs that have at least one access right.
    /// </summary>
    Task<List<(Guid UseCaseId, Guid UserGroupId)>> GetDistinctUseCaseUserGroupPairsAsync();

    /// <summary>
    /// Deletes all access rights whose <c>GuidelineClassificationId</c> is in
    /// <paramref name="classificationIds"/> OR whose <c>GuidlineClassificationPropertyId</c> is in
    /// <paramref name="classificationPropertyIds"/>, and publishes a <c>DeletedAccessRight</c>
    /// outbox event for each deleted record. No-op when both collections are empty.
    /// </summary>
    Task DeleteOrphanedAccessRightsAsync(
        IEnumerable<string> classificationIds,
        IEnumerable<string> classificationPropertyIds,
        CancellationToken cancellationToken = default);
}
