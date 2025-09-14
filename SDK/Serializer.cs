using System.Text.Json;

namespace SDK;

public static class Serializer {

	private static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
	private static string DataFolder => Path.Combine(AppData, "ProtoFlow");
	public static JsonSerializerOptions SerializationOptions { get; }

	static Serializer() {
		SerializationOptions = new() { IncludeFields = true, WriteIndented = true };
		Directory.CreateDirectory(DataFolder);
	}
	
	public static void Save(string key, object data) {
		string filePath = Path.Combine(DataFolder, $"{key}.json");
		string json = JsonSerializer.Serialize(data, SerializationOptions);
		File.WriteAllText(filePath, json);
	}

	public static T? Load<T>(string key) {
		string path = Path.Join(DataFolder, $"{key}.json");
		string json = File.ReadAllText(path);

		try {
			object? data = JsonSerializer.Deserialize<JsonElement>(json, SerializationOptions).GetValue();

			if (data is T output) return output;
			if (data is null) return default;
		}
		catch (JsonException) {
			return default;
		}
		
		throw new InvalidOperationException("Data couldn't be casted to the specified type");
	}
	
	#region JSON extensions
	
	public static Dictionary<string, object> DeserializeArguments(this string argumentsJson) {
		var argumentJsonElements = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
		var arguments = new Dictionary<string, object>();

		foreach (var keyValuePair in argumentJsonElements) {
			var key = keyValuePair.Key;
			var jsonElement = keyValuePair.Value;
			var value = jsonElement.GetValue();
			arguments.Add(key, value);
		}
		
		return arguments;
	}

	public static List<object?> DeserializeList(this string listJson) {
		var jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(listJson) ?? new();
		var list = jsonElements.Select(e => e.GetValue()).ToList();

		return list;
	}

	public static object? GetValue(this JsonElement element) {
		switch (element.ValueKind)
		{
			case JsonValueKind.String:
				return element.GetString();

			case JsonValueKind.Number:
				if (element.TryGetInt32(out var intValue))
					return intValue;

				return (float)element.GetDouble();

			case JsonValueKind.True:
			case JsonValueKind.False:
				return element.GetBoolean();

			case JsonValueKind.Object:
				return element.GetRawText().DeserializeArguments();

			case JsonValueKind.Array:
				return element.GetRawText().DeserializeList();

			case JsonValueKind.Null:
			default:
				return null;
		}
	}
	
	#endregion
	
}