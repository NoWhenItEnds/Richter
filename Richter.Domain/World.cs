using Richter.Domain.Astronomy;
using Richter.Domain.Networking;

namespace Richter.Domain;

/// <summary>
/// Everything a game simulates: the star system, the networks in it, and the current time.
/// </summary>
/// <remarks>
/// The host moves time forward by calling <see cref="Advance"/> once a frame. Nothing in the
/// domain reads the system clock, so a world can start at any instant.
/// </remarks>
public class World
{
    public World(StarSystem starSystem, DateTimeOffset time)
    {
        StarSystem = starSystem;
        Networks = new NetworkGraph(starSystem);
        Time = time;
    }

    public StarSystem StarSystem { get; }

    public NetworkGraph Networks { get; }

    public DateTimeOffset Time { get; private set; }

    /// <summary>
    /// Simulated time per second of real time: 0 pauses, 1 is real time, and 1000 runs a
    /// thousand times faster. Starts at 1.
    /// </summary>
    public Double TimeScale
    {
        get;
        set
        {
            if (!Double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The time scale must be a finite number, zero or more.");
            }

            field = value;
        }
    } = 1;

    /// <summary>
    /// Moves <see cref="Time"/> forward by <paramref name="realElapsed"/> times
    /// <see cref="TimeScale"/>.
    /// </summary>
    /// <param name="realElapsed"> Real time since the last call, normally one frame. </param>
    public void Advance(TimeSpan realElapsed)
    {
        if (realElapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(realElapsed), realElapsed, "Elapsed time can't be negative.");
        }

        Time += realElapsed * TimeScale;
    }
}
