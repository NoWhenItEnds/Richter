namespace Richter.Json.Documents;

/// <summary> A link between two networks in a scenario file. </summary>
internal record LinkDocument
{
    /// <summary> The <see cref="NetworkDocument.Id"/>s of the two networks it joins. </summary>
    public required IReadOnlyList<String> Between { get; init; }

    /// <summary> <c>cable</c>, <c>radio</c> or <c>laser</c>. </summary>
    public required String Medium { get; init; }
}
