using System.Diagnostics;
using System.Globalization;
using Brutal.GlfwApi;
using Brutal.Numerics;
using Richter.Domain;
using Richter.Domain.Astronomy;
using Richter.Json;
using Richter.Vulkan;

namespace Richter;

/// <summary> Entry point and bootstrapper for the Richter application. </summary>
public class Program
{
    // Content files, relative to the executable, so that errors shown to players don't reveal
    // where the game is installed.
    private const String ScenarioPath = "Data/scenario.json";
    private const String StarSystemPath = "Data/solar-system.json";
    private const String FontPath = "Data/Fonts/LiberationSans-Regular.ttf";

    /// <summary>
    /// Loads the game's content, starts GLFW, runs the game until its window closes, then shuts
    /// GLFW down. Returns 1 without opening a window if the content can't be read.
    /// </summary>
    public static async Task<Int32> Main()
    {
        // Everything is loaded before anything is checked, so that every broken file is reported
        // at once. Only the scenario waits: its networks are placed in the star system.
        StarSystem? starSystem = await LoadAsync(StarSystemPath, StarSystemFile.ReadAsync, CancellationToken.None);
        World? world = starSystem is null
            ? null
            : await LoadAsync(ScenarioPath, (stream, token) => ScenarioFile.ReadAsync(stream, starSystem, token), CancellationToken.None);
        IReadOnlyDictionary<String, Byte[]>? shaders = await LoadShadersAsync(CancellationToken.None);
        MapFont? font = await LoadAsync(FontPath, MapFont.ReadAsync, CancellationToken.None);

        if (world is null || shaders is null || font is null)
        {
            return 1;
        }

        GlfwSurfaceSource.ShareVulkanLoader();

        if (!Glfw.Init())
        {
            GlfwErrorCode error = Glfw.GetError(out String? description);
            throw new InvalidOperationException($"Failed to initialise GLFW: {error} ({description}).");
        }

        try
        {
            Run(world, shaders, font);
        }
        finally
        {
            Glfw.Terminate();
        }

        return 0;
    }

    /// <summary>
    /// Reads a content file, given relative to the executable, or reports why it can't and
    /// returns null.
    /// </summary>
    private static async Task<T?> LoadAsync<T>(String path, Func<Stream, CancellationToken, Task<T>> readAsync, CancellationToken cancellationToken)
        where T : class
    {
        T? content = null;

        // Players and modders edit these files, so say what's wrong instead of crashing with a
        // stack trace.
        try
        {
            await using FileStream file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, path));
            content = await readAsync(file, cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            await Console.Error.WriteLineAsync($"Richter can't start: {path} is missing.");
        }
        catch (InvalidDataException exception)
        {
            await Console.Error.WriteLineAsync($"Richter can't start: {path} isn't valid. {exception.Message}");
        }

        return content;
    }

    /// <summary>
    /// Reads and compiles every shader the renderer draws with, by file name. Reports each one
    /// that can't be read or compiled, and returns null if any couldn't.
    /// </summary>
    private static async Task<IReadOnlyDictionary<String, Byte[]>?> LoadShadersAsync(CancellationToken cancellationToken)
    {
        Dictionary<String, Byte[]> shaders = new Dictionary<String, Byte[]>();

        foreach (String fileName in WindowRenderer.ShaderFiles)
        {
            Byte[]? spirv = await LoadAsync(
                $"Data/Shaders/{fileName}",
                (stream, token) => ShaderCompiler.CompileAsync(stream, fileName, token),
                cancellationToken);

            if (spirv is not null)
            {
                shaders.Add(fileName, spirv);
            }
        }

        return shaders.Count == WindowRenderer.ShaderFiles.Count ? shaders : null;
    }

    // Separate from Main so the window and renderer are disposed before GLFW is terminated.
    private static void Run(World world, IReadOnlyDictionary<String, Byte[]> shaders, MapFont font)
    {
        TimeControls timeControls = new TimeControls(world);
        SystemMap map = new SystemMap(world);
        MapScene scene = new MapScene();
        Glfw.WindowHint(GlfwWindowHint.ClientApi, Glfw.Constants.GLFW_NO_API);
        using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo
        {
            Title = TitleFor(world.Time, timeControls, map),
            Size = new int2(1280, 720),
        });
        window.OnKey += (_, key, _, action, modifiers) =>
        {
            timeControls.OnKey(key, action);
            map.OnKey(key, action, modifiers);
        };
        window.OnScroll += (_, offset) => map.OnScroll(offset.Y);
        using WindowRenderer renderer = new WindowRenderer(new GlfwSurfaceSource(window), shaders, font);
        Int64 lastFrame = Stopwatch.GetTimestamp();

        while (!window.ShouldClose)
        {
            // Game time runs on while the window is minimised, as a real-time simulation's clock
            // should.
            Int64 thisFrame = Stopwatch.GetTimestamp();
            world.Advance(Stopwatch.GetElapsedTime(lastFrame, thisFrame));
            lastFrame = thisFrame;

            String title = TitleFor(world.Time, timeControls, map);
            if (title != window.Title)
            {
                window.Title = title;
            }

            int2 framebufferSize = window.FramebufferSize;
            if (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                // Minimised: there's nothing to present to, so sleep until an event arrives
                // instead of spinning.
                Glfw.WaitEvents();
            }
            else
            {
                Glfw.PollEvents();
                map.Draw(scene, framebufferSize);
                renderer.DrawFrame(scene);
            }
        }
    }

    private static String TitleFor(DateTimeOffset time, TimeControls timeControls, SystemMap map)
    {
        return $"Richter - {map.FocusName} - {time.UtcDateTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)} UTC - {timeControls.Status}";
    }
}
