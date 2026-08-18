// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AccessService.Infrastructure.Tests;

/// <summary>
/// Reusable PostgreSQL Testcontainers fixture for integration tests.
/// Starts a single PostgreSQL container per test class, runs migrations, and provides context creation.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<AccessRightDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var ctx = new AccessRightDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Creates a fresh DbContext for a test with the table pre-truncated.
    /// </summary>
    public async Task<AccessRightDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AccessRightDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        var context = new AccessRightDbContext(options);

        // Truncate with CASCADE to cleanly remove all child rows before test
        await context.Database.ExecuteSqlRawAsync("TRUNCATE guideline_version CASCADE");

        return context;
    }

    public async Task<DatabaseScope<TRepository>> CreateScopeAsync<TRepository>(Func<AccessRightDbContext, TRepository> repositoryFactory)
    {
        var context = await CreateContextAsync();
        return new DatabaseScope<TRepository>(context, repositoryFactory(context));
    }
}

public sealed class DatabaseScope<TRepository> : IAsyncDisposable
{
    public DatabaseScope(AccessRightDbContext context, TRepository repository)
    {
        Context = context;
        Repository = repository;
    }

    public AccessRightDbContext Context
    {
        get;
    }

    public TRepository Repository
    {
        get;
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
