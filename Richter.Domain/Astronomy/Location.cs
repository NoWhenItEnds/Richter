namespace Richter.Domain.Astronomy;

/// <summary>
/// Where something is placed relative to a celestial body: fixed to it
/// (<see cref="SurfaceLocation"/>) or in orbit around it (<see cref="OrbitalLocation"/>).
/// </summary>
public abstract record Location;
