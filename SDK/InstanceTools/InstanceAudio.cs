namespace SDK.InstanceTools;

public class InstanceAudio {

	public List<Audio> Audios { get; } = new();

	public Audio Register(Audio audio) {
		Audios.Add(audio);
		return audio;
	}

}

public class Audio {
	
	public List<Client> Clients { get; }
	public string URL { get; }
	public string AudioID { get; }

	public Audio(List<Client> clients, string url) {
		AudioID = Guid.NewGuid().ToString();
		
		Clients = clients;
		URL = url;
		Clients.ForEach(e => e.CreateAudio(this));
	}
	public Audio(Client client, string url) : this([client], url) { }
	
	~Audio() => Clients.ForEach(e => e.DestroyAudio(this));

	public void Play() => Clients.ForEach(e => e.PlayAudio(this));
	public void Pause() => Clients.ForEach(e => e.PauseAudio(this));
	public void Stop() => Clients.ForEach(e => e.StopAudio(this));

}