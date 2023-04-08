using System;
using System.Text.Json;

namespace SS14.ServerHub.Utility;

public static class JsonHelper
{
    /// <summary>
    /// Validate that the input is well-formed JSON.
    /// </summary>
    /// <exception cref="JsonException">Thrown if the input is not valid JSON.</exception>
    public static void CheckJsonValid(ReadOnlySpan<byte> data)
    {
        var reader = new Utf8JsonReader(data);
        reader.Read();
        reader.Skip();
    }
}