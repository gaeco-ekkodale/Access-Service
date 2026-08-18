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

public class GuidelineClassificationBuilder
{
    private readonly Faker _faker = new();

    private Guid _id;
    private Guid _guidelineVersionId;
    private string _classificationId;
    private string _name;
    private string? _identifier;
    private string? _code;
    private string? _description;
    private string? _status;
    private readonly List<GuidelineClassificationProperty> _classificationProperties = [];

    public GuidelineClassificationBuilder()
    {
        _id = _faker.Random.Guid();
        _guidelineVersionId = _faker.Random.Guid();
        _classificationId = _faker.Random.AlphaNumeric(10);
        _name = _faker.Commerce.Department();
        _identifier = _faker.Lorem.Slug();
        _code = _faker.Random.AlphaNumeric(4).ToUpper();
        _description = _faker.Lorem.Sentence();
        _status = "Active";
    }

    public GuidelineClassificationBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    public GuidelineClassificationBuilder WithVersionId(Guid id)
    {
        _guidelineVersionId = id;
        return this;
    }
    public GuidelineClassificationBuilder WithClassificationId(string id)
    {
        _classificationId = id;
        return this;
    }
    public GuidelineClassificationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public GuidelineClassificationBuilder WithIdentifier(string? identifier)
    {
        _identifier = identifier;
        return this;
    }
    public GuidelineClassificationBuilder WithCode(string? code)
    {
        _code = code;
        return this;
    }
    public GuidelineClassificationBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }

    public GuidelineClassificationBuilder WithClassificationProperty(GuidelineClassificationProperty cp)
    {
        _classificationProperties.Add(cp);
        return this;
    }

    public GuidelineClassificationBuilder WithClassificationProperties(params GuidelineClassificationProperty[] cps)
    {
        _classificationProperties.AddRange(cps);
        return this;
    }

    public GuidelineClassification Build()
    {
        var cls = new GuidelineClassification
        {
            Id = _id,
            GuidelineVersionId = _guidelineVersionId,
            ClassificationId = _classificationId,
            Name = _name,
            Identifier = _identifier,
            Code = _code,
            Description = _description,
            Status = _status
        };

        foreach (var cp in _classificationProperties)
            cls.ClassificationProperties.Add(cp);

        return cls;
    }
}
