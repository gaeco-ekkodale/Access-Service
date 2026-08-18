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
using AccessService.Infrastructure.Repositories;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AccessService.Infrastructure.Tests.Repositories;

public class UserGroupsRepositoryTests : IDisposable
{
    private static Faker<UserGroup> FakerUserGroup => new Faker<UserGroup>()
        .RuleFor(g => g.Id, f => Guid.NewGuid())
        .RuleFor(g => g.Name, f => f.Commerce.Department());

    private readonly AccessRightDbContext _context;
    private readonly IOutboxRepository _outboxRepo;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly HttpMessageHandlerMock _handlerMock;
    private readonly UserGroupsRepository _repo;

    public UserGroupsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AccessRightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AccessRightDbContext(options);
        _outboxRepo = new OutboxRepository(_context);

        // Mock configuration
        _configuration = Substitute.For<IConfiguration>();
        _configuration["Keycloak:ServerUrl"].Returns("https://keycloak-server");
        _configuration["Keycloak:Realm"].Returns("test-realm");
        _configuration["Keycloak:ClientId"].Returns("test-client");
        _configuration["Keycloak:ClientSecret"].Returns("test-secret");
        _configuration["Kafka:Topics:UserGroups"].Returns("user-groups-topic");

        // Setup HTTP mock
        _handlerMock = new HttpMessageHandlerMock();
        _httpClient = new HttpClient(_handlerMock);

