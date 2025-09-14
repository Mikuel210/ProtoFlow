using SDK;
using SDK.InstanceTools;

namespace Plugins;

[CanClientOpen(false)]
[CanClientClose(false)]
[InstanceDescription("A protocol for getting ready for school")]
public class MorningProtocol : ProtocolInstance {

	private List<string> _messages = [
		"7:25 — Wash face",
		"7:30 — Shower",
		"7:40 — Dress up",
		"7:45 — Breakfast",
		"7:55 — Wash teeth, fill bottle",
		"8:00 — Have a good day!"
	];

	private int _index;
	private Heading _message;
	private Button _button;
	
	public override void Open() {
		_message = new(_messages[0], 4);
		InstanceUI.Add(_message);
		
		_button = new("Done!");
		InstanceUI.Add(_button);
		_button.OnClick += OnClick;
	}

	private void OnClick() {
		_index++;
		
		if (_index <= _messages.Count - 1)
			_message.Text = _messages[_index];
		
		if (_index == _messages.Count - 1)
			_button.Text = "Close";
		else if (_index >= _messages.Count)
			Core.Close(this);
	}

}