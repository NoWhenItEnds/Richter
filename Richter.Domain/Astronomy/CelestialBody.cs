namespace Richter.Domain.Astronomy;

/// <summary>
/// A natural body, such as a star, planet or moon, that networks can be on or orbit.
/// </summary>
/// <remarks>
/// Bodies form a tree through <see cref="Astronomy.Orbit.Parent"/>: each orbits its parent, up to
/// the root of the system (its star), which has no orbit and sits at the origin of the reference
/// frame. <see cref="StarSystem"/> checks that. Artificial satellites aren't bodies. A network on
/// one has an <see cref="OrbitalLocation"/>, because networking needs a satellite's path but not
/// its mass, size or spin.
/// </remarks>
public class CelestialBody
{
    public required CelestialBodyId Id { get; init; }

    public required String Name { get; init; }

    /// <summary>
    /// The body's mass times the gravitational constant (GM), in m³/s². It sets the period of
    /// everything that orbits the body.
    /// </summary>
    /// <remarks>
    /// Stored instead of mass because published values are given as GM, which is known far more
    /// precisely than either mass or the gravitational constant on its own.
    /// </remarks>
    public required Double GravitationalParameter
    {
        get;
        init
        {
            field = Require.Positive(value, nameof(GravitationalParameter));
        }
    }

    /// <summary> Mean radius in metres. Bodies are treated as spheres. </summary>
    public required Double Radius
    {
        get;
        init
        {
            field = Require.Positive(value, nameof(Radius));
        }
    }

    /// <summary> The body's path around its parent. Null for the root of the system. </summary>
    public required Orbit? Orbit { get; init; }

    public required Rotation Rotation { get; init; }
}
