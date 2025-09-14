using SDK;
using SDK.InstanceTools;

namespace Plugins;

[NotifyOnOpen(false)]
public class CaptureProtocol : ProtocolInstance {
	
	[InstanceStorage] public int instanceStorage;
	
	public override void Open() {
		if (instanceStorage == 0)
			instanceStorage = GetHashCode();
		
		var input = new Input(placeholder: "Your thoughts go here...");
		var button = new Button("Capture");
		
		InstanceUI.Add(input);
		InstanceUI.Add(button);

		button.OnClick += () => {
			var systemInstance = Core.GetSystemInstance<CaptureSystem>();
			systemInstance.Capture(input.Text);
			
			Core.Close(this);
		};
	}

}