using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NinOS.Domain
{
    public static class promo_items_snapshot_store
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
        };

        public static string? serialize(List<promo_item_snapshot>? items)
        {
            if (items == null || items.Count == 0) return null;
            try { return JsonSerializer.Serialize(items, _options); }
            catch (Exception) { return null; }
        }

        public static List<promo_item_snapshot>? deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<List<promo_item_snapshot>>(json, _options); }
            catch (Exception) { return null; }
        }
    }
}
