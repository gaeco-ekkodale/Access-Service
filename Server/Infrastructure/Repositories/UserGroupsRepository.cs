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
using AccessService.Events.UserGroups;
using AccessService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AccessService.Infrastructure.Repositories;

/// <summary>
/// Repository for managing user groups by interacting with Keycloak and a local database.
/// </summary>
public class UserGroupsRepository : IUserGroupsRepository
{
    private readonly HttpClient _httpClient;
    private readonly AccessRightDbContext _dbContext;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IConfiguration _configuration;
    private readonly string _usergroupsTopic;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserGroupsRepository"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making API requests to Keycloak.</param>
    /// <param name="configuration">The application configuration for Keycloak and Kafka settings.</param>
    /// <param name="dbContext">The database context for user group data.</param>
    /// <param name="outboxRepository">The repository for handling outbox messages.</param>
    /// <exception cref="ArgumentNullException">Thrown if the Kafka topic for user groups is not configured.</exception>
    public UserGroupsRepository(HttpClient httpClient, IConfiguration configuration, AccessRightDbContext dbContext, IOutboxRepository outboxRepository)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _configuration = configuration;
        _outboxRepository = outboxRepository;
        _usergroupsTopic = configuration["Kafka:Topics:UserGroups"] ?? throw new ArgumentNullException("Kafka:Topics:UserGroups");
    }

    /// <summary>
    /// API call to get all User Groups from Keycloak.
    /// </summary>
    /// <returns>A list of all user groups.</returns>
    public async Task<IEnumerable<UserGroup>> GetKeycloakGroups()
    {
        try
        {
            var token = await GetAccessToken();

            var groups = await GetUserGroupsAsync(token);

            // Store groups in the database
            await SyncGroupsWithDatabase(groups);

            return groups;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// API call to get all groups of a specific user by user ID from Keycloak.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A list of all groups the user belongs to.</returns>
    public async Task<IEnumerable<UserGroup>> GetKeycloakGroupsByUserId(Guid userId)
    {
        try
        {
            var token = await GetAccessToken();
            var groups = await GetUserGroupsByUserIdAsync(token, userId);

            return groups;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all user groups from the database.
    /// </summary>
    /// <returns>A list of all user groups stored in the database.</returns>
    public async Task<IEnumerable<UserGroup>> GetAllUserGroupsAsync()
    {
        return await _dbContext.UserGroups.ToListAsync();
    }

    /// <summary>
    /// Gets a user group by its ID from the database.
    /// </summary>
    /// <param name="id">The ID of the user group.</param>
    /// <returns>The user group with the specified ID.</returns>
    public async Task<UserGroup> GetUserGroupByIdAsync(Guid id)
    {
        var userGroup = await _dbContext.UserGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (userGroup == null)
        {
            throw new OperationCanceledException($"User group with ID {id} not found.");
        }
        return userGroup;
    }

    /// <summary>
    /// Synchronizes the groups from Keycloak with the database.
    /// </summary>
    /// <param name="keycloakGroups">The groups retrieved from Keycloak.</param>
    private async Task SyncGroupsWithDatabase(IEnumerable<UserGroup> keycloakGroups)
    {
        // Create a HashSet of IDs from Keycloak groups for quick lookup
        var keycloakGroupIds = new HashSet<Guid>(keycloakGroups.Select(g => g.Id));

        // Get all existing groups in the database
        var existingGroups = await _dbContext.UserGroups.ToListAsync();
        var existingGroupIds = new HashSet<Guid>(existingGroups.Select(g => g.Id));

        // Add new groups that exist in Keycloak but not in the database
        foreach (var group in keycloakGroups)
        {
            if (!existingGroupIds.Contains(group.Id))
            {
                await _dbContext.UserGroups.AddAsync(group);

                // Create outbox event for new group
                var createdEvent = new CreatedUserGroup
                {
                    Id = group.Id,
                    Name = group.Name
                };
                _outboxRepository.Add(createdEvent, _usergroupsTopic, group.Id.ToString());
            }
            else
            {
                // Update the name if it's changed
                var existingGroup = await _dbContext.UserGroups.FindAsync(group.Id);
                if (existingGroup != null && existingGroup.Name != group.Name)
                {
                    existingGroup.Name = group.Name;
                    _dbContext.UserGroups.Update(existingGroup);

                    // Create outbox event for updated group
                    var updatedEvent = new UpdatedUserGroup
                    {
                        Id = existingGroup.Id,
                        Name = existingGroup.Name
                    };
                    _outboxRepository.Add(updatedEvent, _usergroupsTopic, existingGroup.Id.ToString());
                }
            }
        }

        // Remove groups that exist in the database but no longer in Keycloak
        foreach (var existingGroup in existingGroups)
        {
            if (!keycloakGroupIds.Contains(existingGroup.Id))
            {
                _dbContext.UserGroups.Remove(existingGroup);

                // Create outbox event for deleted group
                var deletedEvent = new DeletedUserGroup
                {
                    Id = existingGroup.Id
                };
                _outboxRepository.Add(deletedEvent, _usergroupsTopic, existingGroup.Id.ToString());
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// API call to get access token from Keycloak.
    /// </summary>
    /// <returns>Access Token</returns>
    private async Task<string> GetAccessToken()
    {
        var tokenEndpoint = $"{_configuration["Keycloak:ServerUrl"]}/realms/{_configuration["Keycloak:Realm"]}/protocol/openid-connect/token";
        var clientCredentials = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _configuration["Keycloak:ClientId"],
            ["client_secret"] = _configuration["Keycloak:ClientSecret"]
        };

        var tokenResponse = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(clientCredentials));
        tokenResponse.EnsureSuccessStatusCode();

        var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenContent.AccessToken;
    }

    /// <summary>
    /// API call to retrieve all user groups with the access token from Keycloak.
    /// </summary>
    /// <param name="accessToken">The access token for authorization.</param>
    /// <returns>A list of all user groups.</returns>
    private async Task<IEnumerable<UserGroup>> GetUserGroupsAsync(string accessToken)
    {
        var groupsEndpoint = $"{_configuration["Keycloak:ServerUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/groups";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var groupsResponse = await _httpClient.GetAsync(groupsEndpoint);
        groupsResponse.EnsureSuccessStatusCode();

        var groups = await groupsResponse.Content.ReadFromJsonAsync<IEnumerable<UserGroup>>();
        return groups;
    }

    /// <summary>
    /// API call to retrieve all groups of a specific user with the access token from Keycloak.
    /// </summary>
    /// <param name="accessToken">The access token for authorization.</param>
    /// <param name="userId">The ID of the user whose groups are to be retrieved.</param>
    /// <returns>A list of all groups the user belongs to.</returns>
    private async Task<IEnumerable<UserGroup>> GetUserGroupsByUserIdAsync(string accessToken, Guid userId)
    {
        var userGroupsEndpoint = $"{_configuration["Keycloak:ServerUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{userId.ToString()}/groups";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userGroupsResponse = await _httpClient.GetAsync(userGroupsEndpoint);
        userGroupsResponse.EnsureSuccessStatusCode();

        var userGroups = await userGroupsResponse.Content.ReadFromJsonAsync<IEnumerable<UserGroup>>();
        return userGroups;
    }
}