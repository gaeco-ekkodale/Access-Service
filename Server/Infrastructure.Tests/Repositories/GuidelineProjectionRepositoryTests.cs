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
using AccessService.Infrastructure.Repositories;
using FluentAssertions;

namespace AccessService.Infrastructure.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="GuidelineProjectionRepository"/> using a real PostgreSQL instance.
/// Specifically verifies that <c>GetActiveVersionIdsAsync</c> raw SQL uses the <c>AS "Value"</c> column alias
/// required by EF Core's <c>SqlQueryRaw&lt;Guid&gt;</c> scalar projection.
/// </summary>
public class GuidelineProjectionRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public GuidelineProjectionRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private Task<GuidelineProjectionRepositoryHarness> CreateSutAsync()
    {
        return GuidelineProjectionRepositoryHarness.CreateAsync(_fixture);
    }

    [Fact]
    public async Task GetActiveVersionIdsAsync_EfCoreMappingViaValueAlias_ReturnsGuids()
    {
        // Arrange
        await using var sut = await CreateSutAsync();

        var version = MakeVersion("guidelines/ifc-4.3.json", DateTimeOffset.UtcNow.AddHours(-1));
        await sut.SeedAsync(version);

        // Act — verifies AS "Value" alias is understood by EF Core SqlQueryRaw<Guid>
        var result = await sut.GetActiveVersionIdsAsync();

        // Assert
        result.Should().ContainSingle(because: "exactly one version was inserted")
            .Which.Should().Be(version.Id, because: "EF Core must correctly map the 'Value' column alias to Guid");
    }

    [Fact]
    public async Task GetActiveVersionIdsAsync_MultipleVersionsPerObjectName_ReturnsMostRecentOnly()
    {
        // Arrange
        await using var sut = await CreateSutAsync();

        var objectName = "guidelines/ifc-4.3.json";
        var olderVersion = MakeVersion(objectName, processedAt: DateTimeOffset.UtcNow.AddHours(-3));
        var newerVersion = MakeVersion(objectName, processedAt: DateTimeOffset.UtcNow.AddHours(-1));

        await sut.SeedAsync(olderVersion, newerVersion);

        // Act
        var result = await sut.GetActiveVersionIdsAsync();

        // Assert — DISTINCT ON picks the row with the highest processed_at
        result.Should().ContainSingle(because: "only one active version per object_name");
        result.Should().Contain(newerVersion.Id, because: "the newer version is the active one");
        result.Should().NotContain(olderVersion.Id, because: "the older version is superseded");
    }

    [Fact]
    public async Task GetActiveVersionIdsAsync_MultipleDistinctObjectNames_ReturnsOneIdPerObject()
    {
        // Arrange
        await using var sut = await CreateSutAsync();

        var versionA = MakeVersion("guidelines/ifc-4.3.json", DateTimeOffset.UtcNow.AddHours(-2));
        var versionB = MakeVersion("guidelines/iso-16739.json", DateTimeOffset.UtcNow.AddHours(-1));

        await sut.SeedAsync(versionA, versionB);

        // Act
        var result = await sut.GetActiveVersionIdsAsync();

        // Assert
        result.Should().HaveCount(2, because: "one active version per distinct object_name");
        result.Should().Contain(versionA.Id);
        result.Should().Contain(versionB.Id);
    }

    [Fact]
    public async Task GetActiveVersionIdsAsync_NoVersions_ReturnsEmptyList()
    {
        // Arrange
        await using var sut = await CreateSutAsync();

        // Act
        var result = await sut.GetActiveVersionIdsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    private static GuidelineVersion MakeVersion(string objectName, DateTimeOffset processedAt)
    {
        return new GuidelineVersion
        {
            Id = Guid.NewGuid(),
            GuidelineId = Guid.NewGuid().ToString(),
            Name = "Test Guideline",
            ObjectName = objectName,
            BucketName = "test-bucket",
            Etag = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid(),
            EventTimestamp = processedAt.AddMinutes(-5),
            ProcessedAt = processedAt
        };
    }

    private sealed class GuidelineProjectionRepositoryHarness : IAsyncDisposable
    {
        private readonly DatabaseScope<GuidelineProjectionRepository> _scope;

        private GuidelineProjectionRepositoryHarness(DatabaseScope<GuidelineProjectionRepository> scope)
        {
            _scope = scope;
        }

        public static async Task<GuidelineProjectionRepositoryHarness> CreateAsync(PostgresFixture fixture)
        {
            var scope = await fixture.CreateScopeAsync(static context => new GuidelineProjectionRepository(context));
            return new GuidelineProjectionRepositoryHarness(scope);
        }

        public async Task SeedAsync(params GuidelineVersion[] versions)
        {
            await _scope.Context.GuidelineVersions.AddRangeAsync(versions);
            await _scope.Context.SaveChangesAsync();
        }

        public Task<List<Guid>> GetActiveVersionIdsAsync(CancellationToken cancellationToken = default)
        {
            return _scope.Repository.GetActiveVersionIdsAsync(cancellationToken);
        }

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
