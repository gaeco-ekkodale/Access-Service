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
using AccessService.Domain.Models;
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccessService.Api.Tests.AccessRightsController_Tests;

public class GetAccessRightAsync_Tests
{
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRightsController> _logger;
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly AccessRightsController _controller;

    public GetAccessRightAsync_Tests()
    {
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<AccessRightsController>>();
        _accessRightsRepository = Substitute.For<IAccessRightsRepository>();
        _controller = new AccessRightsController(_logger, _mapper, null, _accessRightsRepository);
    }

    /// <summary>
    /// Test that GetAccessRightAsync returns OK (200) with the access right when valid ID is provided.
    /// </summary>
    [Fact]
    public async Task GetAccessRightAsync_ReturnsOk_WithAccessRight()
    {
        // Arrange
        var accessRightDb = new AccessRight(
            id: "1",
            name: "Test Right",
            guidelineClassificationId: "classificationId",
            userGroupId: Guid.NewGuid(),
            useCaseId: Guid.NewGuid(),
            guidlineClassificationPropertyId: "propertyId",
            right: PropertyRight.Read
        );

        // Mock repository to return AccessRightDb
        _accessRightsRepository.GetAccessRightAsync("1").Returns(Task.FromResult(accessRightDb));

        // Mock AutoMapper to map AccessRightDb to AccessRightDTO
        _mapper.Map<AccessRightDTO>(accessRightDb).Returns(new AccessRightDTO { Id = "1", Name = "Test Right" });

        // Act
        var result = await _controller.GetAccessRightAsync("1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedValue = Assert.IsType<AccessRightDTO>(okResult.Value);
        Assert.Equivalent(new AccessRightDTO { Id = "1", Name = "Test Right" }, returnedValue);
    }

    /// <summary>
    /// Test that GetAccessRightAsync returns NotFound (404) when no access right is found with the given ID.
    /// </summary>
    [Fact]
    public async Task GetAccessRightAsync_ReturnsNotFound_WhenAccessRightNotFound()
    {
        // Arrange
        _accessRightsRepository.GetAccessRightAsync("1").Returns(Task.FromResult<AccessRight>(null));

        // Act
        var result = await _controller.GetAccessRightAsync("1");

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("Access right not found", notFoundResult.Value);
    }

    /// <summary>
    /// Test that GetAccessRightAsync throws an exception that would be caught by middleware, resulting in a 500 Internal Server Error.
    /// </summary>
    [Fact]
    public async Task GetAccessRightAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        _accessRightsRepository.GetAccessRightAsync("1").Returns<Task<AccessRight>>(x => throw new Exception("Test exception"));

        // Act
        Func<Task> action = async () => await _controller.GetAccessRightAsync("1");

        // Assert
        await Assert.ThrowsAsync<Exception>(action);
    }

    /// <summary>
    /// Test that GetAccessRightAsync returns BadRequest (400) when an invalid ID is provided.
    /// </summary>
    [Fact]
    public async Task GetAccessRightAsync_ReturnsBadRequest_WhenInvalidIdProvided()
    {
        // Act
        var result = await _controller.GetAccessRightAsync(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Equal("Invalid access right ID", badRequestResult.Value);
    }
}