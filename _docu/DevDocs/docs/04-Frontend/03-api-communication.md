# API Communication

For retrieving and manipulating data, Tanstack Query is being used.

## Use Cases

Use Cases are being retrieved in the UseCaseSelectBar.tsx component, and the selected use case is then passed to the parent.

```tsx title="UseCaseSelectBar.tsx"
const { isLoading, isError, data } = useQuery({
    queryKey: ["useCases"],
    queryFn: () => UseCasesService.getApiUseCases(),
})
```

## User Groups

As with use cases, the user groups are being retrieved in the UserGroupSelectBar.tsx, selected and passed to the parent.

```tsx title="UseCaseSelectBar.tsx"
const { isLoading, isError, data } = useQuery({
    queryKey: ["userGroup"],
    queryFn: () => UserGroupsService.getApiUserGroups(),
})
```

## Classifications

Classifications are being fetched in the ProjectModule.tsx and used for display by ClassificationPanel.tsx.

```tsx title="ProjectModule.tsx"
const { isLoading, isError, data } = useQuery({
    queryKey: ["classifications"],
    queryFn: () => ClassificationService.getClassification(),
})
```

## Properties

Properties are being fetched in the PropertyDialog.tsx and passed to PropertyList to be mapped to a list.

```tsx title="PropertyDialog.tsx"
const { isLoading, isError, data } = useQuery({
    queryKey: ["property"],
    queryFn: () => ClassificationService.getClassificationProperties(encodedClassificationId),
    enabled: !!useCaseId && !!useGroupId && !!encodedClassificationId
})
```

## Access Rights

Access Rights are being fetched in the PropertyList filtered by the classification, and passed to the appropriate property list item.

```tsx title="PropertyList.tsx"
const { isLoading, isError, data } = useQuery({
    queryKey: ["accessRights"],
    queryFn: () => AccessRightsService.getApiAccessRightsUseCaseUserGroup(useCaseId, useGroupId),
})
```