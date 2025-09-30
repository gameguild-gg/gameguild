using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for CQRS pipeline behaviors and validation
/// </summary>
public class PipelineBehaviorTests
{
    [Fact]
    public async Task LoggingBehavior_Should_Log_Request_And_Response()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, string>>>();
        var behavior = new LoggingBehavior<TestRequest, string>(mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var expectedResponse = "handled";
        var nextCalled = false;

        RequestHandlerDelegateBase<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();

        // Verify logging calls
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidationBehavior_Should_Validate_Request_Before_Handler()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { mockValidator.Object });
        var request = new TestRequest { Value = "test" };
        var validationResult = new ValidationResult { IsValid = true };
        var nextCalled = false;

        mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(validationResult);

        RequestHandlerDelegateBase<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("handled");
        nextCalled.Should().BeTrue();
        mockValidator.Verify(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidationBehavior_Should_Throw_When_Validation_Fails()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { mockValidator.Object });
        var request = new TestRequest { Value = "invalid" };
        var validationResult = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { new ValidationError { PropertyName = "Value", ErrorMessage = "Value is invalid" } }
        };

        mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(validationResult);

        RequestHandlerDelegateBase<string> next = () => Task.FromResult("should not be called");

        // Act & Assert
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>()
                 .WithMessage("*Value is invalid*");
    }

    [Fact]
    public async Task ValidationBehavior_Should_Aggregate_Multiple_Validation_Errors()
    {
        // Arrange
        var mockValidator1 = new Mock<IValidator<TestRequest>>();
        var mockValidator2 = new Mock<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { mockValidator1.Object, mockValidator2.Object });
        var request = new TestRequest { Value = "invalid" };

        var validationResult1 = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { new ValidationError { PropertyName = "Value", ErrorMessage = "Error 1" } }
        };

        var validationResult2 = new ValidationResult
        {
            IsValid = false,
            Errors = new[] { new ValidationError { PropertyName = "Value", ErrorMessage = "Error 2" } }
        };

        mockValidator1.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult1);
        mockValidator2.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(validationResult2);

        RequestHandlerDelegateBase<string> next = () => Task.FromResult("should not be called");

        // Act & Assert
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<ValidationException>();

        exception.Which.Message.Should().Contain("Error 1");
        exception.Which.Message.Should().Contain("Error 2");
    }

    [Fact]
    public async Task ValidationBehavior_Should_Skip_When_No_Validators()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
        var request = new TestRequest { Value = "test" };
        var nextCalled = false;

        RequestHandlerDelegateBase<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("handled");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("handled");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task PerformanceBehavior_Should_Log_Slow_Requests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, string>>>();
        var behavior = new PerformanceBehavior<TestRequest, string>(mockLogger.Object);
        var request = new TestRequest { Value = "slow-request" };

        RequestHandlerDelegateBase<string> next = async () =>
        {
            await Task.Delay(100); // Simulate slow operation
            return "handled";
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("handled");
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Long running request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PerformanceBehavior_Should_Not_Log_Fast_Requests()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, string>>>();
        var behavior = new PerformanceBehavior<TestRequest, string>(mockLogger.Object);
        var request = new TestRequest { Value = "fast-request" };

        RequestHandlerDelegateBase<string> next = () => Task.FromResult("handled");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("handled");
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ExceptionHandlingBehavior_Should_Catch_And_Wrap_Exceptions()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ExceptionHandlingBehavior<TestRequest, string>>>();
        var behavior = new ExceptionHandlingBehavior<TestRequest, string>(mockLogger.Object);
        var request = new TestRequest { Value = "exception-request" };
        var originalException = new InvalidOperationException("Original error");

        RequestHandlerDelegateBase<string> next = () => throw originalException;

        // Act & Assert
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<ApplicationException>()
                                 .WithMessage("An error occurred while processing the request");

        exception.Which.InnerException.Should().Be(originalException);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception occurred")),
                originalException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CachingBehavior_Should_Return_Cached_Result_If_Available()
    {
        // Arrange
        var mockCache = new Mock<ICacheService>();
        var behavior = new CachingBehavior<CacheableTestRequest, string>(mockCache.Object);
        var request = new CacheableTestRequest { Value = "cached-request", CacheKey = "test-cache-key" };
        var cachedResult = "cached-result";
        var nextCalled = false;

        mockCache.Setup(c => c.GetAsync<string>(request.CacheKey, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(cachedResult);

        RequestHandlerDelegateBase<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("fresh-result");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(cachedResult);
        nextCalled.Should().BeFalse();
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CachingBehavior_Should_Cache_Fresh_Result_If_Not_Cached()
    {
        // Arrange
        var mockCache = new Mock<ICacheService>();
        var behavior = new CachingBehavior<CacheableTestRequest, string>(mockCache.Object);
        var request = new CacheableTestRequest
        {
            Value = "uncached-request",
            CacheKey = "test-cache-key",
            CacheExpiration = TimeSpan.FromMinutes(5)
        };
        var freshResult = "fresh-result";
        var nextCalled = false;

        mockCache.Setup(c => c.GetAsync<string>(request.CacheKey, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((string?)null);
        mockCache.Setup(c => c.SetAsync(request.CacheKey, freshResult, request.CacheExpiration, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        RequestHandlerDelegateBase<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(freshResult);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(freshResult);
        nextCalled.Should().BeTrue();
        mockCache.Verify(c => c.SetAsync(request.CacheKey, freshResult, request.CacheExpiration, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Multiple_Behaviors_Should_Execute_In_Correct_Order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, string>>>();
        var mockValidator = new Mock<IValidator<TestRequest>>();

        var loggingBehavior = new LoggingBehavior<TestRequest, string>(mockLogger.Object);
        var validationBehavior = new ValidationBehavior<TestRequest, string>(new[] { mockValidator.Object });

        var request = new TestRequest { Value = "pipeline-test" };
        var validationResult = new ValidationResult { IsValid = true };

        mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(validationResult);

        // Create nested pipeline: Logging -> Validation -> Handler
        RequestHandlerDelegateBase<string> handler = () =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult("handled");
        };

        RequestHandlerDelegateBase<string> validationNext = () =>
        {
            executionOrder.Add("ValidationNext");
            return validationBehavior.Handle(request, handler, CancellationToken.None);
        };

        // Act
        executionOrder.Add("LoggingStart");
        var result = await loggingBehavior.Handle(request, validationNext, CancellationToken.None);
        executionOrder.Add("LoggingEnd");

        // Assert
        result.Should().Be("handled");
        executionOrder.Should().ContainInOrder("LoggingStart", "ValidationNext", "Handler", "LoggingEnd");
    }

    [Fact]
    public async Task PerformanceBehavior_Should_Measure_Execution_Time()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, string>>>();
        var behavior = new PerformanceBehavior<TestRequest, string>(mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var nextCalled = false;

        RequestHandlerDelegateBase<string> next = async () =>
        {
            nextCalled = true;
            await Task.Delay(100); // Simulate some work
            return "handled";
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("handled");
        nextCalled.Should().BeTrue();

        // Verify performance logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExceptionHandlingBehavior_Should_Catch_And_Transform_Exceptions()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ExceptionHandlingBehavior<TestRequest, string>>>();
        var behavior = new ExceptionHandlingBehavior<TestRequest, string>(mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var originalException = new InvalidOperationException("Original error");

        RequestHandlerDelegateBase<string> next = () => throw originalException;

        // Act & Assert
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);
        await act.Should().ThrowAsync<ApplicationException>()
                 .WithMessage("*An error occurred while processing the request*");

        // Verify exception logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception")),
                It.Is<Exception>(ex => ex == originalException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CachingBehavior_Should_Cache_Results()
    {
        // Arrange
        var mockCache = new Mock<ICacheService>();
        var behavior = new CachingBehavior<TestCacheableRequest, string>(mockCache.Object);
        var request = new TestCacheableRequest { Value = "test", CacheKey = "cache-key" };
        var expectedResult = "cached-result";
        var nextCallCount = 0;

        // First call - cache miss
        mockCache.Setup(c => c.GetAsync<string>(request.CacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

        mockCache.Setup(c => c.SetAsync(request.CacheKey, expectedResult, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        RequestHandlerDelegateBase<string> next = () =>
        {
            nextCallCount++;
            return Task.FromResult(expectedResult);
        };

        // Act - First call
        var result1 = await behavior.Handle(request, next, CancellationToken.None);

        // Arrange for second call - cache hit
        mockCache.Setup(c => c.GetAsync<string>(request.CacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

        // Act - Second call
        var result2 = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result1.Should().Be(expectedResult);
        result2.Should().Be(expectedResult);
        nextCallCount.Should().Be(1); // Handler should only be called once

        mockCache.Verify(c => c.GetAsync<string>(request.CacheKey, It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockCache.Verify(c => c.SetAsync(request.CacheKey, expectedResult, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test classes and interfaces
    public class TestRequest : IBaseRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestCacheableRequest : IBaseRequest, ICacheableRequest
    {
        public string Value { get; set; } = string.Empty;
        public string CacheKey { get; set; } = string.Empty;
        public TimeSpan CacheExpiration => TimeSpan.FromMinutes(5);
    }

    public interface ICacheableRequest
    {
        string CacheKey { get; }
        TimeSpan CacheExpiration { get; }
    }

    public interface IValidator<T>
    {
        Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public ValidationError[] Errors { get; set; } = Array.Empty<ValidationError>();
    }

    public class ValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    // Sample pipeline behaviors for testing
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseRequest
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
            var response = await next();
            _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
            return response;
        }
    }

    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseRequest
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(request, cancellationToken)));
                var errors = validationResults.SelectMany(r => r.Errors).Where(e => e != null).ToArray();

                if (errors.Any())
                {
                    throw new ValidationException($"Validation failed: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                }
            }

            return await next();
        }
    }

    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseRequest
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 50) // Log if takes longer than 50ms
            {
                _logger.LogWarning("Long running request {RequestType} took {ElapsedMs}ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
    }

    public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseRequest
    {
        private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;

        public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while handling {RequestType}", typeof(TRequest).Name);
                throw new ApplicationException("An error occurred while processing the request", ex);
            }
        }
    }

    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseRequest, ICacheableRequest
    {
        private readonly ICacheService _cache;

        public CachingBehavior(ICacheService cache)
        {
            _cache = cache;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
        {
            var cachedResult = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await next();
            await _cache.SetAsync(request.CacheKey, result, request.CacheExpiration, cancellationToken);
            return result;
        }
    }

    // Test request classes
    public class CacheableTestRequest : IRequest<string>, ICacheableRequest
    {
        public string Value { get; set; } = string.Empty;
        public string CacheKey { get; set; } = string.Empty;
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }
}