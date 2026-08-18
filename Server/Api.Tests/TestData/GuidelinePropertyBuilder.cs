// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Domain.Models;
using Bogus;

namespace AccessService.Api.Tests.TestData;

public class GuidelinePropertyBuilder
{
    private readonly Faker _faker = new();

    private Guid _id;
    private Guid _guidelineVersionId;
    private string _propertyId;
    private string _name;
    private string? _identifier;
    private string? _description;
    private string _storageType;
    private string? _code;
    private string? _unitType;
    private string? _unitAbbreviation;
    private string? _status;
    private string? _propertyType;
    private string? _extraJson;

    public GuidelinePropertyBuilder()
    {
        _id = _faker.Random.Guid();
        _guidelineVersionId = _faker.Random.Guid();
        _propertyId = _faker.Random.AlphaNumeric(10);
        _name = _faker.Commerce.ProductMaterial();
        _identifier = _faker.Lorem.Slug();
        _description = _faker.Lorem.Sentence();
        _storageType = "String";
        _code = _faker.Random.AlphaNumeric(4).ToUpper();
        _unitType = null;
        _unitAbbreviation = null;
        _status = "Active";
        _propertyType = "PropertySimple";
        _extraJson = null;
    }

    public GuidelinePropertyBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    public GuidelinePropertyBuilder WithVersionId(Guid id)
    {
        _guidelineVersionId = id;
        return this;
    }
    public GuidelinePropertyBuilder WithPropertyId(string id)
    {
        _propertyId = id;
        return this;
    }
    public GuidelinePropertyBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public GuidelinePropertyBuilder WithIdentifier(string? identifier)
    {
        _identifier = identifier;
        return this;
    }
    public GuidelinePropertyBuilder WithStorageType(string type)
    {
        _storageType = type;
        return this;
    }
    public GuidelinePropertyBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }
    public GuidelinePropertyBuilder WithPropertyType(string? type)
    {
        _propertyType = type;
        return this;
    }
    public GuidelinePropertyBuilder WithExtraJson(string? json)
    {
        _extraJson = json;
        return this;
    }

    public GuidelineProperty Build() => new()
    {
        Id = _id,
        GuidelineVersionId = _guidelineVersionId,
        PropertyId = _propertyId,
        Name = _name,
        Identifier = _identifier,
        Description = _description,
        StorageType = _storageType,
        Code = _code,
        UnitType = _unitType,
        UnitAbbreviation = _unitAbbreviation,
        Status = _status,
        PropertyType = _propertyType,
        ExtraJson = _extraJson
    };
}
