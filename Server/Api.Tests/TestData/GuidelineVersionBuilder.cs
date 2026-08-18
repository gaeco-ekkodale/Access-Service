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

public class GuidelineVersionBuilder
{
    private readonly Faker _faker = new();

    private Guid _id;
    private string _guidelineId;
    private string _name;
    private string? _identifier;
    private string? _description;
    private string? _version;
    private string _objectName;
    private string _bucketName;
    private string _etag;
    private Guid _correlationId;
    private DateTimeOffset _eventTimestamp;
    private DateTimeOffset _processedAt;
    private string? _domainJson;
    private string? _mappingsJson;
    private string? _complexDataJson;

    public GuidelineVersionBuilder()
    {
        _id = _faker.Random.Guid();
        _guidelineId = _faker.Random.AlphaNumeric(15);
        _name = _faker.Commerce.ProductName();
        _identifier = _faker.Lorem.Slug();
        _description = _faker.Lorem.Sentence();
        _version = $"{_faker.Random.Int(1, 5)}.{_faker.Random.Int(0, 9)}";
        _objectName = $"{_faker.System.FileName("json")}";
        _bucketName = _faker.Lorem.Slug(1);
        _etag = _faker.Random.Hash();
        _correlationId = _faker.Random.Guid();
        _eventTimestamp = _faker.Date.RecentOffset();
        _processedAt = _faker.Date.RecentOffset();
        _domainJson = """{"ID":"domain-1","Name":"TestDomain","Identifier":"test-domain","Description":"A domain","Status":"Active","Version":"1.0"}""";
        _mappingsJson = null;
        _complexDataJson = null;
    }

    public GuidelineVersionBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    public GuidelineVersionBuilder WithGuidelineId(string id)
    {
        _guidelineId = id;
        return this;
    }
    public GuidelineVersionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public GuidelineVersionBuilder WithIdentifier(string? identifier)
    {
        _identifier = identifier;
        return this;
    }
    public GuidelineVersionBuilder WithDescription(string? desc)
    {
        _description = desc;
        return this;
    }
    public GuidelineVersionBuilder WithVersion(string? version)
    {
        _version = version;
        return this;
    }
    public GuidelineVersionBuilder WithObjectName(string name)
    {
        _objectName = name;
        return this;
    }
    public GuidelineVersionBuilder WithBucketName(string name)
    {
        _bucketName = name;
        return this;
    }
    public GuidelineVersionBuilder WithEtag(string etag)
    {
        _etag = etag;
        return this;
    }
    public GuidelineVersionBuilder WithDomainJson(string? json)
    {
        _domainJson = json;
        return this;
    }
    public GuidelineVersionBuilder WithMappingsJson(string? json)
    {
        _mappingsJson = json;
        return this;
    }
    public GuidelineVersionBuilder WithComplexDataJson(string? json)
    {
        _complexDataJson = json;
        return this;
    }

    public GuidelineVersion Build() => new()
    {
        Id = _id,
        GuidelineId = _guidelineId,
        Name = _name,
        Identifier = _identifier,
        Description = _description,
        Version = _version,
        ObjectName = _objectName,
        BucketName = _bucketName,
        Etag = _etag,
        CorrelationId = _correlationId,
        EventTimestamp = _eventTimestamp,
        ProcessedAt = _processedAt,
        DomainJson = _domainJson,
        MappingsJson = _mappingsJson,
        ComplexDataJson = _complexDataJson
    };
}
