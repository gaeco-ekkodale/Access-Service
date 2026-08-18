// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Options;
using FluentValidation;

namespace AccessService.Api.Validators;

/// <summary>
/// Represents a validator for the <see cref="KeycloakOptions"/>.
/// </summary>
public class KeycloakOptionsValidator : AbstractValidator<KeycloakOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeycloakOptionsValidator"/> class
    /// and sets up the validation rules.
    /// </summary>
    public KeycloakOptionsValidator()
    {
        RuleFor(x => x.ServerUrl).NotEmpty().WithMessage("Keycloak Host is required");
        RuleFor(x => x.Realm).NotEmpty().WithMessage("Keycloak Authority is required");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("Keycloak Client-ID is required");
        RuleFor(x => x.ClientSecret).NotEmpty().WithMessage("Keycloak ClientSecret is required");
    }
}
