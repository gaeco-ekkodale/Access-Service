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
using Microsoft.Extensions.Logging;

namespace AccessService.Api.Tests.ClassificationsController_Tests;

public class GetClassificationsAsync_Tests
{
    private readonly IClassificationsService _classificationsService;
    private readonly ILogger<ClassificationsController> _logger;
    private readonly ClassificationsController _controller;

    public GetClassificationsAsync_Tests()
    {
        _classificationsService = Substitute.For<IClassificationsService>();
        _logger = Substitute.For<ILogger<ClassificationsController>>();
        _controller = new ClassificationsController(_classificationsService, _logger);
    }
}