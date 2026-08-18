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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AccessService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ClassificationsController : ControllerBase
{
    private readonly IClassificationsService _classificationsService;
    private readonly ILogger<ClassificationsController> _logger;

    public ClassificationsController(IClassificationsService classificationsService, ILogger<ClassificationsController> logger)
    {
        _classificationsService = classificationsService;
        _logger = logger;
    }

    /// <summary>
    /// An Endpoint to retrieve all classifications.
    /// </summary>
    /// <returns>A list of classifications.</returns>
    [ProducesResponseType(typeof(ClassificationsListSet), 200)]
    [HttpGet()]
    [SwaggerOperation(
            Summary = "Retrieve all classifications.",
            Description = "An Endpoint to retrieve all classifications.",
            OperationId = "GetClassifications",
            Tags = new[] { "Classifications", }
        )]
    public async Task<IActionResult> GetClassificationsAsync(CancellationToken cancellationToken)
    {
        var response = await _classificationsService.GetClassificationsAsync(cancellationToken);
        return Ok(response ?? new ClassificationsListSet());
    }

    /// <summary>
    /// An Endpoint to retrieve the Properties of a specific classification.
    /// <param name="classificationId">The Id of the classification whose properties are to be retrieved.</param>
    /// </summary>
    /// <returns>A List of classification properties.</returns>
    [ProducesResponseType(typeof(List<ClassificationPropertyDTO>), 200)]
    [ProducesResponseType(typeof(BadRequestResult), 400)]
    [ProducesResponseType(typeof(NotFoundResult), 404)]
    [HttpGet("classification/{classificationId}/properties")]
    [SwaggerOperation(
            Summary = "Retrieve properties of certain classification.",
            Description = "An Endpoint to retrieve the Properties of a specific classification.",
            OperationId = "GetPropertiesByClassificationID",
            Tags = new[] { "Classifications", }
        )]
    public async Task<IActionResult> GetPropertiesByClassificationID(string classificationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(classificationId))
        {
            return BadRequest("Invalid classification ID.");
        }

        var properties = await _classificationsService.GetPropertiesByClassificationIdAsync(classificationId, cancellationToken);
        return Ok(properties);
    }
}
