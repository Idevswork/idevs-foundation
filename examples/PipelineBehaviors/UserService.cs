using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Cqrs.Commands;
using Idevs.Foundation.Cqrs.Queries;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Examples.PipelineBehaviors;

// Example models
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserProfile
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

// Query Examples with Caching
public record GetUserQuery(int UserId) : IQuery<User>, ICacheable
{
    public string CacheKey => $"user:{UserId}";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(15);
}

public record GetUserProfileQuery(int UserId) : IQuery<UserProfile>, ICacheable, IRetryable
{
    // Caching configuration
    public string CacheKey => $"user-profile:{UserId}";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(10);

    // Retry configuration for external profile service
    public int MaxRetryAttempts => 2;
    public TimeSpan BaseDelay => TimeSpan.FromMilliseconds(500);
    public RetryPolicy RetryPolicy => RetryPolicy.ExponentialBackoff;
    public bool UseJitter => true;

    public bool ShouldRetry(Exception exception) =>
        exception is HttpRequestException or TimeoutException;
}

// Command Examples with Validation and Transactions
public record CreateUserCommand(string Email, string Name, string Bio) 
    : ICommand<User>, IValidatable, ITransactional
{
    // Validation
    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add(new ValidationError(nameof(Email), "Email is required"));
        else if (!IsValidEmail(Email))
            errors.Add(new ValidationError(nameof(Email), "Invalid email format"));

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError(nameof(Name), "Name is required"));
        else if (Name.Length < 2 || Name.Length > 100)
            errors.Add(new ValidationError(nameof(Name), "Name must be between 2 and 100 characters"));

        if (!string.IsNullOrEmpty(Bio) && Bio.Length > 500)
            errors.Add(new ValidationError(nameof(Bio), "Bio cannot exceed 500 characters"));

        return errors.Count == 0 
            ? ValidationResult.Success 
            : ValidationResult.Failure(errors);
    }

    // Transaction configuration
    public IsolationLevel? IsolationLevel => System.Data.IsolationLevel.ReadCommitted;
    public TimeSpan? Timeout => TimeSpan.FromMinutes(1);

    private static bool IsValidEmail(string email) =>
        email.Contains("@") && email.Contains(".");
}

public record UpdateUserProfileCommand(int UserId, string Name, string Bio, string AvatarUrl) 
    : ICommand<UserProfile>, IValidatable, ITransactional
{
    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        if (UserId <= 0)
            errors.Add(new ValidationError(nameof(UserId), "Invalid user ID"));

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError(nameof(Name), "Name is required"));
        else if (Name.Length < 2 || Name.Length > 100)
            errors.Add(new ValidationError(nameof(Name), "Name must be between 2 and 100 characters"));

        if (!string.IsNullOrEmpty(Bio) && Bio.Length > 500)
            errors.Add(new ValidationError(nameof(Bio), "Bio cannot exceed 500 characters"));

        if (!string.IsNullOrEmpty(AvatarUrl) && !Uri.TryCreate(AvatarUrl, UriKind.Absolute, out _))
            errors.Add(new ValidationError(nameof(AvatarUrl), "Invalid avatar URL format"));

        return errors.Count == 0 
            ? ValidationResult.Success 
            : ValidationResult.Failure(errors);
    }

    public IsolationLevel? IsolationLevel => System.Data.IsolationLevel.ReadCommitted;
    public TimeSpan? Timeout => TimeSpan.FromSeconds(30);
}

// Command with Retry for external service integration
public record SendWelcomeEmailCommand(int UserId, string Email, string Name) 
    : ICommand, IRetryable
{
    public int MaxRetryAttempts => 3;
    public TimeSpan BaseDelay => TimeSpan.FromSeconds(1);
    public RetryPolicy RetryPolicy => RetryPolicy.ExponentialBackoff;
    public bool UseJitter => true;

    public bool ShouldRetry(Exception exception) =>
        // Retry on transient network issues but not on validation errors
        exception is HttpRequestException or TimeoutException or SocketException;
}

// Example Handler Implementations
public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        // This will be cached automatically by CachingBehavior
        return await _repository.GetByIdAsync(query.UserId, cancellationToken);
    }
}

public class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfile>
{
    private readonly IExternalProfileService _externalService;
    private readonly IUserProfileRepository _repository;

    public GetUserProfileQueryHandler(
        IExternalProfileService externalService,
        IUserProfileRepository repository)
    {
        _externalService = externalService;
        _repository = repository;
    }

    public async Task<UserProfile> HandleAsync(GetUserProfileQuery query, CancellationToken cancellationToken)
    {
        // This will be cached and retried automatically
        var profile = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        
        if (profile == null)
        {
            // This call might fail and be retried
            var externalData = await _externalService.GetUserDataAsync(query.UserId, cancellationToken);
            profile = MapToProfile(externalData);
        }

        return profile;
    }

    private static UserProfile MapToProfile(ExternalUserData data) =>
        new UserProfile
        {
            UserId = data.Id,
            Email = data.Email,
            Name = data.DisplayName,
            Bio = data.Biography,
            AvatarUrl = data.ProfileImageUrl,
            LastUpdated = DateTime.UtcNow
        };
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;
    private readonly IUserProfileRepository _profileRepository;

    public CreateUserCommandHandler(
        IUserRepository repository,
        IUserProfileRepository profileRepository)
    {
        _repository = repository;
        _profileRepository = profileRepository;
    }

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // This entire operation runs in a transaction automatically
        
        // Check if user already exists
        var existingUser = await _repository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"User with email {command.Email} already exists");

        // Create user
        var user = new User
        {
            Email = command.Email,
            Name = command.Name,
            CreatedAt = DateTime.UtcNow
        };

        user = await _repository.CreateAsync(user, cancellationToken);

        // Create profile
        var profile = new UserProfile
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Bio = command.Bio,
            LastUpdated = DateTime.UtcNow
        };

        await _profileRepository.CreateAsync(profile, cancellationToken);

        // Transaction will commit automatically if everything succeeds
        // or rollback if any exception occurs
        
        return user;
    }
}

public class SendWelcomeEmailCommandHandler : ICommandHandler<SendWelcomeEmailCommand>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailCommandHandler> _logger;

    public SendWelcomeEmailCommandHandler(
        IEmailService emailService,
        ILogger<SendWelcomeEmailCommandHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(SendWelcomeEmailCommand command, CancellationToken cancellationToken)
    {
        // This will be retried automatically on transient failures
        _logger.LogInformation("Sending welcome email to {Email} for user {UserId}", 
            command.Email, command.UserId);

        await _emailService.SendWelcomeEmailAsync(
            command.Email, 
            command.Name, 
            cancellationToken);

        _logger.LogInformation("Welcome email sent successfully to {Email}", command.Email);
    }
}

// Interface definitions (normally these would be in separate files)
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
}

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserProfile> CreateAsync(UserProfile profile, CancellationToken cancellationToken = default);
}

public interface IExternalProfileService
{
    Task<ExternalUserData> GetUserDataAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default);
}

public class ExternalUserData
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
}