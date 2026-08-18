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

public class GetAccessRightsByUseCaseUserGroupClassificationAsync_Tests
{
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRightsController> _logger;
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly AccessRightsController _controller;

    public GetAccessRightsByUseCaseUserGroupClassificationAsync_Tests()
    {
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<AccessRightsController>>();
        _accessRightsRepository = Substitute.For<IAccessRightsRepository>();
        _controller = new AccessRightsController(_logger, _mapper, null, _accessRightsRepository);
    }

    /// <summary>
    /// Test that GetAccessRightsByUseCaseUserGroupClassificationAsync returns OK (200) with a list of access rights for the given use case, user group, and classification ID.
    /// </summary>
    [Fact]
    public async Task GetAccessRightsByUseCaseUserGroupClassificationAsync_ReturnsOk_WithListOfAccessRights()
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
        _accessRightsRepository.GetAccessRightsByUseCaseUserGroupClassificationAsync("useCaseId", "userGroupId", "classificationId")
            .Returns(accessRightsDb);

        // Mock AutoMapper to map AccessRightDb to AccessRightDTO
        _mapper.Map<List<AccessRightDTO>>(accessRightsDb).Returns(new List<AccessRightDTO>
        {
            new AccessRightDTO { Id = "1", Name = "Test Right" }
        });

        // Act
        var result = await _controller.GetAccessRightsByUseCaseUserGroupClassificationAsync("useCaseId", "userGroupId", "classificationId");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedValue = Assert.IsType<List<AccessRightDTO>>(okResult.Value);
        Assert.Single(returnedValue);
        Assert.Equivalent(new AccessRightDTO { Id = "1", Name = "Test Right" }, returnedValue[0]);
    }

    /// <summary>
    /// Test that GetAccessRightsByUseCaseUserGroupClassificationAsync returns OK (200) with an empty list when no access rights are found for the given use case, user group, and classification ID.
    /// </summary>
    [Fact]
    public async Task GetAccessRightsByUseCaseUserGroupClassificationAsync_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _accessRightsRepository.GetAccessRightsByUseCaseUserGroupClassificationAsync("useCaseId", "userGroupId", "classificationId")
            .Returns(new List<AccessRight>());

        // Mock AutoMapper to map empty AccessRightDb list to empty AccessRightDTO list
        _mapper.Map<List<AccessRightDTO>>(Arg.Any<IEnumerable<AccessRight>>()).Returns(new List<AccessRightDTO>());

        // Act
        var result = await _controller.GetAccessRightsByUseCaseUserGroupClassificationAsync("useCaseId", "userGroupId", "classificationId");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedValue = Assert.IsType<List<AccessRightDTO>>(okResult.Value);
        Assert.Empty(returnedValue);
    }

    /// <summary>
    /// Test that GetAccessRightsByUseCaseUserGroupClassificationAsync throws an exception that would be caught by middleware, resulting in a 500 Internal Server Error.
    /// </summary>
    [Fact]
    public async Task GetAccessRightsByUseCaseUserGroupClassificationAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        _accessRightsRepository.GetAccessRightsByUseCaseUserGroupClassificationAsync("useCaseId", "userGroupId", "classificationId")
            .Returns<Task<IEnumerable<AccessRight>>>(x => throw new Exception("Test exception"));

        // Act
        Func<Task> action = async () => await _controller.GetAccessRightsByUseCaseUserGroupClassificationAsync("useCaseId", "userGroupId", "classificationId");

        // Assert
        await Assert.ThrowsAsync<Exception>(action);
    }
}