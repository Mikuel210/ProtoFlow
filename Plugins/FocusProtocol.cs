using SDK.InstanceTools;

namespace Plugins;
using SDK;

[CanClientOpen(false)]
[CanClientClose(false)]
[InstanceDescription("A protocol for focusing on stuff that matters")]
public class FocusProtocol : ProtocolInstance {
	
	// Private fields
	private Heading h_goal;
	private Heading h_time;

	private Input in_goal;
	private Input in_minutes;
	private Button btn_focus;
	
	private bool _focusing;
	private DateTime _startTime;
	private TimeSpan _timeSpan;

	public override void Open() {
		h_goal = new("Goal", 2);
		h_time = new("Time", 3);

		in_goal = new(placeholder: "Session goal");
		in_minutes = new(Input.InputType.Number, placeholder: "Focus minutes");
		btn_focus = new("Start focus");
		btn_focus.OnClick += OnFocusButtonClick;

		InstanceUI.Add(in_goal);
		InstanceUI.Add(in_minutes);
		InstanceUI.Add(btn_focus);
	}
	
	private void OnFocusButtonClick() {
		if (!int.TryParse(in_minutes.Text, out int minutes)) return;

		var audio = new Audio(Server.Clients, "https://audiohosting.netlify.app/WintergatanLive.mp3");
		audio.Play();

		_focusing = true;
		_startTime = DateTime.Now;
		_timeSpan = TimeSpan.FromMinutes(minutes);

		InstanceUI.Remove(in_goal);
		InstanceUI.Remove(in_minutes);
		InstanceUI.Remove(btn_focus);

		h_goal.Text = in_goal.Text;
		InstanceUI.Insert(0, h_time);
		InstanceUI.Insert(0, h_goal);
	}

	public override void Loop() {
		if (!_focusing) return;

		var elapsed = DateTime.Now - _startTime;
		var timeLeft = _timeSpan - elapsed;

		if (timeLeft.TotalSeconds > 0) {
			h_time.Text = timeLeft.ToString("c")[..11];
		} else {
			_focusing = false;
			h_time.Text = "00:00:00.00";
			Server.Clients.ForEach(e => e.ShowNotification("Focus session ended", h_goal.Text));

			Core.Close(this);
		}
	}

}