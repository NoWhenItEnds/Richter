using System.Globalization;
using Brutal.GlfwApi;
using Richter.Domain;

namespace Richter;

/// <summary>
/// The keys that set how fast game time runs: Space pauses and resumes, and + and - step through
/// the speeds.
/// </summary>
/// <remarks>
/// Speeds go up in tens, as in most simulation games, from real time to a million times faster,
/// where Earth goes round the Sun in about half a minute. Pausing keeps the chosen speed, so
/// resuming goes back to it.
/// </remarks>
internal class TimeControls
{
    private static readonly Double[] Speeds = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000];

    private readonly World _world;
    private Int32 _speed;
    private Boolean _paused;

    public TimeControls(World world)
    {
        _world = world;
        Apply();
    }

    /// <summary> The speed, and whether time is paused, for the title bar. </summary>
    public String Status
    {
        get
        {
            String speed = $"×{Speeds[_speed].ToString("N0", CultureInfo.InvariantCulture)}";
            return _paused ? $"paused ({speed})" : speed;
        }
    }

    /// <summary>
    /// Handles a key event from GLFW. Held + and - keep stepping; held Space doesn't.
    /// </summary>
    public void OnKey(GlfwKey key, GlfwKeyAction action)
    {
        if (action != GlfwKeyAction.Release)
        {
            switch (key)
            {
                case GlfwKey.Space when action == GlfwKeyAction.Press:
                    _paused = !_paused;
                    break;
                case GlfwKey.Equal or GlfwKey.KpAdd:
                    _speed = Math.Min(_speed + 1, Speeds.Length - 1);
                    break;
                case GlfwKey.Minus or GlfwKey.KpSubtract:
                    _speed = Math.Max(_speed - 1, 0);
                    break;
                default:
                    break;
            }

            Apply();
        }
    }

    private void Apply()
    {
        _world.TimeScale = _paused ? 0 : Speeds[_speed];
    }
}
