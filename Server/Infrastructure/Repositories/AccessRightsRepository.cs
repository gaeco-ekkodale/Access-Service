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
using AccessService.Domain.Repositories;
using AccessService.Events.AccessRights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Web;

namespace AccessService.Infrastructure.Repositories;

/// <summary>
/// Repository for managing access rights in the database.
/// </summary>
public class AccessRightsRepository : IAccessRightsRepository
{
    private readonly AccessRightDbContext _context;
    private readonly IOutboxRepository _outboxRepository;
    private readonly string _accessRightsTopic;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessRightsRepository"/> class.
    /// </summary>
    /// <param name="context">The database context for access rights.</param>
    /// <param name="outboxRepository">The repository for handling outbox messages.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown if the Kafka topic for access rights is not configured.</exception>
    public AccessRightsRepository(AccessRightDbContext context, IOutboxRepository outboxRepository, IConfiguration configuration)
    {
        _context = context;
        _outboxRepository = outboxRepository;
        _accessRightsTopic = configuration["Kafka:Topics:AccessRights"] ?? throw new ArgumentNullException("Kafka:Topics:AccessRights");

    }

    /// <summary>
    /// Creates a new access right.
    /// </summary>
    /// <param name="newAccessRight">The access right to be created.</param>
    /// <returns>The created access right.</returns>
    public async Task<AccessRight> CreateAccessRightAsync(AccessRight newAccessRight)
    {
        var addedUseCase = await _context.AccessRights.AddAsync(newAccessRight);

        var existingAccessRights = await _context.AccessRights.FirstOrDefaultAsync(ar =>
            ar.GuidlineClassificationPropertyId == newAccessRight.GuidlineClassificationPropertyId
            && ar.GuidelineClassificationId == newAccessRight.GuidelineClassificationId
            && ar.UseCaseId == newAccessRight.UseCaseId
            && ar.UserGroupId == newAccessRight.UserGroupId);

        _outboxRepository.Add(new CreatedAccessRight
        {
            GuidelineClassificationId = newAccessRight.GuidelineClassificationId,
            GuidlineClassificationPropertyId = newAccessRight.GuidlineClassificationPropertyId,
            Id = newAccessRight.Id,
            Name = newAccessRight.Name,
            PropertyRight = newAccessRight.Right,
            UseCaseId = newAccessRight.UseCaseId,
            UserGroupId = newAccessRight.UserGroupId
        }, _accessRightsTopic, newAccessRight.Id);

        if (existingAccessRights != null)
            throw new OperationCanceledException("Access Right already exists");

        await _context.SaveChangesAsync();
        return addedUseCase.Entity;
    }

    /// <summary>
    /// Updates an existing access right.
    /// </summary>
    /// <param name="updatedAccessRight">The updated access right.</param>
    /// <returns>The updated access right.</returns>
    public async Task<AccessRight> UpdateAccessRightAsync(AccessRight updatedAccessRight)
    {
        var accessRight = await _context.AccessRights.SingleOrDefaultAsync(u => u.Id == updatedAccessRight.Id);

        if (accessRight == null)
            throw new OperationCanceledException("Access Right not found");

        accessRight.Id = updatedAccessRight.Id;
        accessRight.Name = updatedAccessRight.Name;
        accessRight.GuidelineClassificationId = updatedAccessRight.GuidelineClassificationId;
        accessRight.UserGroupId = updatedAccessRight.UserGroupId;
        accessRight.UseCaseId = updatedAccessRight.UseCaseId;
        accessRight.GuidlineClassificationPropertyId = updatedAccessRight.GuidlineClassificationPropertyId;
        accessRight.Right = updatedAccessRight.Right;

        _outboxRepository.Add(new UpdatedAccessRight
        {
            GuidelineClassificationId = updatedAccessRight.GuidelineClassificationId,
            GuidlineClassificationPropertyId = updatedAccessRight.GuidlineClassificationPropertyId,
            Id = updatedAccessRight.Id,
            Name = updatedAccessRight.Name,
            PropertyRight = updatedAccessRight.Right,
            UseCaseId = updatedAccessRight.UseCaseId,
            UserGroupId = updatedAccessRight.UserGroupId
        }, _accessRightsTopic, updatedAccessRight.Id);

        await _context.SaveChangesAsync();

        return accessRight;
    }

    /// <summary>
    /// Deletes an access right by its ID.
    /// </summary>
    /// <param name="id">The ID of the access right to be deleted.</param>
    /// <returns>The deleted access right.</returns>
    public async Task<AccessRight> DeleteAccessRightAsync(string id)
    {
        var accessRight = await _context.AccessRights.FirstOrDefaultAsync(u => u.Id == id);

        if (accessRight == null)
            throw new OperationCanceledException("Access Right not found");

        var removedUseCase = _context.AccessRights.Remove(accessRight);
        _outboxRepository.Add(new DeletedAccessRight
        {
            Id = accessRight.Id
        }, _accessRightsTopic, accessRight.Id);

        await _context.SaveChangesAsync();

        return removedUseCase.Entity;
    }

