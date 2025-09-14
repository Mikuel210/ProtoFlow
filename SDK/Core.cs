using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using SDK.InstanceTools;
using Timer = System.Timers.Timer;

namespace SDK;

public static class Core {
	
	public static List<Type> InstanceTypes { get; private set; } = new();
	public static List<Instance> OpenInstances { get; } = new();

	// Events
	public static event Action<Type>? OnInstanceOpened;
	public static event Action<Type>? OnInstanceClosed;
	
	// Saving
	private static bool _saveInstancesQueued;
	private static readonly Timer _savingTimer = new(60_000);

	static Core() {
		_savingTimer.Elapsed += (_, _) => {
			_saveInstancesQueued = true;
			_savingTimer.Start();
		};
		
		_savingTimer.Start();
	}
	
	public static void LoadPlugins() {
		var relativePath = "../../../../Plugins/bin";
		var pluginsDirectory = Path.GetFullPath(Path.Join(Environment.CurrentDirectory, relativePath));
        
		var path = Directory
			.EnumerateFiles(pluginsDirectory, "Plugins.dll", SearchOption.AllDirectories)
			.FirstOrDefault();

		if (path == null) {
			ConsoleUtilities.Error("Plugins.dll not found");
			return;
		}
		
		var loadContext = new AssemblyLoadContext(path, isCollectible: true);
		var assembly = loadContext.LoadFromAssemblyPath(path);

		InstanceTypes = assembly.GetTypes().Where(e => typeof(Instance).IsAssignableFrom(e)).ToList();
		
		// Deserialize instances
		LoadInstances();
		OpenSystems();
	}

	private static void OpenSystems() {
		foreach (Type systemType in InstanceTypes.Where(e => GetInstanceCategory(e) == InstanceCategory.System))
			if (GetOpenInstance(systemType) == null) Open(systemType);
	}
	
	public static void UpdateInstances() {
		if (_saveInstancesQueued) {
			SaveInstances();
			_saveInstancesQueued = false;
		}
		
		foreach (Instance instance in new List<Instance>(OpenInstances))
			if (OpenInstances.Contains(instance)) instance.Loop();
	}

	private static void OnOpenInstancesUpdated() {
		Server.Clients.ForEach(e => e.PongGetOpenInstances());
		_saveInstancesQueued = true;
	}
	
	#region Instance serializing

	private const string OPEN_INSTANCES_KEY = "open_instances";
	
	private static void LoadInstances() {
		var data = Serializer.Load<dynamic>(OPEN_INSTANCES_KEY);
		if (data is null) return;
		
		foreach (dynamic instanceData in data) {
			try { DeserializeInstance(instanceData); }
			catch (Exception e) { ConsoleUtilities.Warning($"Failed to deserialize instance: {e}"); }
		}
	}

	private static void DeserializeInstance(dynamic instanceData) {
		// Deserialize instance
		var instance = (Instance)Activator.CreateInstance(InstanceTypes.First(e => e.Name == instanceData["TypeName"]))!;

		instance.DeserializeInstanceID(instanceData["InstanceID"]);
		instance.Title = instanceData["Title"].ToString()!;
		
		// Deserialize Instance Storage
		DeserializeInstanceStorage(instance, instanceData);

		// Deserialize UI elements
		if (GetSerializeUI(instance)) {
			foreach (dynamic elementData in instanceData["UI"]) {
				try {
					var element = DeserializeUIElement(elementData);
					instance.InstanceUI.Add(element);
				}
				catch (Exception e) { ConsoleUtilities.Warning($"Failed to deserialize UI element: {e}"); }
			}
		}
		
		// Setup instance
		instance.Deserialize();
		Open(instance);
	}

	private static void DeserializeInstanceStorage(Instance instance, dynamic instanceData) {
		var fields = instance.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		
		foreach (var field in fields) {
			var attribute = field.GetCustomAttribute<InstanceStorageAttribute>();
			if (attribute is null) continue;

			var dictionary = (Dictionary<string, object>)instanceData["InstanceStorage"];

			if (dictionary.TryGetValue(field.Name, out _)) {
				var json = JsonSerializer.Serialize(dictionary[field.Name]);
				var value = JsonSerializer.Deserialize(json, field.FieldType, Serializer.SerializationOptions);
				field.SetValue(instance, value);
			}
		}
	}

