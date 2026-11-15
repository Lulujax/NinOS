using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NinOS.Domain.ViewModels;

namespace NinOS.Domain
{
    public static class note_snapshot_store
    {
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
        };

        public static string? serialize(note_print_dto? dto)
        {
            if (dto == null) return null;
            try
            {
                return JsonSerializer.Serialize(dto, _options);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static note_print_dto? deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<note_print_dto>(json, _options);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
