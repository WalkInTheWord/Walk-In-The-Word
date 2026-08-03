using WalkInTheWord.AIPublishing.Orchestration.Execution;

var tests = new (string Name, Action Execute)[]
{
    ("creates immutable context", CreatesImmutableContext),
    ("orders metadata canonically", OrdersMetadataCanonically),
    ("updates state by returning new context", UpdatesStateByReturningNewContext),
    ("updates metadata by returning new context", UpdatesMetadataByReturningNewContext),
    ("removes metadata by returning new context", RemovesMetadataByReturningNewContext),
    ("requests cancellation by returning new context", RequestsCancellationByReturningNewContext),
    ("preserves original instance after updates", PreservesOriginalInstanceAfterUpdates),
    ("rejects duplicate metadata keys", RejectsDuplicateMetadataKeys),
    ("validates identifiers and states", ValidatesIdentifiersAndStates)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        test.Execute();
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

static void CreatesImmutableContext()
{
    var context = new OrchestrationExecutionContext(
        "exec-001",
        "corr-001",
        OrchestrationExecutionState.Created);

    AssertEqual("exec-001", context.ExecutionId);
    AssertEqual("corr-001", context.CorrelationId);
    AssertEqual(OrchestrationExecutionState.Created, context.State);
    AssertEqual(0, context.Metadata.Count);
    AssertFalse(context.IsCancellationRequested);
}

static void OrdersMetadataCanonically()
{
    var context = new OrchestrationExecutionContext(
        "exec-002",
        "corr-002",
        OrchestrationExecutionState.Ready,
        [
            new("zeta", "3"),
            new("alpha", "1"),
            new("middle", "2")
        ]);

    AssertSequenceEqual(
        ["alpha", "middle", "zeta"],
        context.Metadata.Keys);
}

static void UpdatesStateByReturningNewContext()
{
    var original = new OrchestrationExecutionContext(
        "exec-003",
        "corr-003",
        OrchestrationExecutionState.Created);

    var updated = original.WithState(OrchestrationExecutionState.Validating);

    AssertNotSame(original, updated);
    AssertEqual(OrchestrationExecutionState.Created, original.State);
    AssertEqual(OrchestrationExecutionState.Validating, updated.State);
}

static void UpdatesMetadataByReturningNewContext()
{
    var original = new OrchestrationExecutionContext(
        "exec-004",
        "corr-004",
        OrchestrationExecutionState.Running);

    var updated = original.WithMetadata("stage", "editorial");

    AssertNotSame(original, updated);
    AssertEqual(0, original.Metadata.Count);
    AssertEqual("editorial", updated.Metadata["stage"]);
}

static void RemovesMetadataByReturningNewContext()
{
    var original = new OrchestrationExecutionContext(
        "exec-005",
        "corr-005",
        OrchestrationExecutionState.Running,
        [new("stage", "qa")]);

    var updated = original.WithoutMetadata("stage");

    AssertNotSame(original, updated);
    AssertEqual(1, original.Metadata.Count);
    AssertEqual(0, updated.Metadata.Count);
}

static void RequestsCancellationByReturningNewContext()
{
    var original = new OrchestrationExecutionContext(
        "exec-006",
        "corr-006",
        OrchestrationExecutionState.Running);

    var updated = original.RequestCancellation();

    AssertNotSame(original, updated);
    AssertFalse(original.IsCancellationRequested);
    AssertTrue(updated.IsCancellationRequested);
}

static void PreservesOriginalInstanceAfterUpdates()
{
    var original = new OrchestrationExecutionContext(
        "exec-007",
        "corr-007",
        OrchestrationExecutionState.Ready,
        [new("a", "1")]);

    _ = original
        .WithState(OrchestrationExecutionState.Running)
        .WithMetadata("b", "2")
        .RequestCancellation();

    AssertEqual(OrchestrationExecutionState.Ready, original.State);
    AssertSequenceEqual(["a"], original.Metadata.Keys);
    AssertFalse(original.IsCancellationRequested);
}

static void RejectsDuplicateMetadataKeys()
{
    AssertThrows<ArgumentException>(() =>
        _ = new OrchestrationExecutionContext(
            "exec-008",
            "corr-008",
            OrchestrationExecutionState.Created,
            [
                new("duplicate", "one"),
                new("duplicate", "two")
            ]));
}

static void ValidatesIdentifiersAndStates()
{
    AssertThrows<ArgumentException>(() =>
        _ = new OrchestrationExecutionContext(
            "",
            "corr",
            OrchestrationExecutionState.Created));

    AssertThrows<ArgumentException>(() =>
        _ = new OrchestrationExecutionContext(
            "exec",
            "",
            OrchestrationExecutionState.Created));

    AssertThrows<ArgumentOutOfRangeException>(() =>
        _ = new OrchestrationExecutionContext(
            "exec",
            "corr",
            (OrchestrationExecutionState)999));
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
    }
}

static void AssertTrue(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}

static void AssertNotSame(object first, object second)
{
    if (ReferenceEquals(first, second))
    {
        throw new InvalidOperationException("Expected different object references.");
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
