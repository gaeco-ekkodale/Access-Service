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
public class GuidelinesController : ControllerBase
{
    private readonly IClassificationsService _classificationsService;
    private readonly ILogger<GuidelinesController> _logger;

    public GuidelinesController(IClassificationsService classificationsService, ILogger<GuidelinesController> logger)
    {
        _classificationsService = classificationsService;
        _logger = logger;
    }

    /// <summary>
    /// An Endpoint to retrieve all available guidelines.
    /// </summary>
    [ProducesResponseType(typeof(List<GuidelineDTO>), 200)]
    [HttpGet]
    [SwaggerOperation(
        Summary = "Retrieve all guidelines.",
        Description = "An Endpoint to retrieve all available guidelines.",
        OperationId = "GetGuidelines",
        Tags = new[] { "Guidelines" }
    )]
    public async Task<IActionResult> GetGuidelinesAsync(CancellationToken cancellationToken)
    {
        var result = await _classificationsService.GetGuidelinesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// An Endpoint to retrieve all classifications of a specific guideline.
    /// </summary>
    /// <param name="guidelineId">The ID or Identifier of the guideline.</param>
    [ProducesResponseType(typeof(ClassificationsListSet), 200)]
    [ProducesResponseType(typeof(BadRequestResult), 400)]
    [HttpGet("{guidelineId}/classifications")]
    [SwaggerOperation(
        Summary = "Retrieve classifications of a specific guideline.",
        Description = "An Endpoint to retrieve all classifications belonging to a specific guideline.",
        OperationId = "GetClassificationsByGuideline",
        Tags = new[] { "Guidelines" }
    )]
    public async Task<IActionResult> GetClassificationsByGuidelineAsync(string guidelineId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(guidelineId))
            return BadRequest("Invalid guideline ID.");

        var result = await _classificationsService.GetClassificationsByGuidelineAsync(guidelineId, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// An Endpoint to retrieve the properties and property sets of a specific classification within a guideline.
    /// </summary>
    /// <param name="guidelineId">The ID or Identifier of the guideline.</param>
    /// <param name="classificationId">The ID or Identifier of the classification.</param>
    [ProducesResponseType(typeof(ClassificationDetailDTO), 200)]
    [ProducesResponseType(typeof(BadRequestResult), 400)]
    [HttpGet("{guidelineId}/classifications/{classificationId}/detail")]
    [SwaggerOperation(
        Summary = "Retrieve property sets and properties of a classification.",
        Description = "An Endpoint to retrieve all property sets (with their properties) and standalone properties of a specific classification within a guideline.",
        OperationId = "GetClassificationDetail",
        Tags = new[] { "Guidelines" }
    )]
    public async Task<IActionResult> GetClassificationDetailAsync(string guidelineId, string classificationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(guidelineId) || string.IsNullOrEmpty(classificationId))
            return BadRequest("Invalid guideline or classification ID.");

        var result = await _classificationsService.GetClassificationDetailByGuidelineAsync(guidelineId, classificationId, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
