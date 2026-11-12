using Richter.Domain.Networking;
using Richter.Json.Documents;

namespace Richter.Json;

/// <summary> Reads the kinds of device there can be from every mod's <c>device-kinds.json</c>. </summary>
internal static class DeviceKindsFile
{
    internal const String FileName = "device-kinds.json";

    /// <summary> The kinds from every file, merged by id in load order. </summary>
    internal static IReadOnlyList<DeviceKind> Build(IReadOnlyList<FromFile<DeviceKindsDocument>> files)
    {
        return ContentMerge.ById(files, file => file.Kinds, kind => kind.Id, "device kinds")
            .Select(ToKind)
            .ToList();
    }

    private static DeviceKind ToKind(FromFile<DeviceKindDocument> kind)
    {
        DeviceKind result;

        try
        {
            result = new DeviceKind
            {
                Id = new DeviceKindId(kind.Value.Id),
                Name = kind.Value.Name,
                Lock = kind.Value.Components?.Lock is LockDocument lockDocument
                    ? new LockDefinition { StartsLocked = lockDocument.Locked ?? false }
                    : null,
            };
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"{kind.File}, device kind \"{kind.Value.Id}\": {ContentErrors.Describe(exception)}", exception);
        }

        return result;
    }
}
