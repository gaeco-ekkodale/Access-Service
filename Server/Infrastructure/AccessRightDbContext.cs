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
using Microsoft.EntityFrameworkCore;

namespace AccessService.Infrastructure;

/// <summary>
/// Represents the database context for the Access Service, managing access rights, user groups, and outbox events.
/// </summary>
public class AccessRightDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the database set of <see cref="AccessRight"/> entities.
    /// </summary>
    public DbSet<AccessRight> AccessRights
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="OutboxEvent"/> entities for implementing the outbox pattern.
    /// </summary>
    public DbSet<OutboxEvent> OutboxEvents
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="UserGroup"/> entities.
    /// </summary>
    public DbSet<UserGroup> UserGroups
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="GuidelineVersion"/> entities.
    /// </summary>
    public DbSet<GuidelineVersion> GuidelineVersions
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="GuidelineClassification"/> entities.
    /// </summary>
    public DbSet<GuidelineClassification> GuidelineClassifications
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="GuidelinePropertySet"/> entities.
    /// </summary>
    public DbSet<GuidelinePropertySet> GuidelinePropertySets
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="GuidelineProperty"/> entities.
    /// </summary>
    public DbSet<GuidelineProperty> GuidelineProperties
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="GuidelineClassificationProperty"/> entities.
    /// </summary>
    public DbSet<GuidelineClassificationProperty> GuidelineClassificationProperties
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the database set of <see cref="KafkaDeadLetter"/> entities for poison message tracking.
    /// </summary>
    public DbSet<KafkaDeadLetter> KafkaDeadLetters
    {
        get; set;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessRightDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public AccessRightDbContext(DbContextOptions<AccessRightDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Creation setting for the use-case model to use Guid as the PK.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessRight>().HasKey(x => x.Id);
        modelBuilder.Entity<AccessRight>().ToTable("accessright");

        modelBuilder.Entity<OutboxEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<OutboxEvent>()
            .Property(p => p.Payload)
            .HasColumnType("text")
            .IsRequired(false);

        modelBuilder.Entity<UserGroup>().HasKey(x => x.Id);
        modelBuilder.Entity<UserGroup>().ToTable("usergroup");

        // GuidelineVersion
        modelBuilder.Entity<GuidelineVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_version");
            e.HasIndex(x => x.ServiceId).IsUnique().HasFilter("service_id IS NOT NULL");
            e.HasIndex(x => new { x.ObjectName, x.Etag }).IsUnique();
            e.HasIndex(x => x.ProcessedAt).IsDescending();
            e.HasIndex(x => new { x.ObjectName, x.ProcessedAt }).IsDescending(false, true);
            e.Property(x => x.MappingsJson).HasColumnType("text");
            e.Property(x => x.ComplexDataJson).HasColumnType("text");
            e.Property(x => x.DomainJson).HasColumnType("text");
            e.HasMany(x => x.Classifications).WithOne(x => x.GuidelineVersion).HasForeignKey(x => x.GuidelineVersionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PropertySets).WithOne(x => x.GuidelineVersion).HasForeignKey(x => x.GuidelineVersionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Properties).WithOne(x => x.GuidelineVersion).HasForeignKey(x => x.GuidelineVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        // GuidelineClassification
        modelBuilder.Entity<GuidelineClassification>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_classification");
            e.HasIndex(x => x.ClassificationId).IsUnique();
            e.HasIndex(x => x.Identifier);
            e.Property(x => x.RelationsJson).HasColumnType("text");
            e.HasMany(x => x.ClassificationProperties).WithOne(x => x.GuidelineClassification).HasForeignKey(x => x.GuidelineClassificationId).OnDelete(DeleteBehavior.Cascade);
        });

        // GuidelinePropertySet
        modelBuilder.Entity<GuidelinePropertySet>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_property_set");
            e.HasIndex(x => x.PropertySetId).IsUnique();
        });

        // GuidelineProperty
        modelBuilder.Entity<GuidelineProperty>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_property");
            e.HasIndex(x => x.PropertyId).IsUnique();
            e.Property(x => x.ExtraJson).HasColumnType("text");
        });

        // GuidelineClassificationProperty
        modelBuilder.Entity<GuidelineClassificationProperty>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_classification_property");
            e.HasIndex(x => new { x.GuidelineClassificationId, x.ClassificationPropertyId }).IsUnique();
            e.Property(x => x.AssignmentJson).HasColumnType("text");
        });

        // KafkaDeadLetter
        modelBuilder.Entity<KafkaDeadLetter>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("kafka_dead_letter");
            e.Property(x => x.Value).HasColumnType("text");
            e.HasIndex(x => x.FailedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}
