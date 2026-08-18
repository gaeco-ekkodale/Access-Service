// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Configs.AutoMapperConf;
using AccessService.Api.Consumer;
using AccessService.Api.Extensions;
using AccessService.Api.Middleware;
using AccessService.Api.Options;
using AccessService.Api.Producer;
using AccessService.Api.Services;
using AccessService.Domain.Repositories;
using AccessService.Infrastructure;
using AccessService.Infrastructure.Repositories;
using AutoMapper;
using Ekkodale.TelemetryExtensions;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Minio;
using System.Reflection;
using System.Text.Json.Serialization;
using Throw;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

TelemetryOptions? telOpts = configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>();
telOpts.ThrowIfNull("OpenTelemetry configuration is missing");
builder.AddMonitoring(telOpts, Assembly.GetExecutingAssembly());

// FluentValidation
// Register validators from assembly for configuration validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

// Add Configuration
// Load and validate application configuration sections
builder.Services.AddAppConfiguration(builder.Configuration);

var config = new MapperConfiguration((cfg) =>
{
    cfg.AddProfile<AccessRightProfile>();
    cfg.AddProfile<PropertyRightProfile>();
    cfg.AddProfile<UserGroupProfile>();
});
var mapper = new Mapper(config);

// Add services to the container.
builder.Services.AddSingleton<IMapper>(mapper);
builder.Services.AddHttpClient();
builder.Services.AddPostgres();
builder.Services.AddScoped<IAccessRightsRepository, AccessRightsRepository>();
builder.Services.AddScoped<IUserGroupsRepository, UserGroupsRepository>();
builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
      });

builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();

// Add the UserGroupsSyncHostedService to periodically sync user groups every 8 hours
builder.Services.AddHostedService<UserGroupsSyncHostedService>();

builder.Services.AddEndpointsApiExplorer();

#region Authentication

builder.Services.AddKeycloakAuthentication(options =>
{
    configuration.GetSection("Keycloak").Bind(options);
});

#endregion Authentication

#region Swagger

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "AccessService API",
        Description = "An ASP.NET Core Web API for the access service",
    });
    options.EnableAnnotations();
});

#endregion Swagger

#region CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder
            .AllowAnyOrigin()  // Allowing any origin
            .AllowAnyMethod()  // Allowing any HTTP method
            .AllowAnyHeader()); // Allowing any header
});

#endregion CORS

builder.Services.Configure<MinioOptions>(configuration.GetSection("Minio"));
builder.Services.AddScoped<IMinioClient>(sp =>
{
    var minioOptions = configuration.GetSection("Minio").Get<MinioOptions>()!;
    return (IMinioClient)new MinioClient()
        .WithEndpoint(minioOptions.Address)
        .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
        .WithSSL(minioOptions.Address.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        .Build();
});

#region Services

builder.Services.AddScoped<IUseCaseGuidelineService, UseCaseGuidelineService>();
builder.Services.AddScoped<IAccessRightsService, AccessRightsService>();
builder.Services.AddScoped<IClassificationsService, ClassificationsService>();
builder.Services.AddScoped<IGuidelineProjectionRepository, GuidelineProjectionRepository>();
builder.Services.AddScoped<IGuidelineTransformationService, GuidelineTransformationService>();
builder.Services.AddHostedService<GuidelineConsumer>();

#endregion Services

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AccessRightDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation("Database Creation ensured.");
    }
    catch (Exception e)
    {
        logger.LogError(e, "Database Creation failed!");
        Console.WriteLine(e.Message);
    }

    try
    {
        var userGroupsRepository = services.GetRequiredService<IUserGroupsRepository>();
        await userGroupsRepository.GetKeycloakGroups();
        logger.LogInformation("User groups initialization from Keycloak completed.");
    }
    catch (Exception e)
    {
        logger.LogError(e, "User groups initialization from Keycloak failed!");
        Console.WriteLine(e.Message);
    }
}

app.UseRouting();

app.UseCors("AllowAllOrigins");

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Respect reverse proxy headers (Traefik) for scheme/host
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
        var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
        var basePath = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? httpReq.PathBase.Value ?? string.Empty;

        swagger.Servers = [
            new OpenApiServer { Url = $"{scheme}://{host}{basePath}" }
        ];
    });
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