	private static UIElement DeserializeUIElement(dynamic elementData) {
		UIElement element;
		var type = Type.GetType(elementData["Type"]);
		
		try {
			// Try default constructor
			element = (UIElement)Activator.CreateInstance(type);
		}
		catch (MissingMethodException) {
			// Create element with default values for constructor
			element = (UIElement)Activator.CreateInstance(type, 
				BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding, 
				null, new[] { Type.Missing }, CultureInfo.CurrentCulture);
		}
						
		// Deserialize properties
		element.DeserializeElementID(elementData["ElementID"]);

		foreach (KeyValuePair<string, object> keyValuePair in elementData["Properties"]) {
			try { DeserializeProperty(element, keyValuePair); }
			catch (Exception e) { ConsoleUtilities.Warning($"Failed to deserialize UI element property: {e}"); }
		}

		return element;
	}

	private static void DeserializeProperty(UIElement element, KeyValuePair<string, object> keyValuePair) {
		string propertyName = keyValuePair.Key;
		object propertyValue = keyValuePair.Value;

		var property = element.GetType().GetProperty(propertyName)!;

		if (property.PropertyType.IsEnum)
			propertyValue = Enum.Parse(property.PropertyType, propertyValue.ToString()!);
			
		property.SetValue(element, propertyValue);
	}

	public static void SaveInstances() {
		List<dynamic> data = new();

		foreach (Instance instance in OpenInstances) {
			List<dynamic> ui = new();

			if (GetSerializeUI(instance)) {
				foreach (UIElement element in instance.InstanceUI) {
					ui.Add(new {
						Type = element.GetType().AssemblyQualifiedName,
						Properties = element.Properties,
						ElementID = element.ElementID,
					});
				}
			}
			
			data.Add(new {
				TypeName = instance.GetType().Name,
				InstanceID = instance.InstanceID,
				Title = instance.Title,
				UI = ui,
				InstanceStorage = GetInstanceStorage(instance)
			});
		}
		
		Serializer.Save(OPEN_INSTANCES_KEY, data);
		ConsoleUtilities.Info("Open instances saved");
	}

	private static Dictionary<string, object> GetInstanceStorage(Instance instance ) {
		Dictionary<string, object> output = new();
		
		var fields = instance.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		
		foreach (var field in fields) {
			var attribute = field.GetCustomAttribute<InstanceStorageAttribute>();
			if (attribute is null) continue;
			
			output.Add(field.Name, field.GetValue(instance)!);
		}

		return output;
	}
	
	#endregion
	
	#region Instance management
	
	public static void Open(Instance instance) {
		OpenInstances.Add(instance);
		
		// Initialize title
		instance.Title = GetInstanceName(instance);

		// Notify on open
		var instanceName = GetInstanceName(instance);
		
		if (instance is ProtocolInstance && GetNotifyOnOpen(instance)) {
			foreach (Client client in Server.Clients)
				client.ShowNotification($"New {instanceName}", $"A new {instanceName} instance has been opened.");
		}
		
		// Open instance
		instance.Open();
		OnInstanceOpened?.Invoke(instance.GetType());
		OnOpenInstancesUpdated();
		
		// Debug
		ConsoleUtilities.Info($"A new {instanceName} instance has been opened.");
	}
	
	public static Instance Open(Type instanceType) {
		// Error checking
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to open an instance from a non-instance type");

		if (instanceType.IsAssignableTo(typeof(SystemInstance)) && GetOpenInstance(instanceType) != null) {
			ConsoleUtilities.Warning($"Attempted to open a system instance twice: {GetInstanceName(instanceType)}");
			return GetSystemInstance(instanceType);
		}
		
		// Make instance
		var instance = (Instance)Activator.CreateInstance(instanceType)!;
		Open(instance);
		
		return instance;
	}
	public static T Open<T>() where T : Instance => (T)Open(typeof(T));

	public static Instance OpenFromName(string name) {
		var instanceType = InstanceTypes.FirstOrDefault(e => GetInstanceName(e) == name);

		if (instanceType == null)
			throw new ArgumentException("Attempted to open an instance from an invalid name");

		return Open(instanceType);
	}
	public static Instance OpenFromTypeName(string name) {
		var instanceType = InstanceTypes.FirstOrDefault(e => e.Name == name);

		if (instanceType == null)
			throw new ArgumentException("Attempted to open an instance from an invalid name");

		return Open(instanceType);
	}
	
	public static List<Instance> GetOpenInstances(Type instanceType) =>
		OpenInstances.Where(e => e.GetType() == instanceType).ToList();
	public static List<Instance> GetOpenInstances<T>() where T : Instance => GetOpenInstances(typeof(T));

	public static Instance? GetOpenInstance(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get an open instance from a non-instance type");
		
		return GetOpenInstances(instanceType).FirstOrDefault();	
	}
	public static T? GetOpenInstance<T>() where T : Instance => (T?)GetOpenInstance(typeof(T));

