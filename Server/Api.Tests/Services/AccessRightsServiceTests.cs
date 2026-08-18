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
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AccessService.Api.Tests.Services;

public class AccessRightsServiceTests
{
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly IUseCaseGuidelineService _useCaseGuidelineService;
    private readonly ILogger<AccessRightsService> _logger;
    private readonly AccessRightsService _sut;

    public AccessRightsServiceTests()
    {
        _accessRightsRepository = Substitute.For<IAccessRightsRepository>();
        _useCaseGuidelineService = Substitute.For<IUseCaseGuidelineService>();
        _logger = Substitute.For<ILogger<AccessRightsService>>();
        _sut = new AccessRightsService(_accessRightsRepository, _useCaseGuidelineService, _logger);
    }

    [Fact]
    public async Task CommitAccessRightsAsync_WhenCommitSucceeds_ThenGeneratesUseCaseGuideline()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();
        var accessRights = new[]
        {
            new AccessRight(string.Empty, "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read)
        };
        IReadOnlyCollection<AccessRight> committedAccessRights =
        [
            new AccessRight("generated-id", "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read)
        ];

        _accessRightsRepository.CommitAccessRightsAsync(useCaseId, userGroupId, Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<CancellationToken>())
            .Returns(committedAccessRights);

        // Act
        var result = await _sut.CommitAccessRightsAsync(useCaseId, userGroupId, accessRights, CancellationToken.None);

        // Assert
        Assert.Equivalent(committedAccessRights, result);
        await _useCaseGuidelineService.Received(1).GenerateForUserGroupAsync(useCaseId, userGroupId, CancellationToken.None);
    }

    [Fact]
    public async Task CommitAccessRightsAsync_WhenCommitFails_ThenDoesNotGenerateUseCaseGuideline()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();
        var accessRights = new[]
        {
            new AccessRight(string.Empty, "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read)
        };

        _accessRightsRepository.CommitAccessRightsAsync(useCaseId, userGroupId, Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyCollection<AccessRight>>(new InvalidOperationException("commit failed")));

        // Act
        var act = () => _sut.CommitAccessRightsAsync(useCaseId, userGroupId, accessRights, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        await _useCaseGuidelineService.DidNotReceive().GenerateForUserGroupAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAccessRightsAsync_WhenGuidelineGenerationFails_ThenReturnsCommittedAccessRights()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();
        var accessRights = new[]
        {
            new AccessRight(string.Empty, "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read)
        };
        IReadOnlyCollection<AccessRight> committedAccessRights =
        [
            new AccessRight("generated-id", "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read)
        ];

        _accessRightsRepository.CommitAccessRightsAsync(useCaseId, userGroupId, Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<CancellationToken>())
            .Returns(committedAccessRights);
        _useCaseGuidelineService.GenerateForUserGroupAsync(useCaseId, userGroupId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("generation failed")));

        // Act
        var result = await _sut.CommitAccessRightsAsync(useCaseId, userGroupId, accessRights, CancellationToken.None);

        // Assert
        Assert.Equivalent(committedAccessRights, result);
    }
}