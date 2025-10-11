using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fluid;
using Fluid.Values;
using Fluid.Ast;

namespace GameGuild.Modules.Notifications.Templating;

/// <summary>
/// Notification template engine interface.
/// </summary>
public interface INotificationTemplateEngine
{
    Task<string> RenderAsync(
        string template,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default);

    Task<TemplateValidationResult> ValidateAsync(
        string template,
        CancellationToken cancellationToken = default);

    Task<List<string>> ExtractVariablesAsync(
        string template,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Liquid template engine implementation for notification templates.
/// </summary>
public sealed class LiquidTemplateEngine : INotificationTemplateEngine
{
    private readonly ILogger<LiquidTemplateEngine> _logger;
    private readonly FluidParser _parser;
    private readonly TemplateOptions _options;

    public LiquidTemplateEngine(ILogger<LiquidTemplateEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = new FluidParser();
        _options = new TemplateOptions();

        // Configure template options
        _options.MemberAccessStrategy.Register<Dictionary<string, object>>();
        _options.ValueConverters.Add(x => x is Dictionary<string, object> dict ? new ObjectValue(dict) : null);
    }

    public async Task<string> RenderAsync(
        string template,
        Dictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_parser.TryParse(template, out var fluidTemplate, out var error))
            {
                _logger.LogError("Template parsing failed: {Error}", error);
                throw new TemplateException($"Failed to parse template: {error}");
            }

            var context = new TemplateContext(variables, _options);

            var result = await fluidTemplate.RenderAsync(context);

            return result;
        }
        catch (Exception ex) when (ex is not TemplateException)
        {
            _logger.LogError(ex, "Template rendering failed");
            throw new TemplateException("Failed to render template", ex);
        }
    }

    public Task<TemplateValidationResult> ValidateAsync(
        string template,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return Task.FromResult(new TemplateValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Template cannot be empty" }
                });
            }

            if (!_parser.TryParse(template, out _, out var error))
            {
                return Task.FromResult(new TemplateValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { error }
                });
            }

            return Task.FromResult(new TemplateValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template validation failed");
            return Task.FromResult(new TemplateValidationResult
            {
                IsValid = false,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    public Task<List<string>> ExtractVariablesAsync(
        string template,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_parser.TryParse(template, out var fluidTemplate, out _))
            {
                return Task.FromResult(new List<string>());
            }

            var variables = new HashSet<string>();
            ExtractVariablesFromStatements(fluidTemplate.Statements, variables);

            return Task.FromResult(variables.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract template variables");
            return Task.FromResult(new List<string>());
        }
    }

    private void ExtractVariablesFromStatements(IEnumerable<Statement> statements, HashSet<string> variables)
    {
        foreach (var statement in statements)
        {
            if (statement is OutputStatement outputStatement)
            {
                ExtractVariablesFromExpression(outputStatement.Expression, variables);
            }
        }
    }

    private void ExtractVariablesFromExpression(Expression expression, HashSet<string> variables)
    {
        if (expression is MemberExpression memberExpression)
        {
            var segments = new List<string>();
            CollectMemberSegments(memberExpression, segments);
            if (segments.Any())
            {
                variables.Add(string.Join(".", segments));
            }
        }
    }

    private void CollectMemberSegments(Expression expression, List<string> segments)
    {
        if (expression is MemberExpression memberExpression)
        {
            if (memberExpression.Expression != null)
            {
                CollectMemberSegments(memberExpression.Expression, segments);
            }

            if (memberExpression.MemberName != null)
            {
                segments.Add(memberExpression.MemberName);
            }
        }
    }
}

/// <summary>
/// Template validation result.
/// </summary>
public sealed class TemplateValidationResult
{
    public required bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
}

/// <summary>
/// Template exception.
/// </summary>
public sealed class TemplateException : Exception
{
    public TemplateException(string message) : base(message) { }
    public TemplateException(string message, Exception innerException) : base(message, innerException) { }
}
