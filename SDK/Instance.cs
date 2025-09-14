using System.Collections.ObjectModel;
using System.ComponentModel;
using SDK.InstanceTools;

namespace SDK;

[Serializable] public abstract class Instance : INotifyPropertyChanged {

	public event PropertyChangedEventHandler? PropertyChanged;
	
	public string InstanceID { get; private set; }
	public string Title { get; set; }
	public InstanceUI InstanceUI { get; private set; } = new();
	public InstanceAudio InstanceAudio { get; private set; } = new();

	public Instance() {
		InstanceID = Guid.NewGuid().ToString();
		
		PropertyChanged += (sender, e) => {
			var property = GetType().GetProperty(e.PropertyName!);
			if (property == null) return;
			
			UpdateUI();
		};

		InstanceUI.CollectionChanged += (_, _) => UpdateUI();
	}
	public static Instance FromInstanceID(string instanceID) => 
		Core.OpenInstances.First(e => e.InstanceID == instanceID);
	public void DeserializeInstanceID(string instanceID) => InstanceID = instanceID;
	
	private void UpdateUI() {
		foreach (Client client in Server.Clients) {
			client.PongGetOpenInstances();
			
			if (client.OpenInstance != this) continue;
			client.PongGetUIElements(this);
		}
	}
	
	public virtual void Deserialize() { }
	public virtual void Open() { }
	public virtual void Loop() { }

}

[Serializable] public abstract class ProtocolInstance : Instance {
	
	public virtual void Close() { }

}

[Serializable] public abstract class SystemInstance : Instance;