// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Enums;

namespace AccessService.Api.DTOs;

public class ClassificationDetailDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PropertySetDetailDTO> PropertySets { get; set; } = [];
    public List<PropertyDetailDTO> StandaloneProperties { get; set; } = [];
}

public class PropertySetDetailDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PropertyDetailDTO> Properties { get; set; } = [];
}

public class PropertyDetailDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StorageType StorageType { get; set; }
}
