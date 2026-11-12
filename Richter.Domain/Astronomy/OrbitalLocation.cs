namespace Richter.Domain.Astronomy;

/// <summary> On an object orbiting a body, such as a satellite or space station. </summary>
public record OrbitalLocation : Location
{
    public required Orbit Orbit { get; init; }
}
