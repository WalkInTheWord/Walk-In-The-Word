using System.Collections.Immutable;

namespace WalkInTheWord.AIPublishing.Orchestration.Registration;

public sealed record StageRegistrationDescriptor
{
    public StageRegistrationDescriptor(string stageId, string stageType)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            throw new ArgumentException("StageId is required.", nameof(stageId));
        }

        if (string.IsNullOrWhiteSpace(stageType))
        {
            throw new ArgumentException("StageType is required.", nameof(stageType));
        }

        StageId = stageId;
        StageType = stageType;
    }

    public string StageId { get; }

    public string StageType { get; }
}

public sealed class StageRegistrationCollection
{
    private readonly ImmutableArray<StageRegistrationDescriptor> _registrations;
    private readonly ImmutableDictionary<string, StageRegistrationDescriptor> _byStageId;

    public StageRegistrationCollection(IEnumerable<StageRegistrationDescriptor> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        var byId = ImmutableDictionary.CreateBuilder<string, StageRegistrationDescriptor>(
            StringComparer.Ordinal);

        foreach (var registration in registrations)
        {
            ArgumentNullException.ThrowIfNull(registration);

            if (!byId.TryAdd(registration.StageId, registration))
            {
                throw new ArgumentException(
                    $"Duplicate stage identifier '{registration.StageId}'.",
                    nameof(registrations));
            }
        }

        _registrations = byId.Values
            .OrderBy(static registration => registration.StageId, StringComparer.Ordinal)
            .ToImmutableArray();

        _byStageId = byId.ToImmutable();
    }

    public ImmutableArray<StageRegistrationDescriptor> Registrations => _registrations;

    public int Count => _registrations.Length;

    public bool TryGet(string stageId, out StageRegistrationDescriptor? registration)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            registration = null;
            return false;
        }

        return _byStageId.TryGetValue(stageId, out registration);
    }
}
