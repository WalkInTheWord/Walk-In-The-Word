using System.Collections.Immutable;

namespace WalkInTheWord.AIPublishing.Orchestration;

public enum OrchestrationState
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

public sealed record OrchestrationTransition(
    OrchestrationState From,
    OrchestrationState To);

public static class OrchestrationStateMachine
{
    private static readonly ImmutableDictionary<OrchestrationState, ImmutableArray<OrchestrationState>>
        AllowedTransitions = new Dictionary<OrchestrationState, ImmutableArray<OrchestrationState>>
        {
            [OrchestrationState.Created] =
                [OrchestrationState.Validating, OrchestrationState.Cancelled],
            [OrchestrationState.Validating] =
                [OrchestrationState.Ready, OrchestrationState.Rejected, OrchestrationState.Failed, OrchestrationState.Cancelled],
            [OrchestrationState.Ready] =
                [OrchestrationState.Running, OrchestrationState.Cancelled],
            [OrchestrationState.Running] =
                [OrchestrationState.Succeeded, OrchestrationState.Failed, OrchestrationState.Cancelled],
            [OrchestrationState.Succeeded] = [],
            [OrchestrationState.Failed] = [],
            [OrchestrationState.Cancelled] = [],
            [OrchestrationState.Rejected] = []
        }.ToImmutableDictionary();

    public static bool IsTerminal(OrchestrationState state) =>
        GetAllowedTransitions(state).IsEmpty;

    public static bool CanTransition(
        OrchestrationState from,
        OrchestrationState to) =>
        GetAllowedTransitions(from).Contains(to);

    public static OrchestrationTransition CreateTransition(
        OrchestrationState from,
        OrchestrationState to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Transition from '{from}' to '{to}' is not permitted.");
        }

        return new OrchestrationTransition(from, to);
    }

    public static ImmutableArray<OrchestrationState> GetAllowedTransitions(
        OrchestrationState state) =>
        AllowedTransitions.TryGetValue(state, out var transitions)
            ? transitions
            : throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown orchestration state.");
}
