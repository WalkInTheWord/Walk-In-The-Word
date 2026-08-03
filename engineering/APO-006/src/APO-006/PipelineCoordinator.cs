using System.Collections.Immutable;

namespace WalkInTheWord.AIPublishing.Orchestration.Pipelines;

public sealed record PipelineDefinition
{
    public PipelineDefinition(string pipelineId, IEnumerable<string> stageIds)
    {
        if (string.IsNullOrWhiteSpace(pipelineId))
        {
            throw new ArgumentException("PipelineId is required.", nameof(pipelineId));
        }

        ArgumentNullException.ThrowIfNull(stageIds);
        var orderedStageIds = stageIds.ToImmutableArray();

        if (orderedStageIds.IsDefaultOrEmpty)
        {
            throw new ArgumentException("A pipeline must contain at least one stage.", nameof(stageIds));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stageId in orderedStageIds)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                throw new ArgumentException("Pipeline stage identifiers must be non-empty.", nameof(stageIds));
            }

            if (!seen.Add(stageId))
            {
                throw new ArgumentException($"Duplicate pipeline stage identifier '{stageId}'.", nameof(stageIds));
            }
        }

        PipelineId = pipelineId;
        StageIds = orderedStageIds;
    }

    public string PipelineId { get; }

    public ImmutableArray<string> StageIds { get; }
}

public enum PipelineStageOutcome
{
    Succeeded = 0,
    Failed = 1
}

public sealed record PipelineStageResult(
    string StageId,
    PipelineStageOutcome Outcome,
    string? CanonicalOutput,
    string? DiagnosticCode);

public interface IPipelineStage
{
    string StageId { get; }

    ValueTask<PipelineStageResult> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default);
}

public enum PipelineOutcome
{
    Succeeded = 0,
    Failed = 1
}

public sealed record PipelineResult(
    string PipelineId,
    PipelineOutcome Outcome,
    string? CanonicalOutput,
    ImmutableArray<string> CompletedStageIds,
    string? FailedStageId,
    string? DiagnosticCode);

public sealed class PipelineCoordinator
{
    private readonly ImmutableDictionary<string, IPipelineStage> _stages;

    public PipelineCoordinator(IEnumerable<IPipelineStage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var builder = ImmutableDictionary.CreateBuilder<string, IPipelineStage>(StringComparer.Ordinal);
        foreach (var stage in stages)
        {
            ArgumentNullException.ThrowIfNull(stage);

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                throw new ArgumentException("Registered stages must have non-empty identifiers.", nameof(stages));
            }

            if (!builder.TryAdd(stage.StageId, stage))
            {
                throw new ArgumentException($"Duplicate registered stage identifier '{stage.StageId}'.", nameof(stages));
            }
        }

        _stages = builder.ToImmutable();
    }

    public async ValueTask<PipelineResult> ExecuteAsync(
        PipelineDefinition definition,
        string canonicalInput,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(canonicalInput);

        ValidateRegistrations(definition);

        var completed = ImmutableArray.CreateBuilder<string>();
        var currentInput = canonicalInput;

        foreach (var stageId in definition.StageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _stages[stageId]
                .ExecuteAsync(currentInput, cancellationToken)
                .ConfigureAwait(false);

            ValidateStageResult(stageId, result);

            if (result.Outcome == PipelineStageOutcome.Failed)
            {
                return new PipelineResult(
                    definition.PipelineId,
                    PipelineOutcome.Failed,
                    CanonicalOutput: null,
                    CompletedStageIds: completed.ToImmutable(),
                    FailedStageId: stageId,
                    DiagnosticCode: result.DiagnosticCode ?? "APO_PIPELINE_STAGE_FAILED");
            }

            completed.Add(stageId);
            currentInput = result.CanonicalOutput!;
        }

        return new PipelineResult(
            definition.PipelineId,
            PipelineOutcome.Succeeded,
            currentInput,
            completed.ToImmutable(),
            FailedStageId: null,
            DiagnosticCode: null);
    }

    private void ValidateRegistrations(PipelineDefinition definition)
    {
        foreach (var stageId in definition.StageIds)
        {
            if (!_stages.ContainsKey(stageId))
            {
                throw new InvalidOperationException(
                    $"Pipeline '{definition.PipelineId}' references unregistered stage '{stageId}'.");
            }
        }
    }

    private static void ValidateStageResult(string expectedStageId, PipelineStageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!string.Equals(expectedStageId, result.StageId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stage '{expectedStageId}' returned result for '{result.StageId}'.");
        }

        if (!Enum.IsDefined(result.Outcome))
        {
            throw new InvalidOperationException($"Stage '{expectedStageId}' returned an unknown outcome.");
        }

        if (result.Outcome == PipelineStageOutcome.Succeeded && result.CanonicalOutput is null)
        {
            throw new InvalidOperationException(
                $"Successful stage '{expectedStageId}' must return canonical output.");
        }
    }
}
