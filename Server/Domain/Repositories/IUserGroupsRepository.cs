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
/// Interface for accessing User Groups from both Keycloak and database.
/// </summary>
public interface IUserGroupsRepository
{
    /// <summary>
    /// Retrieves all User Groups from Keycloak and syncs them with the database.
    /// </summary>
    /// <returns>A collection of UserGroup objects representing the User Groups.</returns>
    Task<IEnumerable<UserGroup>> GetKeycloakGroups();

    /// <summary>
    /// Retrieves User Groups from Keycloak based on the specified user ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A collection of UserGroup objects representing the User Groups.</returns>
    Task<IEnumerable<UserGroup>> GetKeycloakGroupsByUserId(Guid userId);
    
    /// <summary>
    /// Retrieves all user groups from the database.
    /// </summary>
    /// <returns>A collection of UserGroup objects from the database.</returns>
    Task<IEnumerable<UserGroup>> GetAllUserGroupsAsync();
    
    /// <summary>
    /// Gets a user group by its ID from the database.
    /// </summary>
    /// <param name="id">The ID of the user group.</param>
    /// <returns>The user group with the specified ID.</returns>
    Task<UserGroup> GetUserGroupByIdAsync(Guid id);
}