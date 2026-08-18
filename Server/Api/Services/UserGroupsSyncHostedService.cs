// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Domain.Repositories;

namespace AccessService.Api.Services;

/// <summary>
/// Background service that periodically synchronizes user groups from Keycloak.
/// </summary>
public class UserGroupsSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserGroupsSyncHostedService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(8);

    /// <summary>
    /// Initializes a new instance of the <see cref="UserGroupsSyncHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The factory for creating service scopes.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    public UserGroupsSyncHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserGroupsSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserGroupsSyncHostedService started. Will sync every {SyncInterval} hours", _syncInterval.TotalHours);

        // Run the periodic sync loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform the synchronization
                await SyncUserGroupsAsync(stoppingToken);

                // Wait for the specified interval before the next sync
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while synchronizing user groups");

                // Wait a shorter time before retrying after an error
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("UserGroupsSyncHostedService stopped");
    }

    private async Task SyncUserGroupsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled user groups synchronization from Keycloak");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userGroupsRepository = scope.ServiceProvider.GetService<IUserGroupsRepository>();

            if (userGroupsRepository == null)
            {
                _logger.LogError("Failed to resolve IUserGroupsRepository");
                return;
            }

            // Fetch and sync user groups from Keycloak
            var groups = await userGroupsRepository.GetKeycloakGroups();

            _logger.LogInformation("Successfully synchronized {Count} user groups from Keycloak", groups.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize user groups from Keycloak");
            throw; // Rethrow to be handled by the main loop
        }
    }
}