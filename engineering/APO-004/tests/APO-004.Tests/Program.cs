using System.Collections.Immutable;
using WalkInTheWord.AIPublishing.Orchestration.Registration;

var tests = new (string Name, Action Execute)[]
{
    ("orders registrations canonically", OrdersRegistrationsCanonically),
    ("is independent of input order", IsIndependentOfInputOrder),
    ("rejects duplicate identifiers", RejectsDuplicateIdentifiers),
    ("looks up registrations without execution", LooksUpRegistrations),
    ("returns false for missing identifiers", ReturnsFalseForMissingIdentifiers),
    ("validates descriptor values", ValidatesDescriptorValues),
    ("exposes immutable registration array", ExposesImmutableRegistrationArray)
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

static void OrdersRegistrationsCanonically()
{
    var registrations = new StageRegistrationCollection(
    [
        new("publish", "PublicationStage"),
        new("editorial", "EditorialStage"),
        new("qa", "QualityAssuranceStage")
    ]);

    AssertSequenceEqual(
        ["editorial", "publish", "qa"],
        registrations.Registrations.Select(static item => item.StageId));
}

static void IsIndependentOfInputOrder()
{
    var first = new StageRegistrationCollection(
    [
        new("publish", "PublicationStage"),
        new("editorial", "EditorialStage")
    ]);

    var second = new StageRegistrationCollection(
    [
        new("editorial", "EditorialStage"),
        new("publish", "PublicationStage")
    ]);

    AssertSequenceEqual(first.Registrations, second.Registrations);
}

static void RejectsDuplicateIdentifiers()
{
    AssertThrows<ArgumentException>(() =>
        _ = new StageRegistrationCollection(
        [
            new("qa", "FirstStage"),
            new("qa", "SecondStage")
        ]));
}

static void LooksUpRegistrations()
{
    var registrations = new StageRegistrationCollection(
    [
        new("qa", "QualityAssuranceStage")
    ]);

    if (!registrations.TryGet("qa", out var registration))
    {
        throw new InvalidOperationException("Expected registration to be found.");
    }

    AssertEqual("QualityAssuranceStage", registration!.StageType);
}

static void ReturnsFalseForMissingIdentifiers()
{
    var registrations = new StageRegistrationCollection([]);

    AssertFalse(registrations.TryGet("missing", out var missing));
    AssertEqual<StageRegistrationDescriptor?>(null, missing);
    AssertFalse(registrations.TryGet("", out var empty));
    AssertEqual<StageRegistrationDescriptor?>(null, empty);
}

static void ValidatesDescriptorValues()
{
    AssertThrows<ArgumentException>(() => _ = new StageRegistrationDescriptor("", "Stage"));
    AssertThrows<ArgumentException>(() => _ = new StageRegistrationDescriptor("qa", ""));
}

static void ExposesImmutableRegistrationArray()
{
    var registrations = new StageRegistrationCollection(
    [
        new("qa", "QualityAssuranceStage")
    ]);

    ImmutableArray<StageRegistrationDescriptor> snapshot = registrations.Registrations;
    AssertEqual(1, snapshot.Length);
    AssertEqual("qa", snapshot[0].StageId);
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
