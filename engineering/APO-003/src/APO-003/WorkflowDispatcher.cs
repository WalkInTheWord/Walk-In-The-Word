using System.Collections.Immutable;

namespace WalkInTheWord.AIPublishing.Orchestration.Dispatching;

public enum DispatchOutcome
{
    Dispatched = 0,
    StageNotFound = 1
}

public sealed record DispatchRequest(
    string ExecutionId,
    string StageId,
    string CanonicalInput);

public sealed record DispatchResult(
    string ExecutionId,
    string StageId,
    DispatchOutcome Outcome,
    string? CanonicalOutput,
    string? DiagnosticCode);

public interface IWorkflowDispatchStage
{
    string StageId { get; }

    ValueTask<string> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default);
}

public sealed class WorkflowDispatcher
{
    private readonly ImmutableDictionary<string, IWorkflowDispatchStage> _stages;

    public WorkflowDispatcher(IEnumerable<IWorkflowDispatchStage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var builder = ImmutableDictionary.CreateBuilder<string, IWorkflowDispatchStage>(
            StringComparer.Ordinal);

        foreach (var stage in stages)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                throw new ArgumentException("A workflow stage must have a non-empty stage identifier.", nameof(stages));
            }

            if (!builder.TryAdd(stage.StageId, stage))
            {
                throw new ArgumentException(
                    $"Duplicate workflow stage identifier '{stage.StageId}'.",
                    nameof(stages));
            }
        }

        _stages = builder.ToImmutable();
    }

    public async ValueTask<DispatchResult> DispatchAsync(
        DispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ExecutionId))
        {
            throw new ArgumentException("ExecutionId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.StageId))
        {
            throw new ArgumentException("StageId is required.", nameof(request));
        }

        ArgumentNullException.ThrowIfNull(request.CanonicalInput);

        if (!_stages.TryGetValue(request.StageId, out var stage))
        {
            return new DispatchResult(
                request.ExecutionId,
                request.StageId,
                DispatchOutcome.StageNotFound,
                CanonicalOutput: null,
                DiagnosticCode: "APO_DISPATCH_STAGE_NOT_FOUND");
        }

        var output = await stage
            .ExecuteAsync(request.CanonicalInput, cancellationToken)
            .ConfigureAwait(false);

        return new DispatchResult(
            request.ExecutionId,
            request.StageId,
            DispatchOutcome.Dispatched,
            output,
            DiagnosticCode: null);
    }
}
