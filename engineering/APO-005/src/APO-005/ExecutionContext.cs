using System.Collections.Immutable;

namespace WalkInTheWord.AIPublishing.Orchestration.Execution;

public enum OrchestrationExecutionState
{
    Created = 0,
    Validating = 1,
    Ready = 2,
    Running = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
    Rejected = 7
}

public sealed record OrchestrationExecutionContext
{
    public OrchestrationExecutionContext(
        string executionId,
        string correlationId,
        OrchestrationExecutionState state,
        IEnumerable<KeyValuePair<string, string>>? metadata = null,
        bool isCancellationRequested = false)
    {
        if (string.IsNullOrWhiteSpace(executionId))
        {
            throw new ArgumentException("ExecutionId is required.", nameof(executionId));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown orchestration state.");
        }

        ExecutionId = executionId;
        CorrelationId = correlationId;
        State = state;
        Metadata = CreateMetadata(metadata);
        IsCancellationRequested = isCancellationRequested;
    }

    public string ExecutionId { get; }

    public string CorrelationId { get; }

    public OrchestrationExecutionState State { get; }

    public ImmutableSortedDictionary<string, string> Metadata { get; }

    public bool IsCancellationRequested { get; }

    public OrchestrationExecutionContext WithState(OrchestrationExecutionState state)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown orchestration state.");
        }

        return state == State
            ? this
            : new OrchestrationExecutionContext(
                ExecutionId,
                CorrelationId,
                state,
                Metadata,
                IsCancellationRequested);
    }

    public OrchestrationExecutionContext WithMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Metadata key is required.", nameof(key));
        }

        ArgumentNullException.ThrowIfNull(value);

        var updated = Metadata.SetItem(key, value);

        return ReferenceEquals(updated, Metadata)
            ? this
            : new OrchestrationExecutionContext(
                ExecutionId,
                CorrelationId,
                State,
                updated,
                IsCancellationRequested);
    }

    public OrchestrationExecutionContext WithoutMetadata(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Metadata key is required.", nameof(key));
        }

        var updated = Metadata.Remove(key);

        return ReferenceEquals(updated, Metadata)
            ? this
            : new OrchestrationExecutionContext(
                ExecutionId,
                CorrelationId,
                State,
                updated,
                IsCancellationRequested);
    }

    public OrchestrationExecutionContext RequestCancellation()
    {
        return IsCancellationRequested
            ? this
            : new OrchestrationExecutionContext(
                ExecutionId,
                CorrelationId,
                State,
                Metadata,
                isCancellationRequested: true);
    }

    private static ImmutableSortedDictionary<string, string> CreateMetadata(
        IEnumerable<KeyValuePair<string, string>>? metadata)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, string>(
            StringComparer.Ordinal);

        if (metadata is null)
        {
            return builder.ToImmutable();
        }

        foreach (var pair in metadata)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("Metadata keys must be non-empty.", nameof(metadata));
            }

            ArgumentNullException.ThrowIfNull(pair.Value);

            if (!builder.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException(
                    $"Duplicate metadata key '{pair.Key}'.",
                    nameof(metadata));
            }
        }

        return builder.ToImmutable();
    }
}
