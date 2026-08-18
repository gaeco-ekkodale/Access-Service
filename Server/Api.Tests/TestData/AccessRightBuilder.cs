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
using AccessService.Domain.Models.Enums;
using Bogus;

namespace AccessService.Api.Tests.TestData;

public class AccessRightBuilder
{
    private readonly Faker _faker = new();

    private string _id;
    private string _name;
    private string _classificationId;
    private Guid _userGroupId;
    private Guid _useCaseId;
    private string _classificationPropertyId;
    private PropertyRight _right;

    public AccessRightBuilder()
    {
        _id = _faker.Random.AlphaNumeric(20);
        _name = _faker.Lorem.Word();
        _classificationId = _faker.Random.AlphaNumeric(10);
        _userGroupId = _faker.Random.Guid();
        _useCaseId = _faker.Random.Guid();
        _classificationPropertyId = _faker.Random.AlphaNumeric(10);
        _right = PropertyRight.Read;
    }

    public AccessRightBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    public AccessRightBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public AccessRightBuilder WithClassificationId(string id)
    {
        _classificationId = id;
        return this;
    }
    public AccessRightBuilder WithUserGroupId(Guid id)
    {
        _userGroupId = id;
        return this;
    }
    public AccessRightBuilder WithUseCaseId(Guid id)
    {
        _useCaseId = id;
        return this;
    }
    public AccessRightBuilder WithClassificationPropertyId(string id)
    {
        _classificationPropertyId = id;
        return this;
    }
    public AccessRightBuilder WithRight(PropertyRight right)
    {
        _right = right;
        return this;
    }

    public AccessRight Build() => new(_id, _name, _classificationId, _userGroupId, _useCaseId, _classificationPropertyId, _right);
}
