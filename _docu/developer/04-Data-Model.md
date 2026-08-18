# Data Model

This document describes the enums and data models of the AccessRight Service.

# Enums

This section describes the enumerations used within the data model.

## ClassificationRight

Enum for the access rights of a classification.

- **None** (`0`): No access is granted.
- **Write** (`1`): Full write access is granted.
- **Read** (`2`): Full read access is granted.
- **Mixed** (`3`): Indicates that the underlying properties have a mix of read and write access.

## PropertyRight

Enum for the access rights of a property.

- **None** (`0`): No access is granted.
- **Write** (`1`): Write access is granted.
- **Read** (`2`): Read access is granted.

## PropertySetRight

Enum for the access rights of a property set.

- **None** (`0`): No access is granted.
- **Write** (`1`): Full write access is granted.
- **Read** (`2`): Full read access is granted.
- **Mixed** (`3`): Indicates that the underlying properties have a mix of read and write access.

# Core Models

This section describes the core data models.

## AccessRight

Represents the database table definition for an Access Right. It has one public constructor to initialize all properties.

- **Id** (`string`, max. 40 characters): The ID of the AccessRight.
- **Name** (`string`, max. 150 characters): The name of the guideline-classification-property.
- **GuidelineClassificationId** (`string`, max. 300 characters): The ID of the GuidelineClassification.
- **UserGroupId** (`Guid`): The ID of the Usergroup the AccessRight belongs to.
- **UseCaseId** (`Guid`): The ID of the Use Case the AccessRight belongs to.
- **GuidlineClassificationPropertyId** (`string`, max. 300 characters): The ID of the Guideline Classification Property.
- **Right** (`PropertyRight`): The access permission for the property.

## Classification

Represents a Classification definition, including its associated property sets.

- **Id** (`string`): The unique identifier of the classification.
- **Name** (`string`): The name of the classification.
- **Right** (`ClassificationRight`): The effective access right for the classification. Defaults to `None`.
- **PropertySets** (`List<PropertySet>`): A list of property sets associated with this classification.

## ClassificationList

Represents a simplified classification entry, typically used in a list.

- **Id** (`string`): The Id of the classification.
- **Name** (`string`): The name of the classification.

## ClassificationsListSet

A container for a list of `ClassificationList` objects.
- **Classifications** (`List<ClassificationList>`): A list of classifications.

## Property

Represents a Property definition.

- **Id** (`string`): The id of the property.
- **Name** (`string`): The name of the property.
- **Value** (`string`): The value of the property.
- **StorageType** (`StorageType`): The storage type of the property.
- **Right** (`PropertyRight`): The access right of the property. Defaults to `None`.

## PropertySet

Represents a Property Set definition, which is a collection of related properties.

- **Id** (`string`): The unique identifier of the property set.
- **Name** (`string`): The name of the property set.
- **Properties** (`List<Property>`): A list of properties associated with this property set.
- **Right** (`PropertySetRight`): The effective access right for the property set. Defaults to `None`.

## UserGroup

Represents a user group definition.

- **Id** (`Guid`): The unique identifier for the user group.
- **Name** (`string`, max. 150 characters): The name of the user group.

## OutboxEvent

Represents an event stored for later processing as part of the outbox pattern.

- **Id** (`Guid`): The unique identifier for the outbox event.
- **Topic** (`string`, max. 200 characters): The topic name for publishing the event.
- **AggregateId** (`string`, max. 40 characters): The identifier of the aggregate root that this event belongs to.
- **EventType** (`string`, max. 200 characters): The type or name of the event.
- **OccurredOn** (`DateTimeOffset`): The timestamp when the event occurred.
- **Payload** (`string?`): An optional JSON-serialized payload of the event.
- **RetryCount** (`int`): The number of times the processing of this event has been attempted. It always starts at `0` if the public constructor was used.

# DTOs

This section describes the Data Transfer Objects (DTOs) used by the service's API.

## DTO Models

This section describes the DTO models.

### PropertyRightDto

DTO enum for the access rights of a property.

- **None** (`0`): No access is granted.
- **Write** (`1`): Write access is granted.
- **Read** (`2`): Read access is granted.

### AccessRightDTO

Represents the Data Transfer Object (DTO) for an access right.

- **Id** (`string`): The ID of the access right.
- **Name** (`string`): The name of the access right.
- **GuidelineClassificationId** (`string`): The ID of the guideline classification.
- **UserGroupId** (`Guid`): The ID of the Usergroup the AccessRight belongs to.
- **UseCaseId** (`Guid`): The ID of the Use Case the AccessRight belongs to.
- **GuidlineClassificationPropertyId** (`string`): The ID of the guideline classification property.
- **Right** (`PropertyRight`): The specific property right.

### ClassificationPropertyDTO

Represents the Data Transfer Object (DTO) for a property of a classification instance item.

- **Id** (`string`): The ID of the property.
- **Name** (`string`): The name of the property.
- **StorageType** (`StorageType`): The storage type of the property.
- **PropertySetName** (`string`): The name of the property set.
- **PropertySetId** (`string`): The ID of the property set.

### UserGroupDTO

Represents the Data Transfer Object (DTO) for a user group.

- **Id** (`Guid`): The ID of the user group.
- **Name** (`string`): The name of the user group.