    public async Task<IReadOnlyCollection<AccessRight>> CommitAccessRightsAsync(Guid useCaseId, Guid userGroupId, IEnumerable<AccessRight> accessRights, CancellationToken cancellationToken = default)
    {
        var desiredAccessRights = accessRights
            .Where(accessRight => accessRight.Right != PropertyRight.None)
            .ToList();

        var duplicateKeys = desiredAccessRights
            .GroupBy(accessRight => BuildNaturalKey(accessRight.GuidelineClassificationId, accessRight.GuidlineClassificationPropertyId))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateKeys.Count > 0)
            throw new ArgumentException($"Duplicate access rights in commit payload: {string.Join(", ", duplicateKeys)}", nameof(accessRights));

        var shouldUseTransaction = _context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
        var transaction = shouldUseTransaction
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var existingAccessRights = await _context.AccessRights
                .Where(accessRight => accessRight.UseCaseId == useCaseId && accessRight.UserGroupId == userGroupId)
                .ToListAsync(cancellationToken);

            var existingAccessRightsByKey = existingAccessRights.ToDictionary(
                accessRight => BuildNaturalKey(accessRight.GuidelineClassificationId, accessRight.GuidlineClassificationPropertyId));

            var desiredKeys = new HashSet<string>();

            foreach (var desiredAccessRight in desiredAccessRights)
            {
                var naturalKey = BuildNaturalKey(
                    desiredAccessRight.GuidelineClassificationId,
                    desiredAccessRight.GuidlineClassificationPropertyId);

                desiredKeys.Add(naturalKey);

                if (existingAccessRightsByKey.TryGetValue(naturalKey, out var existingAccessRight))
                {
                    if (!HasChanged(existingAccessRight, desiredAccessRight))
                        continue;

                    existingAccessRight.Name = desiredAccessRight.Name;
                    existingAccessRight.GuidelineClassificationId = desiredAccessRight.GuidelineClassificationId;
                    existingAccessRight.GuidlineClassificationPropertyId = desiredAccessRight.GuidlineClassificationPropertyId;
                    existingAccessRight.Right = desiredAccessRight.Right;

                    _outboxRepository.Add(new UpdatedAccessRight
                    {
                        GuidelineClassificationId = existingAccessRight.GuidelineClassificationId,
                        GuidlineClassificationPropertyId = existingAccessRight.GuidlineClassificationPropertyId,
                        Id = existingAccessRight.Id,
                        Name = existingAccessRight.Name,
                        PropertyRight = existingAccessRight.Right,
                        UseCaseId = existingAccessRight.UseCaseId,
                        UserGroupId = existingAccessRight.UserGroupId
                    }, _accessRightsTopic, existingAccessRight.Id);

                    continue;
                }

                var createdAccessRight = new AccessRight(
                    Guid.NewGuid().ToString(),
                    desiredAccessRight.Name,
                    desiredAccessRight.GuidelineClassificationId,
                    userGroupId,
                    useCaseId,
                    desiredAccessRight.GuidlineClassificationPropertyId,
                    desiredAccessRight.Right);

                await _context.AccessRights.AddAsync(createdAccessRight, cancellationToken);

                _outboxRepository.Add(new CreatedAccessRight
                {
                    GuidelineClassificationId = createdAccessRight.GuidelineClassificationId,
                    GuidlineClassificationPropertyId = createdAccessRight.GuidlineClassificationPropertyId,
                    Id = createdAccessRight.Id,
                    Name = createdAccessRight.Name,
                    PropertyRight = createdAccessRight.Right,
                    UseCaseId = createdAccessRight.UseCaseId,
                    UserGroupId = createdAccessRight.UserGroupId
                }, _accessRightsTopic, createdAccessRight.Id);
            }

