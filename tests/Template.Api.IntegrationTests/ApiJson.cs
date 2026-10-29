using System.Text.Json;
using System.Text.Json.Serialization;

namespace Template.Api.IntegrationTests;

internal static class ApiJson
{
    /// <summary>Matches the API's serializer settings (web defaults + string enums).</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
