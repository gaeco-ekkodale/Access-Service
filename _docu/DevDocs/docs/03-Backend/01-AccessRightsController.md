# AccessRightsController
This Controller is responsible for posting and retrieving Access Rights.

```csharp title="AccessRightDTO.cs"
public class AccessRightDTO
{
    public string Name { get; set; }

    public string GuidelineClassificationId { get; set; }

    public Guid UserGroupId { get; set; }
    
    public Guid UseCaseId { get; set; }
    
    public string GuidlineClassificationPropertyId { get; set; }
    
    public Right Right { get; set; }
}
```

```csharp title="Right.cs"
public enum Right
{
    None = 0,
    Write = 1,
    Read = 2
}

```

## API Endpoints

```tsx
POST api/AccessRights
```
Adds, updates, or deletes access rights to/from the database. 

```tsx
GET api/AccessRights/useCase/{usecaseId}
```
Retrieves all access rights by use case.

```tsx
GET api/AccessRights/useCase/{usecaseId}/userGroup{userGroupId}
```
Retrieves all access rights by use case and user group.

```tsx
GET api/AccessRights/useCase/{usecaseId}/userGroup{userGroupId}/classification{classificationId}
```
Retrieves all access rights by use case, user group and classification.

## General functionality
The POST API of the AccessRightsController hast all CRUD operations. It takes an AccessRightDTO and checks if there is already an entry in the database, if there is none, it creates one. If there is already an entry, it checks if the Right should be updated or deleted.

```csharp title="ClassificationAccessRights.cs"
[Table("ClassificationAccessRights")]
public class ClassificationAccessRights
{
    /// UseCase of the Access Right
    [Required]
    [Column("UseCaseID")]
    public Guid UseCaseID { get; set; }

    /// User Group the right is in
    [Required]
    [Column("UserGroupID")]
    public Guid UserGroupID { get; set; }

    /// List of classifications that have access rights.
    public List<Classification>? ClassificationRights { get; set; }
}
```

```csharp title="Classification.cs"
public class Classification
{
    /// List of Access Rights That belong to the Classification 
    public List<AccessRight> AccessRights { get; set; }

    /// Id of the Classification
    public string ClassificationIdentifer { get; set; }
}

```

:::info

Only "Read" or "Write" Right values are saved in the database. Access Right with "None" as a value is either not saved or, if there is already an entry, deleted.

:::

All the GET functions are using the Entity Framework to find and retrieve the data.

```csharp title="ClassificationAccessRightRepository.cs"
public Task<List<AccessRight>> GetAllAccessRightForUseCaseAndUserGroupAsync(Guid useCaseId, Guid userGroupId)
{
    return _context.DataSet
      .AsNoTracking()
      .Where(e => e.UseCaseID == useCaseId && e.UserGroupID == userGroupId)
      .SelectMany(e => e.ClassificationRights)
      .SelectMany(f => f.AccessRights)
      .ToListAsync();
}
```
