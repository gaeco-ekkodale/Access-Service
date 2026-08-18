// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.DTOs;
using AccessService.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccessService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UserGroupsController : ControllerBase
{
    private readonly IUserGroupsRepository _userGroupsRepository;
    private readonly IMapper _mapper;

    public UserGroupsController(IUserGroupsRepository userGroupsRepository, IMapper mapper)
    {
        _userGroupsRepository = userGroupsRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// API call to retrieve the IDs of all stored User Groups along with their associated names.
    /// This endpoint returns the locally synchronized UserGroupIds and their corresponding names, allowing clients to identify
    /// available user groups within the system without waiting on a live Keycloak roundtrip.
    /// </summary>
    /// <returns>A list of UserGroupIds with their corresponding names.</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserGroupDTO>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [SwaggerOperation(
            Description = "API call to retrieve the IDs and names of all stored User Groups. "
                        + "This endpoint provides a list of UserGroupIds and their corresponding names, "
                        + "which can be used for further operations.",
            OperationId = "GetKeycloakGroups",
            Tags = new[] { "UserGroups", }
        )]
    public async Task<ActionResult<IEnumerable<UserGroupDTO>>> GetKeycloakGroupsAsync()
    {
        var groups = await _userGroupsRepository.GetAllUserGroupsAsync();
        var groupDtos = _mapper.Map<IEnumerable<UserGroupDTO>>(groups) ?? [];
        return Ok(groupDtos);
    }

    /// <summary>
    /// API call to retrieve all User Groups associated with a specific user in Keycloak.
    /// This endpoint requires a UserId (not a UserGroupId) to be provided, which can typically be found in Keycloak. 
    /// The response will contain detailed information about all User Groups that the specified user belongs to.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom to retrieve the associated User Groups.</param>
    /// <returns>A list of User Groups associated with the specified user.</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserGroupDTO>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("user/{userId}")]
    [SwaggerOperation(
            Description = "API call to retrieve all User Groups associated with a specific user in Keycloak. "
                        + "Provide a UserId (not a UserGroupId) to get information on all User Groups "
                        + "that the specified user is part of. The UserId can be found in Keycloak.",
            OperationId = "GetKeycloakGroupsByUser",
            Tags = new[] { "UserGroups", }
        )]
    public async Task<ActionResult<IEnumerable<UserGroupDTO>>> GetKeycloakGroupsByUserAsync(Guid userId)
    {
        var groups = await _userGroupsRepository.GetKeycloakGroupsByUserId(userId);
        var groupDtos = _mapper.Map<IEnumerable<UserGroupDTO>>(groups) ?? [];
        return Ok(groupDtos);
    }
}