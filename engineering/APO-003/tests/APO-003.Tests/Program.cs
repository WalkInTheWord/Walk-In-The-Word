using WalkInTheWord.AIPublishing.Orchestration.Dispatching;

var tests = new (string Name, Func<ValueTask> Execute)[]
{
    ("dispatches registered stage", DispatchesRegisteredStage),
    ("rejects missing stage", RejectsMissingStage),
    ("rejects duplicate registration", RejectsDuplicateRegistration),
    ("is independent of registration order", IsIndependentOfRegistrationOrder),
    ("validates request identifiers", ValidatesRequestIdentifiers)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        await test.Execute();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL: {test.Name}: {exception.Message}");
    }
}

foreach (var failure in failures)
{
    Console.Error.WriteLine(failure);
}

return failures.Count == 0 ? 0 : 1;

static async ValueTask DispatchesRegisteredStage()
{
    var dispatcher = new WorkflowDispatcher([new EchoStage("editorial")]);
    var result = await dispatcher.DispatchAsync(new DispatchRequest("exec-001", "editorial", "input"));

    AssertEqual(DispatchOutcome.Dispatched, result.Outcome);
    AssertEqual("editorial:input", result.CanonicalOutput);
    AssertEqual<string?>(null, result.DiagnosticCode);
}

static async ValueTask RejectsMissingStage()
{
    var dispatcher = new WorkflowDispatcher([]);
    var result = await dispatcher.DispatchAsync(new DispatchRequest("exec-002", "missing", "input"));

    AssertEqual(DispatchOutcome.StageNotFound, result.Outcome);
    AssertEqual<string?>(null, result.CanonicalOutput);
    AssertEqual("APO_DISPATCH_STAGE_NOT_FOUND", result.DiagnosticCode);
}

static ValueTask RejectsDuplicateRegistration()
{
    AssertThrows<ArgumentException>(() =>
        _ = new WorkflowDispatcher([new EchoStage("qa"), new EchoStage("qa")]));

    return ValueTask.CompletedTask;
}

static async ValueTask IsIndependentOfRegistrationOrder()
{
    var request = new DispatchRequest("exec-003", "publish", "payload");
    var first = new WorkflowDispatcher([new EchoStage("publish"), new EchoStage("research")]);
    var second = new WorkflowDispatcher([new EchoStage("research"), new EchoStage("publish")]);

    var firstResult = await first.DispatchAsync(request);
    var secondResult = await second.DispatchAsync(request);

    AssertEqual(firstResult, secondResult);
}

static async ValueTask ValidatesRequestIdentifiers()
{
    var dispatcher = new WorkflowDispatcher([new EchoStage("qa")]);

    await AssertThrowsAsync<ArgumentException>(() =>
        dispatcher.DispatchAsync(new DispatchRequest("", "qa", "input")));

    await AssertThrowsAsync<ArgumentException>(() =>
        dispatcher.DispatchAsync(new DispatchRequest("exec-004", "", "input")));
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
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

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static async ValueTask AssertThrowsAsync<TException>(Func<ValueTask<DispatchResult>> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

file sealed class EchoStage(string stageId) : IWorkflowDispatchStage
{
    public string StageId { get; } = stageId;

    public ValueTask<string> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult($"{StageId}:{canonicalInput}");
    }
}
