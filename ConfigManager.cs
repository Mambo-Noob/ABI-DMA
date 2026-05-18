using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.Config
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  CONFIG MANAGER
    //  Central save/load for every settings object in the overlay.
    //  Usage:
    //    ConfigManager.Load(ref mySettings, "settings.json");
    //    ConfigManager.Save(mySettings, "settings.json");
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static class ConfigManager
    {
        public static string ConfigDirectory { get; private set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "xmLauncher", "Data", "ABI");

        static ConfigManager()
        {
            Directory.CreateDirectory(ConfigDirectory);
        }

        // //// Serialization //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting            = Formatting.Indented,
            NullValueHandling     = NullValueHandling.Ignore,
            DefaultValueHandling  = DefaultValueHandling.Include,
            TypeNameHandling      = TypeNameHandling.None,
            Converters            = { new Vector2Converter(), new Vector4Converter() }
        };

        public static void Save<T>(T obj, string filename)
        {
            try
            {
                var path = Path.Combine(ConfigDirectory, filename);
                var json = JsonConvert.SerializeObject(obj, _jsonSettings);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigManager] Save failed ({filename}): {ex.Message}");
            }
        }

        public static T Load<T>(string filename, T? fallback = default) where T : new()
        {
            try
            {
                var path = Path.Combine(ConfigDirectory, filename);
                if (!File.Exists(path)) return fallback ?? new T();
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json, _jsonSettings) ?? fallback ?? new T();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigManager] Load failed ({filename}): {ex.Message}");
                return fallback ?? new T();
            }
        }

        public static bool Exists(string filename)
            => File.Exists(Path.Combine(ConfigDirectory, filename));

        public static string[] ListConfigs(string prefix = "")
            => Directory.GetFiles(ConfigDirectory, $"{prefix}*.json");

        public static void Delete(string filename)
        {
            var path = Path.Combine(ConfigDirectory, filename);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  JSON CONVERTERS for System.Numerics types
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Vector2Converter : JsonConverter<System.Numerics.Vector2>
    {
        public override void WriteJson(JsonWriter w, System.Numerics.Vector2 v, JsonSerializer s)
        {
            w.WriteStartObject();
            w.WritePropertyName("x"); w.WriteValue(v.X);
            w.WritePropertyName("y"); w.WriteValue(v.Y);
            w.WriteEndObject();
        }
        public override System.Numerics.Vector2 ReadJson(JsonReader r, Type t, System.Numerics.Vector2 e, bool hasE, JsonSerializer s)
        {
            var obj = Newtonsoft.Json.Linq.JObject.Load(r);
            return new System.Numerics.Vector2((float)(obj["x"] ?? 0), (float)(obj["y"] ?? 0));
        }
    }

    public class Vector4Converter : JsonConverter<System.Numerics.Vector4>
    {
        public override void WriteJson(JsonWriter w, System.Numerics.Vector4 v, JsonSerializer s)
        {
            w.WriteStartObject();
            w.WritePropertyName("r"); w.WriteValue(v.X);
            w.WritePropertyName("g"); w.WriteValue(v.Y);
            w.WritePropertyName("b"); w.WriteValue(v.Z);
            w.WritePropertyName("a"); w.WriteValue(v.W);
            w.WriteEndObject();
        }
        public override System.Numerics.Vector4 ReadJson(JsonReader r, Type t, System.Numerics.Vector4 e, bool hasE, JsonSerializer s)
        {
            var obj = Newtonsoft.Json.Linq.JObject.Load(r);
            return new System.Numerics.Vector4(
                (float)(obj["r"] ?? 0), (float)(obj["g"] ?? 0),
                (float)(obj["b"] ?? 0), (float)(obj["a"] ?? 1));
        }
    }
}