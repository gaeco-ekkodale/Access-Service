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
using AccessService.Domain.Models.Enums;
using AutoMapper;
using AutoMapper.Extensions.EnumMapping;

namespace AccessService.Api.Configs.AutoMapperConf;

// Temporary mapping profile for PropertyRightDto until PortfolioBIM.Model is dissolved!
public class PropertyRightProfile : Profile
{
    public PropertyRightProfile()
    {
        CreateMap<PropertyRight, PropertyRightDto>()
            .ConvertUsingEnumMapping()
            .ReverseMap();
    }
}
