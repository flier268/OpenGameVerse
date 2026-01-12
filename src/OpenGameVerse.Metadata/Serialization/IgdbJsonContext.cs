using System.Text.Json;
using System.Text.Json.Serialization;
using OpenGameVerse.Metadata.Models;

namespace OpenGameVerse.Metadata.Serialization;

/// <summary>
/// JSON source generation context for IGDB API models.
/// Required for Native AOT compatibility.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization
)]
[JsonSerializable(typeof(IgdbGame))]
[JsonSerializable(typeof(IgdbGame[]))]
[JsonSerializable(typeof(IgdbCover))]
[JsonSerializable(typeof(IgdbCover[]))]
[JsonSerializable(typeof(GameMetadata))]
[JsonSerializable(typeof(List<GameMetadata>))]
public partial class IgdbJsonContext : JsonSerializerContext
{
}
