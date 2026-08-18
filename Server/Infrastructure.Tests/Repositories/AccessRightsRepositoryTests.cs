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
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using AccessService.Infrastructure.Repositories;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace AccessService.Infrastructure.Tests.Repositories;

public class AccessRightsRepositoryTests : IDisposable
{
    private static Faker<AccessRight> FakerAccessRight => new Faker<AccessRight>()
        .CustomInstantiator(f => new AccessRight(
            f.Random.AlphaNumeric(10),
            f.Lorem.Word(),
            f.Random.AlphaNumeric(8),
            Guid.NewGuid(),
            Guid.NewGuid(),
            f.Random.AlphaNumeric(8),
            (PropertyRight)f.Random.Int(0, 2)));

    private readonly AccessRightDbContext _context;
    private readonly IOutboxRepository _outboxRepo;
    private readonly AccessRightsRepository _repo;
    private readonly IConfiguration _configuration;

    public AccessRightsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AccessRightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        // Mock configuration
        _configuration = Substitute.For<IConfiguration>();
        _configuration["Kafka:Topics:AccessRights"].Returns("access-rights-topic");

        _context = new AccessRightDbContext(options);
        _outboxRepo = new OutboxRepository(_context);
        _repo = new AccessRightsRepository(_context, _outboxRepo, _configuration);
    }

    [Fact]
    public async Task When_CreatingAccessRight_Then_AccessRightPersistedAndOutboxEventAdded()
    {
        var accessRight = FakerAccessRight.Generate();

        var created = await _repo.CreateAccessRightAsync(accessRight);

        created.Should().NotBeNull();
        (await _context.AccessRights.CountAsync()).Should().Be(1);
        (await _context.OutboxEvents.CountAsync()).Should().Be(1);
        var evt = await _context.OutboxEvents.FirstAsync();
        evt.EventType.Should().Be("CreatedAccessRight");
        evt.AggregateId.Should().Be(accessRight.Id);
    }

    [Fact]
    public async Task When_UpdatingExistingAccessRight_Then_AccessRightUpdatedAndOutboxEventAdded()
    {
        var original = FakerAccessRight.Generate();
        await _repo.CreateAccessRightAsync(original);

        original.Name = "Updated Name";
        original.GuidelineClassificationId = "Updated Classification";

        var updated = await _repo.UpdateAccessRightAsync(original);

        updated.Name.Should().Be("Updated Name");
        updated.GuidelineClassificationId.Should().Be("Updated Classification");
        (await _context.OutboxEvents.CountAsync()).Should().Be(2); // Created + Updated
        (await _context.OutboxEvents.Select(e => e.EventType).ToListAsync())
            .Should().Contain(new[] { "CreatedAccessRight", "UpdatedAccessRight" });
    }

    [Fact]
    public async Task When_DeletingExistingAccessRight_Then_AccessRightRemovedAndOutboxEventAdded()
    {
        var ar = FakerAccessRight.Generate();
        await _repo.CreateAccessRightAsync(ar);

        var deleted = await _repo.DeleteAccessRightAsync(ar.Id);

        deleted.Id.Should().Be(ar.Id);
        (await _context.AccessRights.AnyAsync()).Should().BeFalse();
        (await _context.OutboxEvents.CountAsync()).Should().Be(2); // Created + Deleted
        var types = await _context.OutboxEvents.Select(e => e.EventType).ToListAsync();
        types.Should().Contain(new[] { "CreatedAccessRight", "DeletedAccessRight" });
    }

    [Fact]
    public async Task When_GettingAllAccessRights_Then_ReturnsAllPersisted()
    {
        var list = FakerAccessRight.Generate(3);
        foreach (var ar in list)
        {
            await _repo.CreateAccessRightAsync(ar);
        }

        var all = (await _repo.GetAllAccessRightsAsync()).ToList();
        all.Should().HaveCount(3);
        all.Select(a => a.Id).Should().BeEquivalentTo(list.Select(l => l.Id));
    }

    [Fact]
    public async Task When_GettingNonExistingAccessRight_Then_ThrowsOperationCanceledException()
    {
        var act = async () => await _repo.GetAccessRightAsync(Guid.NewGuid().ToString("N"));

        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("Access Right not found");
    }

    [Fact]
    public async Task When_GettingAccessRightsByUseCase_Then_ReturnsMatchingAccessRights()
    {
        // Create access rights with different use case IDs
        var useCaseId = Guid.NewGuid();
        var matchingAccessRights = FakerAccessRight.Clone()
            .RuleFor(ar => ar.UseCaseId, useCaseId)
            .Generate(2);

        var nonMatchingAccessRights = FakerAccessRight.Generate(2);

        foreach (var ar in matchingAccessRights.Concat(nonMatchingAccessRights))
        {
            await _repo.CreateAccessRightAsync(ar);
        }

        var result = (await _repo.GetAccessRightsByUseCaseAsync(useCaseId.ToString())).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().BeEquivalentTo(matchingAccessRights.Select(m => m.Id));
    }

    [Fact]
    public async Task When_GettingAccessRightsByUserGroup_Then_ReturnsMatchingAccessRights()
    {
        // Create access rights with different user group IDs
        var userGroupId = Guid.NewGuid();
        var matchingAccessRights = FakerAccessRight.Clone()
            .RuleFor(ar => ar.UserGroupId, userGroupId)
            .Generate(2);

        var nonMatchingAccessRights = FakerAccessRight.Generate(2);

        foreach (var ar in matchingAccessRights.Concat(nonMatchingAccessRights))
        {
            await _repo.CreateAccessRightAsync(ar);
        }

        var result = (await _repo.GetAccessRightsByUserGroupAsync(userGroupId.ToString())).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().BeEquivalentTo(matchingAccessRights.Select(m => m.Id));
    }

    [Fact]
    public async Task When_GettingAccessRightsByUseCaseAndUserGroup_Then_ReturnsMatchingAccessRights()
    {
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();

        // Create access rights with the specific use case and user group
        var matchingAccessRights = FakerAccessRight.Clone()
            .RuleFor(ar => ar.UseCaseId, useCaseId)
            .RuleFor(ar => ar.UserGroupId, userGroupId)
            .Generate(2);

        // Create access rights with only matching use case
        var matchingUseCaseOnly = FakerAccessRight.Clone()
            .RuleFor(ar => ar.UseCaseId, useCaseId)
            .Generate(1);

        // Create access rights with only matching user group
        var matchingUserGroupOnly = FakerAccessRight.Clone()
            .RuleFor(ar => ar.UserGroupId, userGroupId)
            .Generate(1);

        // Create completely non-matching access rights
        var nonMatchingAccessRights = FakerAccessRight.Generate(1);

        foreach (var ar in matchingAccessRights
            .Concat(matchingUseCaseOnly)
            .Concat(matchingUserGroupOnly)
            .Concat(nonMatchingAccessRights))
        {
            await _repo.CreateAccessRightAsync(ar);
        }

        var result = (await _repo.GetAccessRightsByUseCaseUserGroupAsync(
            useCaseId.ToString(), userGroupId.ToString())).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().BeEquivalentTo(matchingAccessRights.Select(m => m.Id));
    }

    [Fact]
    public async Task When_CommittingAccessRights_Then_CreatesUpdatesAndDeletesInSingleCommit()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();

        var unchanged = new AccessRight("existing-1", "Keep", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read);
        var updated = new AccessRight("existing-2", "Old Name", "classification-1", userGroupId, useCaseId, "property-2", PropertyRight.Read);
        var deleted = new AccessRight("existing-3", "Delete", "classification-1", userGroupId, useCaseId, "property-3", PropertyRight.Write);

        await _repo.CreateAccessRightAsync(unchanged);
        await _repo.CreateAccessRightAsync(updated);
        await _repo.CreateAccessRightAsync(deleted);

        var commitPayload = new List<AccessRight>
        {
            new(string.Empty, "Keep", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read),
            new(updated.Id, "Updated Name", "classification-1", userGroupId, useCaseId, "property-2", PropertyRight.Write),
            new(string.Empty, "Create", "classification-2", userGroupId, useCaseId, "property-4", PropertyRight.Read),
        };

        // Act
        var committed = await _repo.CommitAccessRightsAsync(useCaseId, userGroupId, commitPayload);

        // Assert
        committed.Should().HaveCount(3);
        committed.Should().Contain(accessRight => accessRight.GuidlineClassificationPropertyId == "property-1" && accessRight.Right == PropertyRight.Read);
        committed.Should().Contain(accessRight => accessRight.GuidlineClassificationPropertyId == "property-2" && accessRight.Name == "Updated Name" && accessRight.Right == PropertyRight.Write);
        committed.Should().Contain(accessRight => accessRight.GuidlineClassificationPropertyId == "property-4" && accessRight.Name == "Create");
        committed.Should().NotContain(accessRight => accessRight.GuidlineClassificationPropertyId == "property-3");

        var persisted = await _context.AccessRights
            .Where(accessRight => accessRight.UseCaseId == useCaseId && accessRight.UserGroupId == userGroupId)
            .ToListAsync();

        persisted.Should().HaveCount(3);
        persisted.Should().NotContain(accessRight => accessRight.Id == deleted.Id);

        var eventTypes = await _context.OutboxEvents.Select(e => e.EventType).ToListAsync();
        eventTypes.Should().Contain("UpdatedAccessRight");
        eventTypes.Should().Contain("DeletedAccessRight");
        eventTypes.Count(type => type == "CreatedAccessRight").Should().Be(4);
    }

    [Fact]
    public async Task When_CommittingAccessRightsWithNone_Then_NoneEntriesAreRemovedFromPersistedState()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();
        var existing = new AccessRight("existing-1", "Delete Me", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read);

        await _repo.CreateAccessRightAsync(existing);

        var commitPayload = new List<AccessRight>
        {
            new(existing.Id, "Delete Me", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.None)
        };

        // Act
        var committed = await _repo.CommitAccessRightsAsync(useCaseId, userGroupId, commitPayload);

        // Assert
        committed.Should().BeEmpty();
        (await _repo.GetAccessRightsByUseCaseUserGroupAsync(useCaseId.ToString(), userGroupId.ToString())).Should().BeEmpty();
    }

    [Fact]
    public async Task When_CommittingAccessRightsContainsDuplicateNaturalKeys_Then_ThrowsArgumentException()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();
        var commitPayload = new List<AccessRight>
        {
            new(string.Empty, "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read),
            new(string.Empty, "Property One Duplicate", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Write)
        };

        // Act
        var act = () => _repo.CommitAccessRightsAsync(useCaseId, userGroupId, commitPayload);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
