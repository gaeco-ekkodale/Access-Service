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
using AccessService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccessService.Api.Tests.ClassificationsController_Tests;

public class GetPropertiesByClassificationID_Tests
{
    private readonly IClassificationsService _classificationsService;
    private readonly ILogger<ClassificationsController> _logger;
    private readonly ClassificationsController _controller;

    public GetPropertiesByClassificationID_Tests()
    {
        _classificationsService = Substitute.For<IClassificationsService>();
        _logger = Substitute.For<ILogger<ClassificationsController>>();
        _controller = new ClassificationsController(_classificationsService, _logger);
    }

    /// <summary>
    /// Test that GetPropertiesByClassificationID returns BadRequest (400) when an invalid classification ID is provided.
    /// </summary>
    [Fact]
    public async Task GetPropertiesByClassificationID_ReturnsBadRequest_WhenClassificationIdIsInvalid()
    {
        // Arrange
        string invalidClassificationId = ""; // Simulating an invalid ID

        // Act
        var result = await _controller.GetPropertiesByClassificationID(invalidClassificationId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Equal("Invalid classification ID.", badRequestResult.Value);
    }
}