using System.Reflection;
using System.Text.Json;
using SDK.InstanceTools;
using WatsonWebsocket;

namespace SDK;

public class Client(Client.PlatformType platform, string ipPort) {

	public enum PlatformType { Windows, MacOS, Linux }

	public PlatformType Platform { get; private set; } = platform;
	public string IpPort { get; private set; } = ipPort;
	public Instance? OpenInstance { get; private set; }

	public static Client? FromIpPort(string ipPort) => Server.Clients.FirstOrDefault(e => e.IpPort == ipPort);
	public static Client? FromMetadata(ClientMetadata metadata) => FromIpPort(metadata.IpPort);
	public static Client? FromPlatform(PlatformType platform) =>
		Server.Clients.FirstOrDefault(e => e.Platform == platform);

	public ClientMetadata GetMetadata() => Server.WebServer.ListClients().First(e => e.IpPort == IpPort);
	
	public void SetOpenInstance(Instance instance) => OpenInstance = instance;


	#region Send commands
	
	public void ShowNotification(string title, string body = "") {
		Server.SendCommand(this,
			new() {
				command = Server.Command.CommandType.ShowNotification,
				arguments = new() {
					{ "Title", title }, 
					{ "Body", body }
				}
			}
		);
	}

	public void CreateAudio(Audio audio) {
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.CreateAudio,
			arguments = new() {
				{ "AudioID", audio.AudioID },
				{ "URL", audio.URL },
			}
		});	
	}
	
	public void PlayAudio(Audio audio) {
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PlayAudio,
			arguments = new() {
				{ "AudioID", audio.AudioID }
			}
		});
	}
	
	public void PauseAudio(Audio audio) {
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PauseAudio,
			arguments = new() {
				{ "AudioID", audio.AudioID }
			}
		});
	}
	
	public void StopAudio(Audio audio) {
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.StopAudio,
			arguments = new() {
				{ "AudioID", audio.AudioID }
			}
		});
	}
	
	public void DestroyAudio(Audio audio) {
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.DestroyAudio,
			arguments = new() {
				{ "AudioID", audio.AudioID }
			}
		});
	}
	
	public void PongGetOpenInstances() {
		List<dynamic> instances = new();

		foreach (Instance instance in Core.OpenInstances.Where(Core.GetShowOnClient)) {
			instances.Add(new {
				InstanceID = instance.InstanceID,
				PluginType = Core.GetInstanceCategory(instance).ToString(),
				Name = Core.GetInstanceName(instance),
				Description = Core.GetInstanceDescription(instance),
				Title = instance.Title,
				CanClientClose = Core.CanClientClose(instance)
			});
		}
		
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PongGetOpenInstances,
			arguments = new() {
				{ "Instances", instances }
			}	
		});
	}

	public void PongGetUIElements(Instance instance) {
		List<dynamic> elements = new();

		foreach (UIElement element in instance.InstanceUI) {
			elements.Add(new {
				ElementID = element.ElementID,
				Type = element.GetType().Name,
				Properties = JsonSerializer.Serialize(element.Properties)
			});
		}
		
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PongGetUIElements,
			arguments = new() {
				{ "Elements", elements }
			}
		});
	}

	public void PongUpdateUIElement(UIElement element, string propertyName, object value) {
		Dictionary<string, object> properties = new() {
			{ propertyName, value }
		};
		
		string json = JsonSerializer.Serialize(properties);
		
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PongUpdateUIElement,
			arguments = new() {
				{ "ElementID", element.ElementID },
				{ "Property", json }
			}
		});
	}

	public void PongGetOpenableProtocols() {
		var protocols = Core.InstanceTypes.Where(e => Core.GetInstanceCategory(e) == Core.InstanceCategory.Protocol);
		var openableProtocols = protocols.Where(Core.CanClientOpen);
		
		List<dynamic> output = new();

		foreach (var protocol in openableProtocols) {
			output.Add(new {
				TypeName = protocol.Name,
				Name = Core.GetInstanceName(protocol)
			});
		}
		
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PongGetOpenableProtocols,
			arguments = new() {
				{ "Protocols", output }
			}
		});
	}

	public void PongOpenProtocol(string instanceId) {
		Server.SendCommand(this, new() {
			command = Server.Command.CommandType.PongOpenProtocol,
			arguments = new() {
				{ "InstanceID", instanceId }
			}
		});
	}

	#endregion
}