using SDK;
using SDK.InstanceTools;

namespace Plugins;

[CanClientOpen(false)]
[CanClientClose(false)]
[InstanceDescription("A protocol for winding down")]
public class NightProtocol : ProtocolInstance {

	private List<string> _messages = [
		"Wash face and teeth",
		"Prepare schoolbag",
		"Put pyjama on",
		"Read 1 chapter",
		"Good night"
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