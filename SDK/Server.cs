using System.Reflection;
using System.Text;
using System.Text.Json;
using SDK.InstanceTools;
using WatsonWebsocket;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SDK;

public static class Server {

	public static WatsonWsServer WebServer { get; } = new("127.0.0.1", 9006);
	public static List<Client> Clients { get; } = new();

	private static string _lastConnectedIpPort = string.Empty;

	public static void Start() {
		WebServer.ClientConnected += ClientConnected;
		WebServer.ClientDisconnected += ClientDisconnected;
		WebServer.MessageReceived += MessageReceived;
		
		WebServer.Start();
	}

	private static void ClientConnected(object? sender, ConnectionEventArgs e) => _lastConnectedIpPort = e.Client.IpPort;

	private static void ClientDisconnected(object? sender, DisconnectionEventArgs e) {
		Client client = Clients.First(c => c.IpPort == e.Client.IpPort);
		Clients.Remove(client);
		
		ConsoleUtilities.Info($"Client disconnected from {client.Platform}: {e.Client.IpPort}");
	}

	public struct Command {

		public enum CommandType {

			// Server to client
			ShowNotification,
			CreateAudio,
			PlayAudio,
			PauseAudio,
			StopAudio,
			DestroyAudio,
			PongGetOpenInstances,
			PongGetUIElements,
			PongUpdateUIElement,
			PongGetOpenableProtocols,
			PongOpenProtocol,
			
			// Client to server
			Connect,
			PingGetUIElements,
			PingUIEvent,
			PingGetOpenableProtocols,
			PingOpenProtocol,
			PingCloseProtocol,

		}

		public CommandType command { get; set;  }
		public Dictionary<string, object> arguments { get; set; }

	}
	
	private static void MessageReceived(object? sender, MessageReceivedEventArgs e) {
		// Parse JSON
		string message = Encoding.UTF8.GetString(e.Data);
		JsonDocument json = JsonDocument.Parse(message);
		
		// Make command
		string commandString = json.RootElement.GetProperty("command").GetString()!;
		var commandType = Enum.Parse<Command.CommandType>(commandString);
		
		string argumentsJson = json.RootElement.GetProperty("arguments").GetRawText();
		var argumentsDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson)!;
		
		Command command = new() {
			command = commandType,
			arguments = argumentsDictionary
		};
		
		// Debug
		ConsoleUtilities.Communication($"Command received: {commandString}");
		
		string dictionaryString = "{ ";
		
		foreach (var keyValuePair in argumentsDictionary)
			dictionaryString += keyValuePair.Key + ": " + keyValuePair.Value + ", ";  
		
		dictionaryString = dictionaryString.TrimEnd(',', ' ') + " }";  
		ConsoleUtilities.Debug(dictionaryString);
		
		// Handle command
		HandleCommand(command, e.Client);
	}
	
	public static void SendCommand(Client client, Command command) {
		var clientMetadata = client.GetMetadata();
		
		string message = JsonSerializer.Serialize(new {
			command = command.command.ToString(),
			arguments = command.arguments
		});

		WebServer.SendAsync(clientMetadata.Guid, message);
		
		// Debug
		ConsoleUtilities.Communication($"Command sent to {client.Platform} ({client.IpPort}): {command.command}");
	}

	
	#region Handle Commands
	
	private static void HandleCommand(Command command, ClientMetadata clientMetadata) {
		// Use reflection to get method
		var method = typeof(Server).GetMethod(
			$"Handle{command.command}", 
			BindingFlags.NonPublic | BindingFlags.Static
		);

		if (method == null) {
			ConsoleUtilities.Error($"Implementation for command {command.command} not found");
			return;
		}

		// Invoke method
		method.Invoke(null, [command, clientMetadata]);
	}
	
	private static void HandleConnect(Command command, ClientMetadata clientMetadata) {
		var platform = command.arguments["Platform"].ToString()!;
		var platformType = Enum.Parse<Client.PlatformType>(platform);

		var client = new Client(
			platformType,
			_lastConnectedIpPort
		);
				
		Clients.Add(client);

		ConsoleUtilities.Info($"Client connected from {platform}: {client.IpPort}");
				
		// Load instances
		client.PongGetOpenInstances();
	}
	
	private static void HandlePingGetUIElements(Command command, ClientMetadata clientMetadata) {
		var instanceID = command.arguments["InstanceID"].ToString()!;
		var instance = Instance.FromInstanceID(instanceID);
		
		var client = Client.FromMetadata(clientMetadata)!;
		client.SetOpenInstance(instance);
		client.PongGetUIElements(instance);
	}

	private static void HandlePingUIEvent(Command command, ClientMetadata clientMetadata) {
		var elementID = command.arguments["ElementID"].ToString()!;
		var eventName = command.arguments["EventName"].ToString()!;
		var element = UIElement.FromElementID(elementID);
		
		var argumentsJson = command.arguments["Arguments"].ToString()!;
		var arguments = argumentsJson.DeserializeArguments();
		
		element.InvokeUIEvent(eventName, arguments);
	}

	private static void HandlePingGetOpenableProtocols(Command command, ClientMetadata clientMetadata) {
		var client = Client.FromMetadata(clientMetadata)!;
		client.PongGetOpenableProtocols();
	}
	
	private static void HandlePingOpenProtocol(Command command, ClientMetadata clientMetadata) {
		var hashCode = command.arguments["TypeName"].ToString()!;
		var type = Core.InstanceTypes.First(e => e.Name == hashCode);
		var instance = Core.Open(type);

		var client = Client.FromMetadata(clientMetadata);
		client?.PongOpenProtocol(instance.InstanceID);
	}
	
	private static void HandlePingCloseProtocol(Command command, ClientMetadata clientMetadata) {
		var instanceID = command.arguments["InstanceID"].ToString()!;
		Core.Close(Instance.FromInstanceID(instanceID));
	}

	#endregion
	
}