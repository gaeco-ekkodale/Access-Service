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

public class GuidelineClassificationPropertyBuilder
{
    private readonly Faker _faker = new();

    private Guid _id;
    private string _classificationPropertyId;
    private string _propertyId;
    private string? _propertySetId;
    private bool _isRequired;
    private int _sortNumber;
    private bool _isReadonly;
    private string? _defaultValue;
    private string? _reference;
    private string? _assignmentJson;

    public GuidelineClassificationPropertyBuilder()
    {
        _id = _faker.Random.Guid();
        _classificationPropertyId = _faker.Random.AlphaNumeric(10);
        _propertyId = _faker.Random.AlphaNumeric(10);
        _propertySetId = null;
        _isRequired = _faker.Random.Bool();
        _sortNumber = _faker.Random.Int(1, 100);
        _isReadonly = false;
        _defaultValue = null;
        _reference = null;
        _assignmentJson = null;
    }

    public GuidelineClassificationPropertyBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithClassificationPropertyId(string id)
    {
        _classificationPropertyId = id;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithPropertyId(string id)
    {
        _propertyId = id;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithPropertySetId(string? id)
    {
        _propertySetId = id;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithIsRequired(bool value)
    {
        _isRequired = value;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithSortNumber(int value)
    {
        _sortNumber = value;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithIsReadonly(bool value)
    {
        _isReadonly = value;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithDefaultValue(string? value)
    {
        _defaultValue = value;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithReference(string? value)
    {
        _reference = value;
        return this;
    }
    public GuidelineClassificationPropertyBuilder WithAssignmentJson(string? json)
    {
        _assignmentJson = json;
        return this;
    }

    public GuidelineClassificationProperty Build() => new()
    {
        Id = _id,
        ClassificationPropertyId = _classificationPropertyId,
        PropertyId = _propertyId,
        PropertySetId = _propertySetId,
        IsRequired = _isRequired,
        SortNumber = _sortNumber,
        IsReadonly = _isReadonly,
        DefaultValue = _defaultValue,
        Reference = _reference,
        AssignmentJson = _assignmentJson
    };
}
