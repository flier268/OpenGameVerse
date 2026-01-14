using System.Text.Json;
using System.Text.Json.Serialization;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Serialization;

/// <summary>
/// AOT-compatible JSON serialization context
/// All types must be explicitly declared for Native AOT compilation
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization
)]
[JsonSerializable(typeof(Game))]
[JsonSerializable(typeof(Library))]
[JsonSerializable(typeof(Platform))]
[JsonSerializable(typeof(GameInstallation))]
[JsonSerializable(typeof(List<Game>))]
[JsonSerializable(typeof(List<Library>))]
[JsonSerializable(typeof(List<Platform>))]
[JsonSerializable(typeof(List<GameInstallation>))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(Dictionary<string, string>))] // For VDF/Epic manifests
[JsonSerializable(typeof(Dictionary<string, object>))] // For nested JSON
public partial class OpenGameVerseJsonContext : JsonSerializerContext
{
}
