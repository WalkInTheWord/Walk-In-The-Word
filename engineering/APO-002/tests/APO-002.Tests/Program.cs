using WalkInTheWord.AIPublishing.Orchestration;

Assert(OrchestrationStateMachine.CanTransition(
    OrchestrationState.Created,
    OrchestrationState.Validating));

Assert(OrchestrationStateMachine.CanTransition(
    OrchestrationState.Validating,
    OrchestrationState.Ready));

Assert(OrchestrationStateMachine.CanTransition(
    OrchestrationState.Running,
    OrchestrationState.Succeeded));

Assert(!OrchestrationStateMachine.CanTransition(
    OrchestrationState.Succeeded,
    OrchestrationState.Running));

Assert(OrchestrationStateMachine.IsTerminal(OrchestrationState.Succeeded));
Assert(OrchestrationStateMachine.IsTerminal(OrchestrationState.Failed));
Assert(OrchestrationStateMachine.IsTerminal(OrchestrationState.Cancelled));
Assert(OrchestrationStateMachine.IsTerminal(OrchestrationState.Rejected));
Assert(!OrchestrationStateMachine.IsTerminal(OrchestrationState.Running));

var transition = OrchestrationStateMachine.CreateTransition(
    OrchestrationState.Ready,
    OrchestrationState.Running);

Assert(transition == new OrchestrationTransition(
    OrchestrationState.Ready,
    OrchestrationState.Running));

AssertThrows<InvalidOperationException>(() =>
    OrchestrationStateMachine.CreateTransition(
        OrchestrationState.Created,
        OrchestrationState.Succeeded));

Console.WriteLine("APO-002 tests passed.");

static void Assert(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Assertion failed.");
    }
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(
        $"Expected exception '{typeof(TException).Name}' was not thrown.");
}
