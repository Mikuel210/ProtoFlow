using SDK;
using SDK.InstanceTools;

namespace Plugins;

public class CaptureSystem : SystemInstance {
	
	// Private fields
	[InstanceStorage] private List<string> _captureList = new();
	private List<UIElement> _captureElements = new();
	
	public override void Open() {
		InstanceUI.Add(new Heading("Capture List", 2));
		Update();
	}

	public void Capture(string text) {
		_captureList.Add(text);
		Update();
	}

	private void Update() {
		foreach (UIElement element in _captureElements)
			InstanceUI.Remove(element);
		
		_captureElements.Clear();

		foreach (string text in _captureList) {
			Heading heading = new Heading(text, 4);
			_captureElements.Add(heading);
			InstanceUI.Add(heading);

			Button deleteButton = new Button("Delete");
			_captureElements.Add(deleteButton);
			InstanceUI.Add(deleteButton);

			deleteButton.OnClick += () => {
				_captureList.Remove(text);
				Update();
			};

			HorizontalRule horizontalRule = new();
			_captureElements.Add(horizontalRule);
			InstanceUI.Add(horizontalRule);
		}
	}

}