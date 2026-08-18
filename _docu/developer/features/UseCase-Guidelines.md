# Use Case Guidelines

This document explains what use case guidelines are, why they exist, and how they are generated in the AccessService backend.

## Purpose

Use case guidelines are reduced guideline artifacts derived from the active guideline projection and filtered by the access rights of a single use case.

Their purpose is to provide downstream consumers with a guideline that already contains only the classifications and classification properties that are relevant for one concrete use case.

The generated result still follows the `Guideline.Model` contract. It is not a proprietary DTO. The service reconstructs a valid guideline model and serializes it again before upload.

## What A Use Case Guideline Contains

A generated use case guideline contains:

- the domain and version metadata of the source guideline version
- only the classifications referenced by access rights of the target use case
- only the classification properties referenced by these access rights
- only the property definitions that are still referenced after filtering
- only the property sets that are still referenced after filtering

This means the artifact is a projection of an already projected guideline version, narrowed to one use case.

## Source Data

The feature does not rebuild the use case guideline from the original uploaded guideline file.

Instead, it uses the relational guideline projection stored in the database:

- `GuidelineVersion`
- `GuidelineClassification`
- `GuidelineClassificationProperty`
- `GuidelineProperty`
- `GuidelinePropertySet`
- `AccessRight`

The service reads the active guideline versions and combines them with the access rights of a use case. This keeps generation deterministic and independent from re-reading the original object from MinIO.

## Generation Flow

The generation flow is:

1. Load all distinct use case IDs or one specific use case ID, depending on the entry point.
2. Load access rights for the target use case.
3. Load all active guideline version IDs.
4. For each active version, load the projected version metadata.
5. Determine the allowed classification IDs from the access rights.
6. Load only those classifications including their classification properties.
7. Filter the classification properties again to keep only the property assignments referenced by the access rights.
8. Collect the required property IDs and property set IDs.
9. Load the matching properties and property sets.
10. Reconstruct a `Guideline.Model` object graph from the stored relational data and JSON fragments.
11. Serialize the guideline.
12. Upload the serialized JSON to object storage.
13. Publish a success event via the outbox.

If a use case has no access rights, no use case guideline is generated.

If a guideline version cannot be loaded or filtering removes all relevant classifications or classification properties, that version is skipped.

## Filtering Rules

Filtering is driven by `AccessRight` entries.

The relevant fields are:

- `GuidelineClassificationId`
- `GuidlineClassificationPropertyId`
- `UseCaseId`

The filtering behavior is intentionally narrow:

- A classification is included only if its original classification ID is referenced by at least one access right of the use case.
- A classification property is included only if its original classification property ID is referenced by at least one access right of the use case.
- A property definition is included only if it is referenced by one of the remaining classification properties.
- A property set is included only if one of the remaining classification properties references it.

This avoids shipping unused parts of the source guideline.

## Reconstruction To Guideline.Model

The stored relational projection does not directly contain a ready-to-serialize `Guideline.Model` object graph. The service therefore reconstructs the model from database columns and persisted JSON fragments.

The reconstruction includes:

- domain metadata from `DomainJson`
- classification metadata and status
- classification property flags such as `IsRequired`, `SortNumber`, `IsReadonly`, `DefaultValue`, and `Reference`
- property definitions, including `PropertySimple`, `PropertyEnum`, `PropertySuperEnum`, and `PropertyTree`
- property assignments, including enum, simple, and super-enum assignments
- property sets and their metadata

The serialized output is produced through `GuidelineReaderWriter`, so the resulting file remains compatible with consumers that expect `Guideline.Model` JSON.

## Storage

Generated use case guidelines are uploaded to the S3-compatible object storage used by the service.

The target bucket is configured via:

- `UseCaseGuideline:BucketName`

If the bucket does not exist, the service creates it before uploading.

The uploaded object represents the generated use case guideline JSON for one use case and one active guideline version.

## Success Event

After a successful upload, the service emits an outbox event for Kafka publication.

The event contract is `UploadedUseCaseGuideline` and contains:

- `UseCaseId`
- `Name`
- `Etag`
- `BucketName`
- `CorrelationId`
- `Timestamp`

The event is published to the Kafka topic configured as:

- `Kafka:Topics:UseCaseGuidelines`

Using the outbox ensures that event publication follows the same reliability pattern already used in other parts of the service.

## Triggering

The intended primary trigger is guideline processing.

After the guideline transformation pipeline has successfully projected guideline data into the relational model, the service can generate use case guidelines for all known use cases.

At the time of writing, this is the documented trigger path:

- guideline is received and transformed
- relational projection is updated
- use case guideline generation is started for all distinct use cases

If additional trigger paths are introduced later, for example regeneration after access-right changes, this document should be updated accordingly.

## Configuration

The feature relies on the following settings:

```json
{
  "Kafka": {
    "Topics": {
      "UseCaseGuidelines": "<topic-name>"
    }
  },
  "UseCaseGuideline": {
    "BucketName": "usecase-guideline"
  }
}
```

## Failure Behavior

Generation is designed to skip invalid or irrelevant slices instead of failing the complete batch whenever possible.

Examples:

- no access rights for a use case: skip generation
- active version missing in the projection: skip that version
- no matching classifications after filtering: skip upload
- no matching classification properties after filtering: skip upload

For bulk generation across all use cases, one failing use case should not block processing of the remaining use cases.

## Why This Feature Exists

The full guideline may contain much more structure than a single use case needs.

Use case guidelines reduce that payload to the subset that is actually relevant for authorization-aware consumers. This improves clarity for downstream systems and creates a stable integration artifact that is:

- scoped to a single use case
- based on persisted projection data
- compatible with `Guideline.Model`
- distributed through S3-compatible storage and Kafka events