        _repo = new UserGroupsRepository(_httpClient, _configuration, _context, _outboxRepo);
    }

    [Fact]
    public async Task GetAllUserGroupsAsync_ShouldReturnAllGroups()
    {
        // Arrange
        var groups = FakerUserGroup.Generate(3);
        await _context.UserGroups.AddRangeAsync(groups);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.GetAllUserGroupsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(g => g.Id).Should().BeEquivalentTo(groups.Select(g => g.Id));
    }

    [Fact]
    public async Task GetUserGroupByIdAsync_WhenGroupExists_ShouldReturnGroup()
    {
        // Arrange
        var group = FakerUserGroup.Generate();
        await _context.UserGroups.AddAsync(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repo.GetUserGroupByIdAsync(group.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(group.Id);
        result.Name.Should().Be(group.Name);
    }

    [Fact]
    public async Task GetUserGroupByIdAsync_WhenGroupDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await _repo.GetUserGroupByIdAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage($"User group with ID {nonExistentId} not found.");
    }

    [Fact]
    public async Task GetKeycloakGroups_ShouldFetchGroupsAndSyncWithDatabase()
    {
        // Arrange
        var keycloakGroups = FakerUserGroup.Generate(3);

        // Mock token response
        var tokenResponse = new { access_token = "test-token", expires_in = 300, token_type = "Bearer" };
        _handlerMock.SetupResponse(HttpMethod.Post, "https://keycloak-server/realms/test-realm/protocol/openid-connect/token",
            JsonSerializer.Serialize(tokenResponse), HttpStatusCode.OK);

        // Mock groups response
        _handlerMock.SetupResponse(HttpMethod.Get, "https://keycloak-server/admin/realms/test-realm/groups",
            JsonSerializer.Serialize(keycloakGroups), HttpStatusCode.OK);

        // Act
        var result = await _repo.GetKeycloakGroups();

        // Assert
        result.Should().HaveCount(3);
        result.Select(g => g.Id).Should().BeEquivalentTo(keycloakGroups.Select(g => g.Id));

        // Verify database sync
        var dbGroups = await _context.UserGroups.ToListAsync();
        dbGroups.Should().HaveCount(3);
        dbGroups.Select(g => g.Id).Should().BeEquivalentTo(keycloakGroups.Select(g => g.Id));

        // Verify outbox events
        var outboxEvents = await _context.OutboxEvents.ToListAsync();
        outboxEvents.Should().HaveCount(3); // 3 Created events
        outboxEvents.Select(e => e.EventType).Should().AllBe("CreatedUserGroup");
    }

    [Fact]
    public async Task GetKeycloakGroups_WhenGroupsChange_ShouldUpdateDatabaseAndCreateEvents()
    {
        // Arrange - Add initial groups to the database
        var existingGroups = FakerUserGroup.Generate(2);
        await _context.UserGroups.AddRangeAsync(existingGroups);
        await _context.SaveChangesAsync();

        // One group will remain unchanged, one will be updated, and one new group will be added
        var unchangedGroup = existingGroups[0];
        var updatedGroup = new UserGroup { Id = existingGroups[1].Id, Name = "Updated Name" };
        var newGroup = FakerUserGroup.Generate();

        var keycloakGroups = new List<UserGroup> { unchangedGroup, updatedGroup, newGroup };

        // Mock token response
        var tokenResponse = new { access_token = "test-token", expires_in = 300, token_type = "Bearer" };
        _handlerMock.SetupResponse(HttpMethod.Post, "https://keycloak-server/realms/test-realm/protocol/openid-connect/token",
            JsonSerializer.Serialize(tokenResponse), HttpStatusCode.OK);

        // Mock groups response
        _handlerMock.SetupResponse(HttpMethod.Get, "https://keycloak-server/admin/realms/test-realm/groups",
            JsonSerializer.Serialize(keycloakGroups), HttpStatusCode.OK);

        // Act
        var result = await _repo.GetKeycloakGroups();

        // Assert
        result.Should().HaveCount(3);

        // Verify database sync
        var dbGroups = await _context.UserGroups.ToListAsync();
        dbGroups.Should().HaveCount(3);
        dbGroups.Select(g => g.Id).Should().BeEquivalentTo(keycloakGroups.Select(g => g.Id));

        // Verify the updated name
        var updatedDbGroup = await _context.UserGroups.FindAsync(updatedGroup.Id);
        updatedDbGroup.Name.Should().Be("Updated Name");

        // Verify outbox events - we should have UpdatedUserGroup and CreatedUserGroup events
        var outboxEvents = await _context.OutboxEvents.ToListAsync();
        outboxEvents.Should().HaveCount(2);

        outboxEvents.Count(e => e.EventType == "CreatedUserGroup").Should().Be(1);
        outboxEvents.Count(e => e.EventType == "UpdatedUserGroup").Should().Be(1);

        // Verify specific event details
        outboxEvents.Where(e => e.EventType == "CreatedUserGroup")
            .Select(e => e.AggregateId)
            .Should().Contain(newGroup.Id.ToString());

        outboxEvents.Where(e => e.EventType == "UpdatedUserGroup")
            .Select(e => e.AggregateId)
            .Should().Contain(updatedGroup.Id.ToString());
    }

    [Fact]
    public async Task GetKeycloakGroups_WithRemovedGroups_ShouldUpdateDatabaseAndCreateDeletedEvent()
    {
        // Arrange - Add initial groups to the database
        var existingGroups = FakerUserGroup.Generate(3);
        await _context.UserGroups.AddRangeAsync(existingGroups);
        await _context.SaveChangesAsync();

        // Return only one of the groups from Keycloak, simulating that two were deleted
        var remainingGroup = existingGroups[0];
        var keycloakGroups = new List<UserGroup> { remainingGroup };

        // Mock token response
        var tokenResponse = new { access_token = "test-token", expires_in = 300, token_type = "Bearer" };
        _handlerMock.SetupResponse(HttpMethod.Post, "https://keycloak-server/realms/test-realm/protocol/openid-connect/token",
            JsonSerializer.Serialize(tokenResponse), HttpStatusCode.OK);

        // Mock groups response
        _handlerMock.SetupResponse(HttpMethod.Get, "https://keycloak-server/admin/realms/test-realm/groups",
            JsonSerializer.Serialize(keycloakGroups), HttpStatusCode.OK);

        // Act
        var result = await _repo.GetKeycloakGroups();

        // Assert
        result.Should().HaveCount(1);

        // Verify database sync - should only have the remaining group
        var dbGroups = await _context.UserGroups.ToListAsync();
        dbGroups.Should().HaveCount(1);
        dbGroups[0].Id.Should().Be(remainingGroup.Id);

        // Verify outbox events - we should have DeletedUserGroup events for the two removed groups
        var outboxEvents = await _context.OutboxEvents.ToListAsync();
        outboxEvents.Should().HaveCount(2);
        outboxEvents.Select(e => e.EventType).Should().AllBe("DeletedUserGroup");

        // Verify deleted groups
        var deletedGroupIds = new[] { existingGroups[1].Id, existingGroups[2].Id };
        outboxEvents.Select(e => e.AggregateId)
            .Should().BeEquivalentTo(deletedGroupIds.Select(id => id.ToString()));
    }

    [Fact]
    public async Task GetKeycloakGroupsByUserId_ShouldReturnUserGroups()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userGroups = FakerUserGroup.Generate(2);

        // Mock token response
        var tokenResponse = new { access_token = "test-token", expires_in = 300, token_type = "Bearer" };
        _handlerMock.SetupResponse(HttpMethod.Post, "https://keycloak-server/realms/test-realm/protocol/openid-connect/token",
            JsonSerializer.Serialize(tokenResponse), HttpStatusCode.OK);

        // Mock user groups response
        _handlerMock.SetupResponse(HttpMethod.Get, $"https://keycloak-server/admin/realms/test-realm/users/{userId}/groups",
            JsonSerializer.Serialize(userGroups), HttpStatusCode.OK);

        // Act
        var result = await _repo.GetKeycloakGroupsByUserId(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Select(g => g.Id).Should().BeEquivalentTo(userGroups.Select(g => g.Id));
    }

    [Fact]
    public async Task GetKeycloakGroups_WhenHttpRequestFails_ShouldThrowException()
    {
        // Arrange
        // Mock token response
        var tokenResponse = new { access_token = "test-token", expires_in = 300, token_type = "Bearer" };
        _handlerMock.SetupResponse(HttpMethod.Post, "https://keycloak-server/realms/test-realm/protocol/openid-connect/token",
            JsonSerializer.Serialize(tokenResponse), HttpStatusCode.OK);

        // Mock groups response with error
        _handlerMock.SetupResponse(HttpMethod.Get, "https://keycloak-server/admin/realms/test-realm/groups",
            "Error", HttpStatusCode.InternalServerError);

        // Act
        var act = async () => await _repo.GetKeycloakGroups();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _httpClient.Dispose();
    }
}

/// <summary>
/// Mock HttpMessageHandler for testing HTTP requests
/// </summary>
public class HttpMessageHandlerMock : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode StatusCode, string Content)> _responses = new();

    public void SetupResponse(HttpMethod method, string requestUri, string content, HttpStatusCode statusCode)
    {
        _responses[$"{method}:{requestUri}"] = (statusCode, content);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = $"{request.Method}:{request.RequestUri}";

        if (_responses.TryGetValue(key, out var response))
        {
            var responseMessage = new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Content)
            };

            // Set content type to application/json
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return Task.FromResult(responseMessage);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}