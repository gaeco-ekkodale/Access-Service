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

public class GetKeycloakGroupsAsync_Tests
{
    private readonly IUserGroupsRepository _userGroupsRepository;
    private readonly UserGroupsController _controller;
    private readonly IMapper _mapper;

    public GetKeycloakGroupsAsync_Tests()
    {
        _userGroupsRepository = Substitute.For<IUserGroupsRepository>();
        _mapper = Substitute.For<IMapper>();
        _controller = new UserGroupsController(_userGroupsRepository, _mapper);
    }

    /// <summary>
    /// Tests that the GetKeycloakGroupsAsync method returns a status 200 OK with a list of user groups when the groups exist.
    /// </summary>
    /// <returns>Asynchronous task representing the test.</returns>
    [Fact]
    public async Task GetKeycloakGroupsAsync_ReturnsOkWithGroups_WhenGroupsExist()
    {
        // Arrange
        var userGroups = new List<UserGroup>
        {
            new UserGroup { Id = Guid.NewGuid(), Name = "Group1" },
            new UserGroup { Id = Guid.NewGuid(), Name = "Group2" }
        };
        var userGroupsDtos = new List<UserGroupDTO>
        {
            new UserGroupDTO { Id = userGroups[0].Id, Name = "Group1" },
            new UserGroupDTO { Id = userGroups[1].Id, Name = "Group2" }
        };
        _userGroupsRepository.GetAllUserGroupsAsync().Returns(Task.FromResult<IEnumerable<UserGroup>>(userGroups));
        _mapper.Map<IEnumerable<UserGroupDTO>>(userGroups).Returns(userGroupsDtos);

        // Act
        var result = await _controller.GetKeycloakGroupsAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(userGroupsDtos, okResult.Value);
    }

    /// <summary>
    /// Tests that the GetKeycloakGroupsAsync method throws an exception that would be caught by middleware, resulting in a 500 Internal Server Error.
    /// </summary>
    /// <returns>Asynchronous task representing the test.</returns>
    [Fact]
    public async Task GetKeycloakGroupsAsync_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        _userGroupsRepository.GetAllUserGroupsAsync()
            .Returns(Task.FromException<IEnumerable<UserGroup>>(new Exception("Test Exception")));

        // Act
        Func<Task> action = async () => await _controller.GetKeycloakGroupsAsync();

        // Assert
        await Assert.ThrowsAsync<Exception>(action);
    }
}