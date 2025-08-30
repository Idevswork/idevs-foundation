using System.Security.Claims;
using Idevs.Foundation.Cqrs.Results;
using Idevs.Foundation.Services.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.AspNetCore;

[ApiController]
public abstract class IdevsController<TController>(ILogger<TController>? logger = null)
    : ControllerBase
{
    protected readonly ILogger<TController> Logger = logger ?? Log.GetLogger<TController>();

    protected abstract bool IsSupportedTenant { get; }
    protected abstract string? TenantColumnName { get; }

    #region User Context Properties

    /// <summary>
    /// Gets the current user's ID from JWT claims.
    /// </summary>
    /// <typeparam name="TId">Type of the ID e.g., int, long, Guid</typeparam>
    /// <returns>ID or default</returns>
    protected virtual TId? GetCurrentUserId<TId>()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return userIdClaim != null ? (TId?)Convert.ChangeType(userIdClaim.Value, typeof(TId)) : default;
    }

    /// <summary>
    /// Gets the current user's email from JWT claims.
    /// </summary>
    protected virtual string? CurrentUserEmail =>
        User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

    /// <summary>
    /// Gets the current user's name from JWT claims.
    /// </summary>
    protected virtual string? CurrentUserName =>
        User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value;

    /// <summary>
    /// Gets the current user's tenant ID from JWT claims.
    /// </summary>
    /// <typeparam name="TId">Type of the ID e.g., int, long, Guid</typeparam>
    /// <returns>ID or default</returns>
    protected virtual TId? GetTenantId<TId>()
    {
        if (!IsSupportedTenant) return default;
        if (string.IsNullOrWhiteSpace(TenantColumnName)) return default;
        var tenantIdClaim = User.FindFirst(TenantColumnName);
        return tenantIdClaim != null ? (TId?)Convert.ChangeType(tenantIdClaim.Value, typeof(TId)) : default;
    }

    /// <summary>
    /// Gets the current user's roles from JWT claims.
    /// </summary>
    protected virtual IEnumerable<string> CurrentUserRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value);

    /// <summary>
    /// Checks if the current user has the specified role.
    /// </summary>
    /// <param name="role">The role to check for</param>
    /// <returns>True if the user has the role, false otherwise.</returns>
    protected virtual bool HasRole(string role) => CurrentUserRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the client IP address from the request.
    /// </summary>
    protected virtual string? ClientIpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ??
        Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
        Request.Headers["X-Real-IP"].FirstOrDefault();

    #endregion

    #region Response Helper Methods

    /// <summary>
    /// Creates a response based on a Result object.
    /// </summary>
    /// <param name="result">The result to convert to an HTTP response.</param>
    /// <returns>An appropriate HTTP response</returns>
    protected virtual IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
            return Ok(new { message = "Operation completed successfully" });

        return BadRequest(new
        {
            message = result.ErrorMessage,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Creates a response based on a Result{T} object.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The result to convert to an HTTP response.</param>
    /// <returns>An appropriate HTTP response</returns>
    protected virtual IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);

        return BadRequest(new
        {
            message = result.ErrorMessage,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Creates an authentication-specific response based on a Result{T} object.
    /// Returns 401 Unauthorized for authentication failures instead of 400 Bad Request.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The result to convert to an HTTP response.</param>
    /// <returns>An appropriate HTTP response.</returns>
    protected virtual IActionResult HandleAuthResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);

        return Unauthorized();
    }

    /// <summary>
    /// Creates a Created response with location header.
    /// </summary>
    /// <typeparam name="T">The type of the created resource.</typeparam>
    /// <param name="actionName">The action name for the location header.</param>
    /// <param name="routeValues">Route values for the location header.</param>
    /// <param name="value">The created resource.</param>
    /// <returns>A 201 Created response.</returns>
    protected IActionResult CreatedAtAction<T>(string actionName, object? routeValues, T value)
        => base.CreatedAtAction(actionName, routeValues, value);

    /// <summary>
    /// Creates a paginated response with metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection of items.</param>
    /// <param name="totalCount">Total number of items.</param>
    /// <param name="pageNumber">Current page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A response with paginated data and metadata.</returns>
    protected IActionResult PaginatedResponse<T>(
        IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new
        {
            data = items,
            pagination = new
            {
                currentPage = pageNumber,
                pageSize,
                totalPages,
                totalCount,
                hasNext = pageNumber < totalPages,
                hasPrevious = pageNumber > 1
            }
        });
    }
    
    #endregion

    #region Validation Helper Methods

    /// <summary>
    /// Validates the model state and returns a bad request if invalid.
    /// </summary>
    /// <returns>BadRequest if the model is invalid, null if valid.</returns>
    protected virtual IActionResult? ValidateModel()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []
                );

            return BadRequest(new {
                message = "Validation failed",
                errors
            });
        }
        return null;
    }

    /// <summary>
    /// Ensures the current user is authenticated.
    /// </summary>
    /// <returns>Unauthorized response if is not authenticated, null if authenticated.</returns>
    protected IActionResult? RequireAuthentication()
    {
        return User?.Identity is not { IsAuthenticated: true } ? Unauthorized() : null;
    }

    /// <summary>
    /// Ensures the current user has a specific role.
    /// </summary>
    /// <param name="requiredRole">The required role.</param>
    /// <returns>Forbidden response if a role missing, null if authorized.</returns>
    protected IActionResult? RequireRole(string requiredRole)
    {
        return !HasRole(requiredRole) ? Forbid($"Role '{requiredRole}' is required for this operation") : null;
    }

    /// <summary>
    /// Ensures the current user has at least one of the specified roles.
    /// </summary>
    /// <param name="roles">The allowed roles.</param>
    /// <returns>Forbidden response if no roles match, null if authorized.</returns>
    protected IActionResult? RequireAnyRole(params string[] roles)
    {
        return roles.Any(HasRole)
            ? null
            : Forbid($"One of the following roles is required: {string.Join(", ", roles)}");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the user agent from the request headers.
    /// </summary>
    protected virtual string? UserAgent => Request.Headers["User-Agent"].FirstOrDefault();

    /// <summary>
    /// Logs an operation with user context.
    /// </summary>
    /// <param name="userId">User ID as string value</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="additionalData">Additional data to log.</param>
    protected virtual void LogUserOperation(string userId, string operation, object? additionalData = null)
    {
        Logger.LogInformation("User {UserId} ({Email}) performed {Operation} from {IP}. Data: {@AdditionalData}",
            userId, CurrentUserEmail, operation, ClientIpAddress, additionalData);
    }

    #endregion
}
