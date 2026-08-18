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

namespace AccessService.Api.Tests.AccessRightsController_Tests;

/// <summary>
/// Unit tests for the UpdateAccessRightsAsync method in AccessRightsController.
/// </summary>
public class UpdateAccessRightsAsync_Tests
{
    private readonly IAccessRightsService _accessRightsService;
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRightsController> _logger;
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly AccessRightsController _controller;

    /// <summary>
    /// Initializes the test class, setting up the necessary mock dependencies.
    /// </summary>
    public UpdateAccessRightsAsync_Tests()
    {
        _accessRightsService = Substitute.For<IAccessRightsService>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<AccessRightsController>>();
        _accessRightsRepository = Substitute.For<IAccessRightsRepository>();
        _controller = new AccessRightsController(_logger, _mapper, _accessRightsService, _accessRightsRepository);
    }

    /// <summary>
    /// Test that UpdateAccessRightsAsync returns OK (200) when valid access rights are provided.
    /// </summary>
    [Fact]
    public async Task UpdateAccessRightsAsync_ReturnsOk_WhenAccessRightsAreValid()
    {
        // Arrange
        var accessRightsDto = new List<AccessRightDTO>
        {
            new AccessRightDTO {
                Id = "1",
                Name = "Test Right",
                GuidelineClassificationId = "gcid-1",
                UserGroupId = Guid.NewGuid(),
                UseCaseId = Guid.NewGuid(),
                GuidlineClassificationPropertyId = "gcpid-1",
                Right = PropertyRight.Read
            }
        };
        var accessRightEntity = new AccessRight(
            accessRightsDto[0].Id,
            accessRightsDto[0].Name,
            accessRightsDto[0].GuidelineClassificationId,
            accessRightsDto[0].UserGroupId,
            accessRightsDto[0].UseCaseId,
            accessRightsDto[0].GuidlineClassificationPropertyId,
            accessRightsDto[0].Right
        );

        _mapper.Map<AccessRight>(Arg.Any<AccessRightDTO>()).Returns(accessRightEntity);

        // Act
        var result = await _controller.UpdateAccessRightsAsync(accessRightsDto);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Ensure mediator was called
        await _accessRightsService.Received().UpdateAccessRightAsync(Arg.Any<AccessRight>());
    }

    /// <summary>
    /// Test that UpdateAccessRightsAsync throws an exception that would be caught by middleware, resulting in a 500 Internal Server Error.
    /// </summary>
    [Fact]
    public async Task UpdateAccessRightsAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var accessRightsDto = new List<AccessRightDTO>
        {
            new AccessRightDTO {
                Id = "1",
                Name = "Test Right",
                GuidelineClassificationId = "gcid-1",
                UserGroupId = Guid.NewGuid(),
                UseCaseId = Guid.NewGuid(),
                GuidlineClassificationPropertyId = "gcpid-1",
                Right = PropertyRight.Read
            }
        };

        // Simulate an exception during mapping
        _mapper.When(x => x.Map<AccessRight>(Arg.Any<AccessRightDTO>())).Do(x => { throw new Exception("Test Exception"); });

        // Act
        Func<Task> action = async () => await _controller.UpdateAccessRightsAsync(accessRightsDto);

        // Assert
        await Assert.ThrowsAsync<Exception>(action);
    }

    /// <summary>
    /// Test that UpdateAccessRightsAsync returns 400 Bad Request when invalid access rights are provided.
    /// </summary>
    [Fact]
    public async Task UpdateAccessRightsAsync_ReturnsBadRequest_WhenAccessRightsAreInvalid()
    {
        // Arrange
        var accessRightsDto = new List<AccessRightDTO>
        {
            new AccessRightDTO {
                Id = null,
                Name = "",
                GuidelineClassificationId = null,
                UserGroupId = Guid.Empty,
                UseCaseId = Guid.Empty,
                GuidlineClassificationPropertyId = null,
                Right = default
            }
        };

        _controller.ModelState.AddModelError("Id", "Id is required");
        _controller.ModelState.AddModelError("Name", "Name is required");
        _controller.ModelState.AddModelError("GuidelineClassificationId", "GuidelineClassificationId is required");
        _controller.ModelState.AddModelError("UserGroupId", "UserGroupId is required");
        _controller.ModelState.AddModelError("UseCaseId", "UseCaseId is required");
        _controller.ModelState.AddModelError("GuidlineClassificationPropertyId", "GuidlineClassificationPropertyId is required");
        _controller.ModelState.AddModelError("Right", "Right is required");

        // Act
        var result = await _controller.UpdateAccessRightsAsync(accessRightsDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Test that UpdateAccessRightsAsync processes multiple valid access rights correctly.
    /// </summary>
    [Fact]
    public async Task UpdateAccessRightsAsync_ProcessesMultipleAccessRights_WhenValidAccessRightsAreProvided()
    {
        // Arrange
        var accessRightsDto = new List<AccessRightDTO>
        {
            new AccessRightDTO {
                Id = "1",
                Name = "Test Right 1",
                GuidelineClassificationId = "gcid-1",
                UserGroupId = Guid.NewGuid(),
                UseCaseId = Guid.NewGuid(),
                GuidlineClassificationPropertyId = "gcpid-1",
                Right = PropertyRight.Read
            },
            new AccessRightDTO {
                Id = "2",
                Name = "Test Right 2",
                GuidelineClassificationId = "gcid-2",
                UserGroupId = Guid.NewGuid(),
                UseCaseId = Guid.NewGuid(),
                GuidlineClassificationPropertyId = "gcpid-2",
                Right = PropertyRight.Write
            }
        };

        _mapper.Map<AccessRight>(Arg.Any<AccessRightDTO>())
            .Returns(x =>
            {
                var dto = (AccessRightDTO)x.Args()[0];
                return new AccessRight(
                    dto.Id,
                    dto.Name,
                    dto.GuidelineClassificationId,
                    dto.UserGroupId,
                    dto.UseCaseId,
                    dto.GuidlineClassificationPropertyId,
                    dto.Right
                );
            });

        // Act
        var result = await _controller.UpdateAccessRightsAsync(accessRightsDto);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Ensure the service was called twice (once for each valid access right)
        await _accessRightsService.Received(2).UpdateAccessRightAsync(Arg.Any<AccessRight>());
    }

    /// <summary>
    /// Test that UpdateAccessRightsAsync returns OK (200) when no access rights are provided (empty list).
    /// </summary>
    [Fact]
    public async Task UpdateAccessRightsAsync_ReturnsOk_WhenNoAccessRightsProvided()
    {
        // Arrange
        var accessRightsDto = new List<AccessRightDTO>();

        // Act
        var result = await _controller.UpdateAccessRightsAsync(accessRightsDto);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Ensure no service calls were made
        await _accessRightsService.DidNotReceive().UpdateAccessRightAsync(Arg.Any<AccessRight>());
    }
}