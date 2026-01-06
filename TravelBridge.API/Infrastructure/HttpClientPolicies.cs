using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace TravelBridge.API.Infrastructure;

/// <summary>
/// Provides Polly retry policies for HTTP clients.
/// </summary>
public static class HttpClientPolicies
{
    /// <summary>
    /// Creates a standard retry policy with exponential backoff for external API calls.
    /// Retries 3 times with delays of 200ms, 400ms, 800ms.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetStandardRetryPolicy(IServiceProvider serviceProvider, string clientName)
    {
        var logger = serviceProvider.GetService<ILogger<Program>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger?.LogWarning(
                        "HTTP {ClientName} retry {RetryAttempt} after {DelayMs}ms due to {StatusCode}",
                        clientName,
                        retryAttempt,
                        timespan.TotalMilliseconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                });
    }

    /// <summary>
    /// Creates a fast-fail retry policy for payment operations.
    /// Only 1 retry with 100ms delay - payments should fail fast.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetPaymentRetryPolicy(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<Program>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 1,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(100),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger?.LogWarning(
                        "Payment HTTP retry {RetryAttempt} after {DelayMs}ms due to {Reason}",
                        retryAttempt,
                        timespan.TotalMilliseconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures.
    /// Opens circuit after 5 consecutive failures, stays open for 30 seconds.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider serviceProvider, string clientName)
    {
        var logger = serviceProvider.GetService<ILogger<Program>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    logger?.LogWarning(
                        "Circuit breaker OPEN for {ClientName} for {BreakSeconds}s due to {Reason}",
                        clientName,
                        breakDelay.TotalSeconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                },
                onReset: () =>
                {
                    logger?.LogInformation("Circuit breaker RESET for {ClientName}", clientName);
                });
    }
}
