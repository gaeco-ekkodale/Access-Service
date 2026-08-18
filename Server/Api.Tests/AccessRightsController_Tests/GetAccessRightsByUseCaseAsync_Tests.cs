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

public class GetAccessRightsByUseCaseAsync_Tests
{
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRightsController> _logger;
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly AccessRightsController _controller;

    public GetAccessRightsByUseCaseAsync_Tests()
    {
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<AccessRightsController>>();
        _accessRightsRepository = Substitute.For<IAccessRightsRepository>();
        _controller = new AccessRightsController(_logger, _mapper, null, _accessRightsRepository);
    }

    /// <summary>
    /// Test that GetAccessRightsByUseCaseAsync returns OK (200) with a list of access rights for the given use case ID.
    /// </summary>
    [Fact]
    public async Task GetAccessRightsByUseCaseAsync_ReturnsOk_WithListOfAccessRights()
    {
        // Arrange
        var accessRightsDb = new List<AccessRight>
        {
            new AccessRight(
                id: "1",
                name: "Test Right",
                guidelineClassificationId: "classificationId",
                userGroupId: Guid.NewGuid(),
                useCaseId: Guid.NewGuid(),
                guidlineClassificationPropertyId: "propertyId",
                right: PropertyRight.Read
            )
        };

        // Mock repository to return a list of access rights
        _accessRightsRepository.GetAccessRightsByUseCaseAsync("useCaseId").Returns(accessRightsDb);

        // Mock AutoMapper to map AccessRightDb to AccessRightDTO
        _mapper.Map<List<AccessRightDTO>>(accessRightsDb).Returns(new List<AccessRightDTO>
        {
            new AccessRightDTO { Id = "1", Name = "Test Right" }
        });

        // Act
        var result = await _controller.GetAccessRightsByUseCaseAsync("useCaseId");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedValue = Assert.IsType<List<AccessRightDTO>>(okResult.Value);
        Assert.Single(returnedValue);
        Assert.Equivalent(new AccessRightDTO { Id = "1", Name = "Test Right" }, returnedValue[0]);
    }

    /// <summary>
    /// Test that GetAccessRightsByUseCaseAsync returns OK (200) with an empty list when no access rights are found for the use case ID.
    /// </summary>
    [Fact]
    public async Task GetAccessRightsByUseCaseAsync_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _accessRightsRepository.GetAccessRightsByUseCaseAsync("useCaseId").Returns(new List<AccessRight>());

        // Mock AutoMapper to map empty AccessRightDb list to empty AccessRightDTO list
        _mapper.Map<List<AccessRightDTO>>(Arg.Any<IEnumerable<AccessRight>>()).Returns(new List<AccessRightDTO>());

        // Act
        var result = await _controller.GetAccessRightsByUseCaseAsync("useCaseId");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedValue = Assert.IsType<List<AccessRightDTO>>(okResult.Value);
        Assert.Empty(returnedValue);
    }

    /// <summary>
    /// Test that GetAccessRightsByUseCaseAsync throws an exception that would be caught by middleware, resulting in a 500 Internal Server Error.
    /// </summary>
    [Fact]
    public async Task GetAccessRightsByUseCaseAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        _accessRightsRepository.GetAccessRightsByUseCaseAsync("useCaseId").Returns<Task<IEnumerable<AccessRight>>>(x => throw new Exception("Test exception"));

        // Act
        Func<Task> action = async () => await _controller.GetAccessRightsByUseCaseAsync("useCaseId");

        // Assert
        await Assert.ThrowsAsync<Exception>(action);
    }
}