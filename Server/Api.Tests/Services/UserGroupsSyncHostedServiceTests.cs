// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Services;
using AccessService.Domain.Models;
using AccessService.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AccessService.Api.Tests.Services;

public class UserGroupsSyncHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSyncUserGroupsPeriodically()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UserGroupsSyncHostedService>>();
        var userGroupsRepository = Substitute.For<IUserGroupsRepository>();

        // Setup service provider and scope
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

        // Mock repository to return user groups
        var userGroups = new List<UserGroup>
        {
            new UserGroup { Id = Guid.NewGuid(), Name = "Group1" },
            new UserGroup { Id = Guid.NewGuid(), Name = "Group2" }
        };

        // Signal once the sync loop has run at least twice, instead of relying on
        // wall-clock timing (which is flaky under CI load).
        const int expectedCalls = 2;
        var callCount = 0;
        var reachedExpectedCalls = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        userGroupsRepository.GetKeycloakGroups().Returns(_ =>
        {
            if (Interlocked.Increment(ref callCount) >= expectedCalls)
            {
                reachedExpectedCalls.TrySetResult();
            }

            return Task.FromResult<IEnumerable<UserGroup>>(userGroups);
        });

        // Setup the service provider to return our mocked repository
        serviceProvider
            .GetService(typeof(IUserGroupsRepository))
            .Returns(userGroupsRepository);

        serviceScope.ServiceProvider.Returns(serviceProvider);
        serviceScopeFactory.CreateScope().Returns(serviceScope);

        // Create the service and set short interval for testing
        var service = new UserGroupsSyncHostedService(serviceScopeFactory, logger);

        // Use reflection to override the private sync interval
        typeof(UserGroupsSyncHostedService)
            .GetField("_syncInterval", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(service, TimeSpan.FromMilliseconds(50));

        // Act
        await service.StartAsync(CancellationToken.None);

        // Wait until the sync loop has actually run the expected number of times,
        // with a generous timeout so a slow CI agent does not cause a false failure.
        var completed = await Task.WhenAny(reachedExpectedCalls.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.Same(reachedExpectedCalls.Task, completed);

        var getKeycloakGroupsCallCount = userGroupsRepository.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IUserGroupsRepository.GetKeycloakGroups));
        Assert.True(getKeycloakGroupsCallCount >= expectedCalls);

        // Verify logging
        logger.ReceivedWithAnyArgs().LogInformation(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionOccurs_ShouldLogErrorAndContinue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UserGroupsSyncHostedService>>();
        var userGroupsRepository = Substitute.For<IUserGroupsRepository>();

        // Setup service provider and scope
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

        // Signal as soon as the repository has been invoked at least once, instead
        // of relying on wall-clock timing (which is flaky under CI load).
        var wasCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Mock repository to throw exception
        userGroupsRepository.GetKeycloakGroups().Returns(_ =>
        {
            wasCalled.TrySetResult();
            return Task.FromException<IEnumerable<UserGroup>>(new Exception("Test exception"));
        });

        // Setup the service provider to return our mocked repository
        serviceProvider
            .GetService(typeof(IUserGroupsRepository))
            .Returns(userGroupsRepository);

        serviceScope.ServiceProvider.Returns(serviceProvider);
        serviceScopeFactory.CreateScope().Returns(serviceScope);

        // Create the service and set short interval for testing
        var service = new UserGroupsSyncHostedService(serviceScopeFactory, logger);

        // Use reflection to override the private sync interval
        typeof(UserGroupsSyncHostedService)
            .GetField("_syncInterval", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(service, TimeSpan.FromMilliseconds(50));

        // Act
        await service.StartAsync(CancellationToken.None);

        // Wait until the sync loop has actually invoked the repository at least once,
        // with a generous timeout so a slow CI agent does not cause a false failure.
        var completed = await Task.WhenAny(wasCalled.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.Same(wasCalled.Task, completed);

        // Verify error is logged
        logger.ReceivedWithAnyArgs().LogError(Arg.Any<Exception>(), default);
    }
}