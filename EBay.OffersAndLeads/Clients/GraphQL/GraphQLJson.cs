// Copyright 2026 eBay Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Xml;

namespace EBay.OffersAndLeads.Clients.GraphQL;

/// <summary>Thrown when a GraphQL response does not match the shape the operation expects.</summary>
internal sealed class GraphQLResultException : Exception
{
    public GraphQLResultException(string message) : base(message) { }
}

/// <summary>JSON read/write helpers used by the generated GraphQL operation types.</summary>
internal static class GraphQLJson
{
    internal static JsonElement Required(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null)
            return value;
        throw new GraphQLResultException($"Non-null field '{name}' is null or missing.");
    }

    internal static JsonElement? Optional(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) ? Nullable(value) : null;

    internal static JsonElement? Nullable(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined ? null : value;

    internal static T? MapRef<T>(JsonElement? value, Func<JsonElement, T> map) where T : class =>
        value.HasValue ? map(value.Value) : null;

    internal static T? MapValue<T>(JsonElement? value, Func<JsonElement, T> map) where T : struct =>
        value.HasValue ? map(value.Value) : null;

    internal static IReadOnlyList<T> ReadList<T>(JsonElement value, Func<JsonElement, T> map)
    {
        if (value.ValueKind != JsonValueKind.Array)
            throw new GraphQLResultException($"Expected a JSON array but found {value.ValueKind}.");
        var list = new List<T>(value.GetArrayLength());
        foreach (var item in value.EnumerateArray())
            list.Add(map(item));
        return list;
    }

    internal static string ReadTypename(JsonElement obj) => ReadString(Required(obj, "__typename"));

    /// <summary>For object-typed fields: rejects a response whose __typename (when present) is a different type.</summary>
    internal static void EnsureTypename(JsonElement obj, string expected, string interfaceName)
    {
        if (obj.TryGetProperty("__typename", out var value)
            && value.ValueKind == JsonValueKind.String
            && value.GetString() is { } actual
            && actual != expected)
        {
            throw new GraphQLResultException($"Unknown __typename '{actual}' for {interfaceName}.");
        }
    }

    internal static string ReadString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new GraphQLResultException($"Expected a JSON string but found {value.ValueKind}.");

    internal static int ReadInt32(JsonElement value) => value.GetInt32();

    internal static long ReadInt64(JsonElement value) => value.GetInt64();

    internal static double ReadDouble(JsonElement value) => value.GetDouble();

    internal static bool ReadBoolean(JsonElement value) => value.GetBoolean();

    internal static decimal ReadDecimal(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? decimal.Parse(value.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture)
            : value.GetDecimal();

    internal static DateTimeOffset ReadDateTimeOffset(JsonElement value) =>
        DateTimeOffset.Parse(ReadString(value), CultureInfo.InvariantCulture, DateTimeStyles.None);

    internal static TimeSpan ReadTimeSpan(JsonElement value) => XmlConvert.ToTimeSpan(ReadString(value));

    internal static void WriteString(Utf8JsonWriter writer, string value) => writer.WriteStringValue(value);

    internal static void WriteInt32(Utf8JsonWriter writer, int value) => writer.WriteNumberValue(value);

    internal static void WriteInt64(Utf8JsonWriter writer, long value) => writer.WriteNumberValue(value);

    internal static void WriteDouble(Utf8JsonWriter writer, double value) => writer.WriteNumberValue(value);

    internal static void WriteBoolean(Utf8JsonWriter writer, bool value) => writer.WriteBooleanValue(value);

    internal static void WriteDecimal(Utf8JsonWriter writer, decimal value) => writer.WriteNumberValue(value);

    // UTC is written with a "Z" suffix, other offsets as +hh:mm.
    internal static void WriteDateTimeOffset(Utf8JsonWriter writer, DateTimeOffset value) =>
        writer.WriteStringValue(value.Offset == TimeSpan.Zero
            ? value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
            : value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture));

    internal static void WriteTimeSpan(Utf8JsonWriter writer, TimeSpan value) =>
        writer.WriteStringValue(XmlConvert.ToString(value));

    internal static bool SequenceEqual(IEnumerable? left, IEnumerable? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        var l = left.GetEnumerator();
        var r = right.GetEnumerator();
        while (true)
        {
            var hasLeft = l.MoveNext();
            if (hasLeft != r.MoveNext()) return false;
            if (!hasLeft) return true;
            if (!Equals(l.Current, r.Current)) return false;
        }
    }

    internal static int SequenceHash(IEnumerable? values)
    {
        if (values is null) return 0;
        unchecked
        {
            var hash = 5;
            foreach (var value in values)
                hash ^= 397 * (value?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
