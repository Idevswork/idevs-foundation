using FluentAssertions;
using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Mediator.Behaviors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Idevs.Foundation.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private readonly ILogger<ValidationBehavior<TestValidatableRequest, string>> _logger;
    private readonly ValidationBehavior<TestValidatableRequest, string> _behavior;

    public ValidationBehaviorTests()
    {
        _logger = Substitute.For<ILogger<ValidationBehavior<TestValidatableRequest, string>>>();
        _behavior = new ValidationBehavior<TestValidatableRequest, string>(_logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldProceedToNext()
    {
        // Arrange
        var request = new TestValidatableRequest(true);
        var expectedResponse = "success";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new TestValidatableRequest(false);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns("success");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _behavior.HandleAsync(request, next, CancellationToken.None));

        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().Contain(e => e.PropertyName == "Property1" && e.ErrorMessage == "Error1");
        exception.Errors.Should().Contain(e => e.PropertyName == "Property2" && e.ErrorMessage == "Error2");
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ValidationBehavior<TestValidatableRequest, string>(null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void ValidationError_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var error = new ValidationError("TestProperty", "Test error message");

        // Assert
        error.PropertyName.Should().Be("TestProperty");
        error.ErrorMessage.Should().Be("Test error message");
    }

    [Fact]
    public void ValidationResult_Success_ShouldBeValid()
    {
        // Act & Assert
        ValidationResult.Success.IsValid.Should().BeTrue();
        ValidationResult.Success.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Failure_ShouldBeInvalid()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Prop1", "Error1"),
            new ValidationError("Prop2", "Error2")
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void ValidationException_WithErrors_ShouldHaveCorrectMessage()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Prop1", "Error1"),
            new ValidationError("Prop2", "Error2")
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Be("Validation failed with 2 error(s)");
        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().BeEquivalentTo(errors);
    }

    public record TestValidatableRequest(bool IsValid) : IValidatable
    {
        public ValidationResult Validate()
        {
            if (IsValid)
                return ValidationResult.Success;

            var errors = new[]
            {
                new ValidationError("Property1", "Error1"),
                new ValidationError("Property2", "Error2")
            };

            return ValidationResult.Failure(errors);
        }
    }
}