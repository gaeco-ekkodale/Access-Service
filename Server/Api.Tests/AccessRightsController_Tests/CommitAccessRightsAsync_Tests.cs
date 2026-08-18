// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Controllers;
using AccessService.Api.DTOs;
using AccessService.Api.Services;
using AccessService.Domain.Models;
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AccessService.Api.Tests.AccessRightsController_Tests;

public class CommitAccessRightsAsync_Tests
{
    private readonly IAccessRightsService _accessRightsService;
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRightsController> _logger;
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly AccessRightsController _controller;

    public CommitAccessRightsAsync_Tests()
    {
        _accessRightsService = Substitute.For<IAccessRightsService>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<AccessRightsController>>();
        _accessRightsRepository = Substitute.For<IAccessRightsRepository>();
        _controller = new AccessRightsController(_logger, _mapper, _accessRightsService, _accessRightsRepository);
    }

    [Fact]
    public async Task When_RequestIsValid_Then_ReturnsCommittedAccessRights()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();

        var request = new CommitAccessRightsRequestDTO
        {
            AccessRights =
            [
                new CommitAccessRightDTO
                {
                    Name = "Property One",
                    GuidelineClassificationId = "classification-1",
                    UserGroupId = userGroupId,
                    UseCaseId = useCaseId,
                    GuidlineClassificationPropertyId = "property-1",
                    Right = PropertyRight.Read
                }
            ]
        };

        var committedAccessRights = new List<AccessRight>
        {
            new("generated-id", "Property One", "classification-1", userGroupId, useCaseId, "property-1", PropertyRight.Read)
        };

        var committedAccessRightDtos = new List<AccessRightDTO>
        {
            new()
            {
                Id = "generated-id",
                Name = "Property One",
                GuidelineClassificationId = "classification-1",
                UserGroupId = userGroupId,
                UseCaseId = useCaseId,
                GuidlineClassificationPropertyId = "property-1",
                Right = PropertyRight.Read
            }
        };

        _accessRightsService
            .CommitAccessRightsAsync(useCaseId, userGroupId, Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<CancellationToken>())
            .Returns(committedAccessRights);
        _mapper.Map<List<AccessRightDTO>>(committedAccessRights).Returns(committedAccessRightDtos);

        // Act
        var result = await _controller.CommitAccessRightsAsync(useCaseId, userGroupId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(committedAccessRightDtos, okResult.Value);

        await _accessRightsService.Received(1).CommitAccessRightsAsync(
            useCaseId,
            userGroupId,
            Arg.Is<IEnumerable<AccessRight>>(accessRights =>
                accessRights.Count() == 1
                && accessRights.Single().GuidlineClassificationPropertyId == "property-1"
                && accessRights.Single().UseCaseId == useCaseId
                && accessRights.Single().UserGroupId == userGroupId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_RequestContainsMismatchedRouteScope_Then_ReturnsBadRequest()
    {
        // Arrange
        var useCaseId = Guid.NewGuid();
        var userGroupId = Guid.NewGuid();

        var request = new CommitAccessRightsRequestDTO
        {
            AccessRights =
            [
                new CommitAccessRightDTO
                {
                    Name = "Property One",
                    GuidelineClassificationId = "classification-1",
                    UserGroupId = Guid.NewGuid(),
                    UseCaseId = useCaseId,
                    GuidlineClassificationPropertyId = "property-1",
                    Right = PropertyRight.Read
                }
            ]
        };

        // Act
        var result = await _controller.CommitAccessRightsAsync(useCaseId, userGroupId, request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        await _accessRightsService.DidNotReceive().CommitAccessRightsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_ModelStateIsInvalid_Then_ReturnsBadRequest()
    {
        // Arrange
        var request = new CommitAccessRightsRequestDTO();
        _controller.ModelState.AddModelError("AccessRights", "AccessRights is required");

        // Act
        var result = await _controller.CommitAccessRightsAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