	public static Instance GetSystemInstance(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(SystemInstance)))
			throw new ArgumentException("Attempted to get a system instance from a non-system type");

		var instance = GetOpenInstance(instanceType);
		if (instance == null) throw new InvalidOperationException("System is not initialized yet");
		
		return instance;
	}
	public static T GetSystemInstance<T>() where T : SystemInstance => (T)GetSystemInstance(typeof(T));
	
	public static void Close(Instance instance) {
		if (instance is not ProtocolInstance protocol) {
			ConsoleUtilities.Warning($"Attempted to close a system instance: {GetInstanceName(instance)}");
			return;
		}
		
		// Stop instance audio
		protocol.InstanceAudio.Audios.ForEach(e => e.Stop());
		
		// Close protocol
		protocol.Close();
		OpenInstances.Remove(instance);
		OnInstanceClosed?.Invoke(protocol.GetType());
		OnOpenInstancesUpdated();
	}

	#endregion
	
	#region Instance metadata
	
	public static string GetInstanceName(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get the instance name from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<InstanceNameAttribute>();

		if (attribute != null)
			return attribute.Name;
		
		// Convert PascalCase to natural language
		string output = string.Empty;

		for (int i = 0; i < instanceType.Name.Length; i++) {
			var character = instanceType.Name[i];

			if (char.IsUpper(character) && i != 0) output += " ";
			output += character;
		}

		return output;
	}
	public static string GetInstanceName<T>() where T : Instance => GetInstanceName(typeof(T));
	public static string GetInstanceName(Instance instance) => GetInstanceName(instance.GetType());
	
	public static string GetInstanceDescription(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get the instance description from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<InstanceDescriptionAttribute>();

		if (attribute != null)
			return attribute.Description;
		
		return "No description provided";
	}
	public static string GetInstanceDescription<T>() where T : Instance => GetInstanceDescription(typeof(T));
	public static string GetInstanceDescription(Instance instance) => GetInstanceDescription(instance.GetType());

	public static bool CanClientOpen(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get instance metadata from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<CanClientOpenAttribute>();

		if (attribute != null)
			return attribute.CanClientOpen;
		
		return true;
	}
	public static bool CanClientOpen<T>() => CanClientOpen(typeof(T));
	public static bool CanClientOpen(Instance instance) => CanClientOpen(instance.GetType());
	
	public static bool CanClientClose(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get instance metadata from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<CanClientCloseAttribute>();

		if (attribute != null)
			return attribute.CanClientClose;
		
		return true;
	}
	public static bool CanClientClose<T>() => CanClientClose(typeof(T));
	public static bool CanClientClose(Instance instance) => CanClientClose(instance.GetType());

	public static bool GetNotifyOnOpen(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get instance metadata from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<NotifyOnOpenAttribute>();

		if (attribute != null)
			return attribute.NotifyOnOpen;
		
		return true;
	}
	public static bool GetNotifyOnOpen<T>() => GetNotifyOnOpen(typeof(T));
	public static bool GetNotifyOnOpen(Instance instance) => GetNotifyOnOpen(instance.GetType());
	
	public static bool GetShowOnClient(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get instance metadata from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<ShowOnClientAttribute>();

		if (attribute != null)
			return attribute.ShowOnClient;
		
		return true;
	}
	public static bool GetShowOnClient<T>() => GetShowOnClient(typeof(T));
	public static bool GetShowOnClient(Instance instance) => GetShowOnClient(instance.GetType());
	
	public static bool GetSerializeUI(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get instance metadata from a non-instance type");

		var attribute = instanceType.GetCustomAttribute<SerializeUIAttribute>();

		if (attribute != null)
			return attribute.SerializeUI;
		
		return false;
	}
	public static bool GetSerializeUI<T>() => GetSerializeUI(typeof(T));
	public static bool GetSerializeUI(Instance instance) => GetSerializeUI(instance.GetType());
	

	public enum InstanceCategory {

		Protocol,
		System

	}
	public static InstanceCategory GetInstanceCategory(Type instanceType) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to get the instance category from a non-instance type");
		
		bool isProtocol = instanceType.IsAssignableTo(typeof(ProtocolInstance));
		return isProtocol ? InstanceCategory.Protocol : InstanceCategory.System;
	}
	public static InstanceCategory GetInstanceCategory<T>() => GetInstanceCategory(typeof(T));
	public static InstanceCategory GetInstanceCategory(Instance instance) => GetInstanceCategory(instance.GetType());

	#endregion
}