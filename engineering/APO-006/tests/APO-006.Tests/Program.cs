using WalkInTheWord.AIPublishing.Orchestration.Pipelines;

var tests = new (string Name, Func<ValueTask> Execute)[]
{
    ("executes stages sequentially", ExecutesStagesSequentially),
    ("preserves declared stage order", PreservesDeclaredStageOrder),
    ("rejects unregistered stage", RejectsUnregisteredStage),
    ("fails fast on stage failure", FailsFastOnStageFailure),
    ("tracks completed stages", TracksCompletedStages),
    ("rejects duplicate pipeline stage identifiers", RejectsDuplicatePipelineStageIdentifiers),
    ("rejects duplicate registered stage identifiers", RejectsDuplicateRegisteredStageIdentifiers),
    ("validates returned stage identity", ValidatesReturnedStageIdentity),
    ("requires output from successful stage", RequiresOutputFromSuccessfulStage)
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

static async ValueTask ExecutesStagesSequentially()
{
    var coordinator = new PipelineCoordinator(
    [
        new TransformStage("first", input => $"first({input})"),
        new TransformStage("second", input => $"second({input})")
    ]);

    var result = await coordinator.ExecuteAsync(
        new PipelineDefinition("pipeline-001", ["first", "second"]),
        "input");

    AssertEqual(PipelineOutcome.Succeeded, result.Outcome);
    AssertEqual("second(first(input))", result.CanonicalOutput);
    AssertSequenceEqual(["first", "second"], result.CompletedStageIds);
    AssertEqual<string?>(null, result.FailedStageId);
}

static async ValueTask PreservesDeclaredStageOrder()
{
    var executionLog = new List<string>();
    var coordinator = new PipelineCoordinator(
    [
        new TransformStage("a", input => { executionLog.Add("a"); return input + "a"; }),
        new TransformStage("b", input => { executionLog.Add("b"); return input + "b"; })
    ]);

    var result = await coordinator.ExecuteAsync(
        new PipelineDefinition("pipeline-002", ["b", "a"]),
        "");

    AssertEqual("ba", result.CanonicalOutput);
    AssertSequenceEqual(["b", "a"], executionLog);
}

static async ValueTask RejectsUnregisteredStage()
{
    var coordinator = new PipelineCoordinator([new TransformStage("registered", input => input)]);

    await AssertThrowsAsync<InvalidOperationException>(() =>
        coordinator.ExecuteAsync(
            new PipelineDefinition("pipeline-003", ["missing"]),
            "input"));
}

static async ValueTask FailsFastOnStageFailure()
{
    var trailingExecuted = false;
    var coordinator = new PipelineCoordinator(
    [
        new TransformStage("first", input => $"first({input})"),
        new FailingStage("failure", "APO_TEST_FAILURE"),
        new TransformStage("trailing", input => { trailingExecuted = true; return input; })
    ]);

    var result = await coordinator.ExecuteAsync(
        new PipelineDefinition("pipeline-004", ["first", "failure", "trailing"]),
        "input");

    AssertEqual(PipelineOutcome.Failed, result.Outcome);
    AssertSequenceEqual(["first"], result.CompletedStageIds);
    AssertEqual("failure", result.FailedStageId);
    AssertEqual("APO_TEST_FAILURE", result.DiagnosticCode);
    AssertFalse(trailingExecuted);
}

static async ValueTask TracksCompletedStages()
{
    var coordinator = new PipelineCoordinator(
    [
        new TransformStage("one", input => input + "1"),
        new TransformStage("two", input => input + "2"),
        new TransformStage("three", input => input + "3")
    ]);

    var result = await coordinator.ExecuteAsync(
        new PipelineDefinition("pipeline-005", ["one", "two", "three"]),
        "");

    AssertSequenceEqual(["one", "two", "three"], result.CompletedStageIds);
}

static ValueTask RejectsDuplicatePipelineStageIdentifiers()
{
    AssertThrows<ArgumentException>(() =>
        _ = new PipelineDefinition("pipeline-006", ["duplicate", "duplicate"]));
    return ValueTask.CompletedTask;
}

static ValueTask RejectsDuplicateRegisteredStageIdentifiers()
{
    AssertThrows<ArgumentException>(() =>
        _ = new PipelineCoordinator(
        [
            new TransformStage("duplicate", input => input),
            new TransformStage("duplicate", input => input)
        ]));
    return ValueTask.CompletedTask;
}

static async ValueTask ValidatesReturnedStageIdentity()
{
    var coordinator = new PipelineCoordinator([new MismatchedStage("expected", "different")]);

    await AssertThrowsAsync<InvalidOperationException>(() =>
        coordinator.ExecuteAsync(
            new PipelineDefinition("pipeline-007", ["expected"]),
            "input"));
}

static async ValueTask RequiresOutputFromSuccessfulStage()
{
    var coordinator = new PipelineCoordinator([new NullOutputStage("null-output")]);

    await AssertThrowsAsync<InvalidOperationException>(() =>
        coordinator.ExecuteAsync(
            new PipelineDefinition("pipeline-008", ["null-output"]),
            "input"));
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}

static void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"Expected '[{string.Join(", ", expected)}]', actual '[{string.Join(", ", actual)}]'.");
    }
}

static async ValueTask AssertThrowsAsync<TException>(Func<ValueTask<PipelineResult>> action)
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

file sealed class TransformStage(string stageId, Func<string, string> transform) : IPipelineStage
{
    public string StageId { get; } = stageId;

    public ValueTask<PipelineStageResult> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            new PipelineStageResult(
                StageId,
                PipelineStageOutcome.Succeeded,
                transform(canonicalInput),
                DiagnosticCode: null));
    }
}

file sealed class FailingStage(string stageId, string diagnosticCode) : IPipelineStage
{
    public string StageId { get; } = stageId;

    public ValueTask<PipelineStageResult> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            new PipelineStageResult(
                StageId,
                PipelineStageOutcome.Failed,
                CanonicalOutput: null,
                diagnosticCode));
    }
}

file sealed class MismatchedStage(string stageId, string returnedStageId) : IPipelineStage
{
    public string StageId { get; } = stageId;

    public ValueTask<PipelineStageResult> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            new PipelineStageResult(
                returnedStageId,
                PipelineStageOutcome.Succeeded,
                canonicalInput,
                DiagnosticCode: null));
    }
}

file sealed class NullOutputStage(string stageId) : IPipelineStage
{
    public string StageId { get; } = stageId;

    public ValueTask<PipelineStageResult> ExecuteAsync(
        string canonicalInput,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            new PipelineStageResult(
                StageId,
                PipelineStageOutcome.Succeeded,
                CanonicalOutput: null,
                DiagnosticCode: null));
    }
}
