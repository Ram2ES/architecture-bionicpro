using Microsoft.AspNetCore.Authorization;

namespace BionicPRO.Api.Authorization;

/// <summary>
/// Обработчик авторизации: проверяет, что пользователь запрашивает свои данные
/// </summary>
public class UserIdAuthorizationHandler : AuthorizationHandler<UserIdAuthorizationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserIdAuthorizationHandler> _logger;

    public UserIdAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserIdAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserIdAuthorizationRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null");
            context.Fail();
            return Task.CompletedTask;
        }

        var systemUserIdClaim = context.User.FindFirst("system_user_id");
        if (systemUserIdClaim == null || !Guid.TryParse(systemUserIdClaim.Value, out var systemUserId))
        {
            _logger.LogWarning("system_user_id claim not found or invalid");
            context.Fail();
            return Task.CompletedTask;
        }

        var routeData = httpContext.GetRouteData();
        if (!routeData.Values.TryGetValue(requirement.RouteParameterName, out var userIdObj) ||
            !Guid.TryParse(userIdObj?.ToString(), out var requestedUserId))
        {
            _logger.LogWarning("Route parameter {ParameterName} not found or invalid", requirement.RouteParameterName);
            context.Fail();
            return Task.CompletedTask;
        }

        // Администраторы могут получать любые данные
        if (context.User.IsInRole("administrator"))
        {
            _logger.LogInformation("Administrator accessing userId {RequestedUserId}", requestedUserId);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Обычные пользователи могут получать только свои данные
        if (systemUserId == requestedUserId)
        {
            _logger.LogInformation("User {SystemUserId} accessing own data", systemUserId);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        _logger.LogWarning("User {SystemUserId} attempted to access user {RequestedUserId}",
            systemUserId, requestedUserId);
        context.Fail();
        return Task.CompletedTask;
    }
}