            foreach (var accessRightToDelete in existingAccessRights.Where(accessRight =>
                         !desiredKeys.Contains(BuildNaturalKey(accessRight.GuidelineClassificationId, accessRight.GuidlineClassificationPropertyId))))
            {
                _context.AccessRights.Remove(accessRightToDelete);

                _outboxRepository.Add(new DeletedAccessRight
                {
                    Id = accessRightToDelete.Id
                }, _accessRightsTopic, accessRightToDelete.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            return await _context.AccessRights
                .AsNoTracking()
                .Where(accessRight => accessRight.UseCaseId == useCaseId && accessRight.UserGroupId == userGroupId)
                .OrderBy(accessRight => accessRight.GuidelineClassificationId)
                .ThenBy(accessRight => accessRight.GuidlineClassificationPropertyId)
                .ToListAsync(cancellationToken);
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);

            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets all access rights.
    /// </summary>
    /// <returns>A list of all access rights.</returns>
    public async Task<IEnumerable<AccessRight>> GetAllAccessRightsAsync()
    {
        var accessRights = await _context.AccessRights.ToListAsync();

        return accessRights;
    }

    /// <summary>
    /// Gets an access right by its ID.
    /// </summary>
    /// <param name="id">The ID of the access right to be retrieved.</param>
    /// <returns>The access right with the specified ID.</returns>
    public async Task<AccessRight> GetAccessRightAsync(string id)
    {
        var accessRight = await _context.AccessRights.SingleOrDefaultAsync(u => u.Id == id);

        if (accessRight == null)
            throw new OperationCanceledException("Access Right not found");

        return accessRight;
    }

    /// <summary>
    /// Gets access rights by use case ID.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <returns>A list of access rights for the specified use case.</returns>
    public async Task<IEnumerable<AccessRight>> GetAccessRightsByUseCaseAsync(string useCaseId)
    {
        var accessRights = await _context.AccessRights
        .Where(ar => ar.UseCaseId.ToString() == useCaseId)
        .ToListAsync();

        return accessRights;
    }

    /// <summary>
    /// Gets access rights by user group ID.
    /// </summary>
    /// <param name="userGroupId">The user group ID.</param>
    /// <returns>A list of access rights for the specified user group.</returns>
    public async Task<IEnumerable<AccessRight>> GetAccessRightsByUserGroupAsync(string userGroupId)
    {
        var accessRights = await _context.AccessRights
        .Where(ar => ar.UserGroupId.ToString() == userGroupId)
        .ToListAsync();

        return accessRights;
    }

    /// <summary>
    /// Gets access rights by use case ID and user group ID.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <param name="userGroupId">The user group ID.</param>
    /// <returns>A list of access rights for the specified use case and user group.</returns>
    public async Task<IEnumerable<AccessRight>> GetAccessRightsByUseCaseUserGroupAsync(string useCaseId, string userGroupId)
    {
        var accessRights = await _context.AccessRights
        .Where(ar => ar.UseCaseId.ToString() == useCaseId && ar.UserGroupId.ToString() == userGroupId)
        .ToListAsync();

        return accessRights;
    }

    /// <summary>
    /// Gets access rights by use case ID, user group ID, and classification ID.
    /// </summary>
    /// <param name="useCaseId">The use case ID.</param>
    /// <param name="userGroupId">The user group ID.</param>
    /// <param name="classificationId">The classification ID.</param>
    /// <returns>A list of access rights for the specified use case, user group, and classification.</returns>
    public async Task<IEnumerable<AccessRight>> GetAccessRightsByUseCaseUserGroupClassificationAsync(string useCaseId, string userGroupId, string classificationId)
    {
        // classificationId beeing decoded to match the entry in the database.
        var decodedClassificationId = HttpUtility.UrlDecode(classificationId);

        var accessRights = await _context.AccessRights
        .Where(ar => ar.UseCaseId.ToString() == useCaseId
                     && ar.UserGroupId.ToString() == userGroupId
                     && ar.GuidelineClassificationId == decodedClassificationId)
        .ToListAsync();

        return accessRights;
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetDistinctUseCaseIdsAsync()
    {
        return await _context.AccessRights
            .Select(ar => ar.UseCaseId)
            .Distinct()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<(Guid UseCaseId, Guid UserGroupId)>> GetDistinctUseCaseUserGroupPairsAsync()
    {
        var rows = await _context.AccessRights
            .Select(ar => new { ar.UseCaseId, ar.UserGroupId })
            .Distinct()
            .ToListAsync();

        return rows.Select(x => (x.UseCaseId, x.UserGroupId)).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteOrphanedAccessRightsAsync(
        IEnumerable<string> classificationIds,
        IEnumerable<string> classificationPropertyIds,
        CancellationToken cancellationToken = default)
    {
        var clsIds = classificationIds.ToList();
        var propIds = classificationPropertyIds.ToList();

        if (clsIds.Count == 0 && propIds.Count == 0)
            return;

        var toDelete = await _context.AccessRights
            .Where(ar => clsIds.Contains(ar.GuidelineClassificationId)
                      || propIds.Contains(ar.GuidlineClassificationPropertyId))
            .ToListAsync(cancellationToken);

        if (toDelete.Count == 0)
            return;

        foreach (var ar in toDelete)
        {
            _context.AccessRights.Remove(ar);
            _outboxRepository.Add(new DeletedAccessRight { Id = ar.Id }, _accessRightsTopic, ar.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildNaturalKey(string guidelineClassificationId, string guidelineClassificationPropertyId)
    {
        return $"{guidelineClassificationId}::{guidelineClassificationPropertyId}";
    }

    private static bool HasChanged(AccessRight existingAccessRight, AccessRight desiredAccessRight)
    {
        return existingAccessRight.Name != desiredAccessRight.Name
               || existingAccessRight.GuidelineClassificationId != desiredAccessRight.GuidelineClassificationId
               || existingAccessRight.GuidlineClassificationPropertyId != desiredAccessRight.GuidlineClassificationPropertyId
               || existingAccessRight.Right != desiredAccessRight.Right;
    }
}
