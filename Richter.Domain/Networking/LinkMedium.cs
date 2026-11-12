namespace Richter.Domain.Networking;

/// <summary>
/// What carries a <see cref="Link"/>'s signal, which decides when the link is up and how slow it
/// is.
/// </summary>
public enum LinkMedium
{
    /// <summary>
    /// Optical fibre along the ground, between two networks on the ground of the same body. Always
    /// up.
    /// </summary>
    Cable = 0,

    /// <summary> A radio beam, up while each end can see the other. </summary>
    Radio = 1,

    /// <summary> A laser beam. It follows the same rules as radio for now. </summary>
    Laser = 2,
}
