# UserGroupsController
This controller is responsible for retrieving User Groups.

```csharp title="UserGroupDTO.cs"
public class UserGroupDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; }
}
```

## API Endpoint

```tsx
GET api/UserGroups
```
Retrieves all user groups from Keycloak.

## General functionality

When an API call to retrieve all user groups is being made, the controller calls the GetKeycloakGroups() function from the IUserGroupsRepository.

```csharp title="UserGroupsController.cs"
var groups = await _userGroupsRepository.GetKeycloakGroups();

return Ok(groups);
```

In the UserGroupsRepository, an access token is retrieved first.

```csharp title="UserGroupsRepository.cs"
var token = await GetAccessToken();

var groups = await GetGroups(token);

return groups;
```

```csharp title="UserGroupsRepository.cs"
var tokenResponse = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(clientCredentials));
tokenResponse.EnsureSuccessStatusCode();

var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
return tokenContent.AccessToken;
```

With this token from our Keycloak client, the user groups can be retrieved.

```csharp title="UserGroupsRepository.cs"
var groupsEndpoint = $"{_configuration["Keycloak:ServerUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/groups";
_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

var groupsResponse = await _httpClient.GetAsync(groupsEndpoint);
groupsResponse.EnsureSuccessStatusCode();

var groups = await groupsResponse.Content.ReadFromJsonAsync<IEnumerable<UserGroupDTO>>();
return groups;
```