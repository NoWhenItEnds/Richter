using Brutal.GlfwApi;
using Brutal.Numerics;
using Richter.Domain;
using Richter.Domain.Astronomy;
using Richter.Domain.Networking;
using Richter.Vulkan;

namespace Richter;

/// <summary>
/// The map of the star system: looking straight down on its reference plane, centred on one body.
/// Tab and Shift+Tab change the body in focus, and the scroll wheel zooms.
/// </summary>
/// <remarks>
/// Positions are taken relative to the focused body while they're still <see cref="Double"/>s,
/// and only then turned into <see cref="Single"/> pixels. A Single can only place things to about
/// 16 km at Earth's distance from the Sun, but a position relative to the view only has to be
/// right to the nearest pixel.
/// </remarks>
internal class SystemMap
{
    private const Int32 OrbitPoints = 256;
    private const Double ZoomStep = 1.25;
    private const Double SmallestMetresPerPixel = 1;
    private const Double LargestMetresPerPixel = 1e12;
    private const Single SmallestBodyRadius = 3;
    private const Single NetworkRadius = 2;
    private const Single LabelGap = 5;

    private static readonly float4 StarColour = new float4(1.0f, 0.85f, 0.4f, 1.0f);
    private static readonly float4 BodyColour = new float4(0.75f, 0.8f, 0.9f, 1.0f);
    private static readonly float4 OrbitColour = new float4(0.35f, 0.4f, 0.5f, 1.0f);
    private static readonly float4 NetworkColour = new float4(0.3f, 0.9f, 1.0f, 1.0f);
    private static readonly float4 SatelliteOrbitColour = new float4(0.2f, 0.45f, 0.5f, 1.0f);
    private static readonly float4 LinkUpColour = new float4(0.4f, 1.0f, 0.5f, 0.9f);
    private static readonly float4 LinkDownColour = new float4(0.7f, 0.25f, 0.25f, 0.5f);
    private static readonly float4 BodyLabelColour = new float4(0.85f, 0.87f, 0.92f, 1.0f);
    private static readonly float4 NetworkLabelColour = new float4(0.55f, 0.9f, 1.0f, 1.0f);

    private readonly World _world;
    private readonly CelestialBody[] _bodies;
    private readonly float2[] _orbitPixels = new float2[OrbitPoints];
    private readonly float2[] _linkPixels = new float2[2];
    private Int32 _focus;
    private Double _metresPerPixel = 1e9;
    private Boolean _fitPending = true;

    public SystemMap(World world)
    {
        _world = world;
        _bodies = world.StarSystem.Bodies.ToArray();
    }

    public String FocusName => _bodies[_focus].Name;

    /// <summary> Handles a key event from GLFW. Holding Tab keeps stepping. </summary>
    public void OnKey(GlfwKey key, GlfwKeyAction action, GlfwModifier modifiers)
    {
        if (key == GlfwKey.Tab && action != GlfwKeyAction.Release)
        {
            Int32 step = (modifiers & GlfwModifier.Shift) != 0 ? -1 : 1;
            _focus = (_focus + step + _bodies.Length) % _bodies.Length;
            _fitPending = true;
        }
    }

    /// <summary> Zooms in for a positive number of scroll notches and out for a negative one. </summary>
    public void OnScroll(Double notches)
    {
        _metresPerPixel = Math.Clamp(_metresPerPixel * Math.Pow(ZoomStep, -notches), SmallestMetresPerPixel, LargestMetresPerPixel);
    }

    /// <summary> Fills <paramref name="scene"/> with the map as it is at the world's time. </summary>
    public void Draw(MapScene scene, int2 framebufferSize)
    {
        if (_fitPending)
        {
            FitFocus(framebufferSize);
            _fitPending = false;
        }

        DateTimeOffset time = _world.Time;
        StarSystem starSystem = _world.StarSystem;
        Vector3D centre = starSystem.PositionOf(_bodies[_focus].Id, time);
        scene.Clear();

        foreach (CelestialBody body in _bodies.Where(body => body.Orbit is not null))
        {
            AddPath(scene, starSystem.OrbitPathOf(body.Id, time, OrbitPoints), centre, OrbitColour);
        }

        foreach (OrbitalLocation satellite in _world.Networks.Networks.Select(network => network.Location).OfType<OrbitalLocation>())
        {
            AddPath(scene, starSystem.OrbitPathOf(satellite.Orbit, time, OrbitPoints), centre, SatelliteOrbitColour);
        }

        // A link is a straight line between its networks, green while it's up.
        foreach (Link link in _world.Networks.Links)
        {
            _linkPixels[0] = ToPixels(starSystem.PositionOf(_world.Networks.GetNetwork(link.NetworkA).Location, time), centre);
            _linkPixels[1] = ToPixels(starSystem.PositionOf(_world.Networks.GetNetwork(link.NetworkB).Location, time), centre);
            scene.AddLine(_linkPixels, _world.Networks.DelayOf(link.Id, time) is null ? LinkDownColour : LinkUpColour);
        }

        // Labels that would overlap one added earlier are dropped, so the focused body goes first
        // and keeps its label, then the others in the data file's order, then the networks.
        CelestialBodyId focus = _bodies[_focus].Id;
        foreach (CelestialBody body in _bodies.OrderBy(body => body.Id == focus ? 0 : 1))
        {
            float2 position = ToPixels(starSystem.PositionOf(body.Id, time), centre);
            Single radius = Math.Max((Single)(body.Radius / _metresPerPixel), SmallestBodyRadius);
            scene.AddDisc(position, radius, body.Orbit is null ? StarColour : BodyColour);
            scene.AddLabel(position + new float2(radius + LabelGap, 0), body.Name, BodyLabelColour);
        }

        foreach (Network network in _world.Networks.Networks)
        {
            float2 position = ToPixels(starSystem.PositionOf(network.Location, time), centre);
            scene.AddDisc(position, NetworkRadius, NetworkColour);
            scene.AddLabel(position + new float2(NetworkRadius + LabelGap, 0), network.Name, NetworkLabelColour);
        }
    }

    private void AddPath(MapScene scene, IReadOnlyList<Vector3D> path, Vector3D centre, float4 colour)
    {
        for (Int32 i = 0; i < path.Count; i++)
        {
            _orbitPixels[i] = ToPixels(path[i], centre);
        }

        scene.AddLine(_orbitPixels, colour);
    }

    private float2 ToPixels(Vector3D position, Vector3D centre)
    {
        Vector3D relative = position - centre;

        return new float2((Single)(relative.X / _metresPerPixel), (Single)(relative.Y / _metresPerPixel));
    }

    /// <summary>
    /// Zooms so that everything orbiting the focused body fits in the view, or, with nothing
    /// orbiting it, so that the body is a tenth of the view across.
    /// </summary>
    private void FitFocus(int2 framebufferSize)
    {
        CelestialBody focus = _bodies[_focus];
        Double furthest = _bodies
            .Select(body => body.Orbit)
            .OfType<Orbit>()
            .Where(orbit => orbit.Parent == focus.Id)
            .Select(orbit => orbit.SemiMajorAxis * (1 + orbit.Eccentricity))
            .DefaultIfEmpty(focus.Radius * 10)
            .Max();
        Double halfView = Math.Min(framebufferSize.X, framebufferSize.Y) / 2.0;

        _metresPerPixel = Math.Clamp(furthest * 1.1 / halfView, SmallestMetresPerPixel, LargestMetresPerPixel);
    }
}
