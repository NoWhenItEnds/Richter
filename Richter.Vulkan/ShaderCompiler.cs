using System.Text;
using Brutal.ShaderCApi;

namespace Richter.Vulkan;

/// <summary> Compiles GLSL shaders to SPIR-V with ShaderC, as the game starts. </summary>
/// <remarks>
/// Shaders ship as GLSL in the game's data and compile at start-up, as BRUTAL's own Vulkan sample
/// does. Nothing needs installing, and a modder can change a shader without any tools.
/// </remarks>
public static class ShaderCompiler
{
    /// <summary>
    /// Reads GLSL from <paramref name="source"/> and compiles it for the stage that
    /// <paramref name="fileName"/>'s extension names, as glslc does: <c>.vert</c> or
    /// <c>.frag</c>. The compiler's messages refer to the file by that name. Throws
    /// <see cref="InvalidDataException"/> with the compiler's message if it doesn't compile.
    /// </summary>
    public static async Task<Byte[]> CompileAsync(Stream source, String fileName, CancellationToken cancellationToken)
    {
        using StreamReader reader = new StreamReader(source);
        String glsl = await reader.ReadToEndAsync(cancellationToken);

        return Compile(glsl, fileName);
    }

    private static unsafe Byte[] Compile(String glsl, String fileName)
    {
        ShaderKind shaderKind = Path.GetExtension(fileName) switch
        {
            ".vert" => ShaderKind.VertexShader,
            ".frag" => ShaderKind.FragmentShader,
            _ => throw new ArgumentException($"{fileName} isn't named as a vertex (.vert) or fragment (.frag) shader.", nameof(fileName)),
        };
        Byte[] spirv;

        using Compiler compiler = ShaderC.CreateCompiler();
        using CompileOptions options = ShaderC.CreateCompilerOptions();

        // ShaderC reads the names as C strings, so each array ends with a terminator that the views
        // passed to it leave out, as BRUTAL's own encoder does.
        Byte[] source = Encoding.UTF8.GetBytes(glsl + '\0');
        Byte[] name = Encoding.UTF8.GetBytes(fileName + '\0');

        try
        {
            using CompilationResult result = compiler.Compile(
                source.AsSpan(0, source.Length - 1),
                shaderKind,
                name.AsSpan(0, name.Length - 1),
                "main"u8,
                options);
            spirv = new ReadOnlySpan<Byte>(result.GetBytes(), (Int32)result.GetLength()).ToArray();
        }
        catch (ShaderException exception)
        {
            throw new InvalidDataException(exception.Message.Trim(), exception);
        }

        return spirv;
    }
}
