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

public class GuidelinePropertySetBuilder
{
    private readonly Faker _faker = new();

    private Guid _id;
    private Guid _guidelineVersionId;
    private string _propertySetId;
    private string _name;
    private string? _identifier;
    private string? _description;
    private string? _status;

    public GuidelinePropertySetBuilder()
    {
        _id = _faker.Random.Guid();
        _guidelineVersionId = _faker.Random.Guid();
        _propertySetId = _faker.Random.AlphaNumeric(10);
        _name = _faker.Commerce.Categories(1)[0];
        _identifier = _faker.Lorem.Slug();
        _description = _faker.Lorem.Sentence();
        _status = "Active";
    }

    public GuidelinePropertySetBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    public GuidelinePropertySetBuilder WithVersionId(Guid id)
    {
        _guidelineVersionId = id;
        return this;
    }
    public GuidelinePropertySetBuilder WithPropertySetId(string id)
    {
        _propertySetId = id;
        return this;
    }
    public GuidelinePropertySetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public GuidelinePropertySetBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }

    public GuidelinePropertySet Build() => new()
    {
        Id = _id,
        GuidelineVersionId = _guidelineVersionId,
        PropertySetId = _propertySetId,
        Name = _name,
        Identifier = _identifier,
        Description = _description,
        Status = _status
    };
}
