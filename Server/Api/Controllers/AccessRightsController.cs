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
using AccessService.Api.Services;
using AccessService.Domain.Models;
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccessService.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ApiController]
public class AccessRightsController : ControllerBase
{
    private readonly ILogger<AccessRightsController> _logger;
    private readonly IMapper _mapper;
    private readonly IAccessRightsService _accessRightsService;
    private readonly IAccessRightsRepository _accessRightsRepository;

    /// <summary>
    /// Creates a new instance of the AccessRightsController.
    /// </summary>
    /// <param name="accessRightService">The service for access rights operations.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    /// <param name="mapper">The mapper to map between data types.</param>
    public AccessRightsController(ILogger<AccessRightsController> logger, IMapper mapper, IAccessRightsService accessRightsService, IAccessRightsRepository accessRightsRepository)
    {
        _logger = logger;
        _mapper = mapper;
        _accessRightsService = accessRightsService;
        _accessRightsRepository = accessRightsRepository;
    }

    /// <summary>
    /// Adds a new list of access rights to the database.
    /// </summary>
    /// <param name="accessRights">The access right details from the request body.</param>
    /// <returns>A status code indicating success or failure, along with the created access right list or an error message.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 201)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpPost]
    [SwaggerOperation(
            Description = "Adds a new list of access rights to the database.",
            OperationId = "CreateAccessRightsAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> CreateAccessRightsAsync([FromBody] List<AccessRightDTO> accessRights)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        foreach (var accessRight in accessRights)
        {
            var accessRightDbo = _mapper.Map<AccessRight>(accessRight);

            if (accessRightDbo.Right != PropertyRight.None)
            {
                await _accessRightsService.CreateAccessRightAsync(accessRightDbo);
            }
        }

        return Created("URL", accessRights);
    }

    /// <summary>
    /// Updates a list of access rights to the database.
    /// </summary>
    /// <param name="accessRights">The access right details from the request body.</param>
    /// <returns>A status code indicating success or failure, along with the created access right list or an error message.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpPut]
    [SwaggerOperation(
            Description = "Updates a list of access rights to the database.",
            OperationId = "UpdateAccessRightsAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> UpdateAccessRightsAsync([FromBody] List<AccessRightDTO> accessRights)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);  // Ensure this returns BadRequestObjectResult
        }

        foreach (var accessRight in accessRights)
        {
            var accessRightDbo = _mapper.Map<AccessRight>(accessRight);

            if (accessRightDbo.Right != PropertyRight.None)
            {
                await _accessRightsService.UpdateAccessRightAsync(accessRightDbo);
            }
        }

        return Ok();
    }

    /// <summary>
    /// Deletes a list of access rights to the database.
    /// </summary>
    /// <param name="accessRights">The access right details from the request body.</param>
    /// <returns>A status code indicating success or failure, along with the created access right list or an error message.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpDelete]
    [SwaggerOperation(
            Description = "Deletes a list of access rights to the database.",
            OperationId = "DeleteAccessRightsAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> DeleteAccessRightsAsync([FromBody] List<AccessRightDTO> accessRights)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);  // Ensure this is returning BadRequestObjectResult
        }

        foreach (var accessRight in accessRights)
        {
            var accessRightDbo = _mapper.Map<AccessRight>(accessRight);

            await _accessRightsService.DeleteAccessRightAsync(accessRightDbo.Id);
        }

        return Ok();
    }

    /// <summary>
    /// Commits the final list of access rights for the specified use case and user group.
    /// </summary>
    /// <param name="useCaseId">The use case identifier.</param>
    /// <param name="userGroupId">The user group identifier.</param>
    /// <param name="request">The final set of access rights that should remain persisted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted access rights after the commit.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpPut("usecase/{useCaseId}/usergroup/{userGroupId}/commit")]
    [SwaggerOperation(
            Description = "Commits the final list of access rights for the specified use case and user group.",
            OperationId = "CommitAccessRightsAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> CommitAccessRightsAsync(Guid useCaseId, Guid userGroupId, [FromBody] CommitAccessRightsRequestDTO request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request?.AccessRights == null)
        {
            return BadRequest("Commit request body is required.");
        }

        if (request.AccessRights.Any(accessRight => accessRight.UseCaseId != useCaseId || accessRight.UserGroupId != userGroupId))
        {
            return BadRequest("All access rights in the commit request must match the use case and user group in the route.");
        }

        var accessRightsToCommit = request.AccessRights.Select(accessRight => new AccessRight(
            accessRight.Id ?? string.Empty,
            accessRight.Name,
            accessRight.GuidelineClassificationId,
            accessRight.UserGroupId,
            accessRight.UseCaseId,
            accessRight.GuidlineClassificationPropertyId,
            accessRight.Right));

        var committedAccessRights = await _accessRightsService.CommitAccessRightsAsync(
            useCaseId,
            userGroupId,
            accessRightsToCommit,
            cancellationToken);

        var committedAccessRightDtos = _mapper.Map<List<AccessRightDTO>>(committedAccessRights);
        return Ok(committedAccessRightDtos);
    }

    /// <summary>
    /// Gets all access rights.
    /// </summary>
    /// <returns>A list of all access rights.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpGet()]
    [SwaggerOperation(
            Description = "Gets all access rights.",
            OperationId = "GetAllAccessRightsAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> GetAllAccessRightsAsync()
    {
        var accessRights = await _accessRightsRepository.GetAllAccessRightsAsync();
        var accessRightDtos = _mapper.Map<List<AccessRightDTO>>(accessRights);
        return Ok(accessRightDtos);
    }

    /// <summary>
    /// Gets an access right by its ID.
    /// </summary>
    /// <param name="id">The ID of the access right to be retrieved.</param>
    /// <returns>The access right with the specified ID.</returns>
    [ProducesResponseType(typeof(AccessRightDTO), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpGet("{id}")]
    [SwaggerOperation(
            Description = "Gets an access right by its ID.",
            OperationId = "GetAccessRightAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> GetAccessRightAsync(string id)
    {
        if (string.IsNullOrEmpty(id))  // Ensure invalid or null ID returns BadRequest
        {
            return BadRequest("Invalid access right ID");
        }

        var accessRight = await _accessRightsRepository.GetAccessRightAsync(id);
        if (accessRight == null)
        {
            return NotFound("Access right not found");
        }

        var accessRightDto = _mapper.Map<AccessRightDTO>(accessRight);
        return Ok(accessRightDto);
    }

    /// <summary>
    /// Gets access rights by use case ID.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <returns>A list of access rights for the specified use case.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpGet("usecase/{useCaseId}")]
    [SwaggerOperation(
            Description = "Gets access rights by use case ID.",
            OperationId = "GetAccessRightsByUseCaseAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> GetAccessRightsByUseCaseAsync(string useCaseId)
    {
        var accessRights = await _accessRightsRepository.GetAccessRightsByUseCaseAsync(useCaseId);
        var accessRightDtos = _mapper.Map<List<AccessRightDTO>>(accessRights);
        return Ok(accessRightDtos);
    }

    /// <summary>
    /// Gets access rights by user group ID.
    /// </summary>
    /// <param name="userGroupId">The user group ID.</param>
    /// <returns>A list of access rights for the specified user group.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpGet("usergroup/{userGroupId}")]
    [SwaggerOperation(
            Description = "Gets access rights by user group ID.",
            OperationId = "GetAccessRightsByUserGroupAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> GetAccessRightsByUserGroupAsync(string userGroupId)
    {
        var accessRights = await _accessRightsRepository.GetAccessRightsByUserGroupAsync(userGroupId);
        var accessRightDtos = _mapper.Map<List<AccessRightDTO>>(accessRights);
        return Ok(accessRightDtos);
    }

    /// <summary>
    /// Gets access rights by use case ID and user group ID.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <param name="userGroupId">The user group ID.</param>
    /// <returns>A list of access rights for the specified use case and user group.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpGet("usecase/{useCaseId}/usergroup/{userGroupId}")]
    [SwaggerOperation(
            Description = "Gets access rights by use case ID and user group ID.",
            OperationId = "GetAccessRightsByUseCaseUserGroupAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> GetAccessRightsByUseCaseUserGroupAsync(string useCaseId, string userGroupId)
    {
        var accessRights = await _accessRightsRepository.GetAccessRightsByUseCaseUserGroupAsync(useCaseId, userGroupId);
        var accessRightDtos = _mapper.Map<List<AccessRightDTO>>(accessRights);
        return Ok(accessRightDtos);
    }

    /// <summary>
    /// Gets access rights by use case ID, user group ID, and classification ID.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <param name="userGroupId">The user group ID.</param>
    /// <param name="classificationId">The classification ID.</param>
    /// <returns>A list of access rights for the specified use case, user group, and classification.</returns>
    [ProducesResponseType(typeof(List<AccessRightDTO>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    [HttpGet("usecase/{useCaseId}/usergroup/{userGroupId}/classification/{classificationId}")]
    [SwaggerOperation(
            Description = "Gets access rights by use case ID, user group ID, and classification ID.",
            OperationId = "GetAccessRightsByUseCaseUserGroupClassificationAsync",
            Tags = new[] { "AccessRights", }
        )]
    public async Task<IActionResult> GetAccessRightsByUseCaseUserGroupClassificationAsync(string useCaseId, string userGroupId, string classificationId)
    {
        var accessRights = await _accessRightsRepository.GetAccessRightsByUseCaseUserGroupClassificationAsync(useCaseId, userGroupId, classificationId);
        var accessRightDtos = _mapper.Map<List<AccessRightDTO>>(accessRights);
        return Ok(accessRightDtos);
    }
}
