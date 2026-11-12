namespace Richter.Json.Documents;

/// <summary>
/// Components and their settings: on a device kind, the components its devices have; on a
/// device, settings that replace its kind's for the components the kind has.
/// </summary>
/// <remarks> A new component is a property here, named as it's written in files. </remarks>
internal record ComponentsDocument
{
    public LockDocument? Lock { get; init; }
}
