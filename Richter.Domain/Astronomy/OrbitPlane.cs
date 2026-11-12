namespace Richter.Domain.Astronomy;

/// <summary> The plane an <see cref="Orbit"/>'s angles are measured against. </summary>
public enum OrbitPlane
{
    /// <summary>
    /// The parent's equator, with the node measured from the system's reference direction
    /// projected onto it: for Earth, the March equinox. Published satellite elements use this.
    /// </summary>
    ParentEquator = 0,

    /// <summary>
    /// The star system's reference plane, with the node measured from its reference direction:
    /// for the Solar System, the ecliptic and the March equinox. Published planet and Moon
    /// elements use this.
    /// </summary>
    ReferencePlane = 1,
}
