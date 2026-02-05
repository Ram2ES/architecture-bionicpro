using Microsoft.AspNetCore.Authorization;

namespace BionicPRO.Api.Authorization;

public class UserIdAuthorizationRequirement : IAuthorizationRequirement
{
    public string RouteParameterName { get; }

    public UserIdAuthorizationRequirement(string routeParameterName = "userId")
    {
        RouteParameterName = routeParameterName;
    }
}
