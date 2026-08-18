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
using AccessService.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccessService.Api.Tests.UserGroupsController_Tests;

public class GetKeycloakGroupsByUserAsync_Tests
{
    private readonly IUserGroupsRepository _userGroupsRepository;
    private readonly UserGroupsController _controller;
    private readonly IMapper _mapper;

    public GetKeycloakGroupsByUserAsync_Tests()
    {
        _userGroupsRepository = Substitute.For<IUserGroupsRepository>();
        _mapper = Substitute.For<IMapper>();
        _controller = new UserGroupsController(_userGroupsRepository, _mapper);
    }

    /// <summary>
    /// Tests that the GetKeycloakGroupsByUserAsync method returns a status 200 OK with a list of user groups for a specific user when the groups exist.
    /// </summary>
    /// <returns>Asynchronous task representing the test.</returns>
    [Fact]
    public async Task GetKeycloakGroupsByUserAsync_ReturnsOkWithGroups_WhenUserGroupsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userGroups = new List<UserGroup>
        {
            new UserGroup { Id = Guid.NewGuid(), Name = "Group1" }
        };
        var userGroupDtos = new List<UserGroupDTO>
        {
            new UserGroupDTO { Id = userGroups[0].Id, Name = "Group1" }
        };
        
        _userGroupsRepository.GetKeycloakGroupsByUserId(userId).Returns(Task.FromResult<IEnumerable<UserGroup>>(userGroups));
        _mapper.Map<IEnumerable<UserGroupDTO>>(userGroups).Returns(userGroupDtos);

        // Act
        var result = await _controller.GetKeycloakGroupsByUserAsync(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(userGroupDtos, okResult.Value);
    }

    /// <summary>
    /// Tests that the GetKeycloakGroupsByUserAsync method throws an exception that would be caught by middleware, resulting in a 500 Internal Server Error.
    /// </summary>
    /// <returns>Asynchronous task representing the test.</returns>
    [Fact]
    public async Task GetKeycloakGroupsByUserAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userGroupsRepository.GetKeycloakGroupsByUserId(userId)
            .Returns(Task.FromException<IEnumerable<UserGroup>>(new Exception("Test Exception")));

        // Act
        Func<Task> action = async () => await _controller.GetKeycloakGroupsByUserAsync(userId);

        // Assert
        await Assert.ThrowsAsync<Exception>(action);
    }
}