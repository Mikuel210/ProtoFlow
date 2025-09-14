using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SDK.InstanceTools;

[Serializable] public class InstanceUI : ObservableCollection<UIElement>;

[Serializable] public abstract class UIElement : INotifyPropertyChanged {
	
	public event PropertyChangedEventHandler? PropertyChanged;

	public Dictionary<string, object> Properties { get; } = new();
	public string ElementID { get; private set; }

	protected UIElement() {
		ElementID = Guid.NewGuid().ToString();
		PropertyChanged += UpdateProperty;
	}
	public static UIElement FromElementID(string elementID) =>
		Core.OpenInstances
			.First(e => e.InstanceUI.ToList().Exists(e => e.ElementID == elementID)).InstanceUI
			.First(e => e.ElementID == elementID);
	
	public void DeserializeElementID(string elementID) => ElementID = elementID;
	
	protected void UpdateProperty(object? sender, PropertyChangedEventArgs e) {
		// Get property
		var propertyName = e.PropertyName!;
		var property = GetType().GetProperty(propertyName)!;
		if (property.DeclaringType != GetType()) return;
		
		// Update value on dictionary
		var value = property.GetValue(this)!;
		if (value.GetType().IsEnum) value = value.ToString()!;
		Properties[propertyName] = value;
		
		// Update client
		UpdateClient(propertyName, value);
	}
	protected void UpdateClient(string propertyName, object value) {
		var instance = Core.OpenInstances.FirstOrDefault(e => e.InstanceUI.Contains(this));
		if (instance == null) return; // Ignore on object creation
		
		foreach (Client client in new List<Client>(Server.Clients)) {
			if (client.OpenInstance != instance) continue;
			client.PongUpdateUIElement(this, propertyName, value);
		}
	}
	public void InvokeUIEvent(string eventName, Dictionary<string, object> arguments) {
		// Get event
		var field = GetType().GetField($"On{eventName}", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field?.GetValue(this) is not Delegate eventDelegate) return;

		// Invoke subscribers
		foreach (var method in eventDelegate.GetInvocationList().Select(e => e.Method)) {
			var parameters = method.GetParameters();

			// Build argument array from dictionary
			var argumentArray = parameters
				.Where(e => arguments.Keys.Contains(e.Name!))
				.Select(e => arguments[e.Name!])
				.ToArray();
			
			eventDelegate.DynamicInvoke(argumentArray);
		}
	}
	
}

[Serializable] public class Heading : UIElement {
	
	public string Text { get; set; }
	[Range(1, 6)] public int Level { get; set; }
	
	public Heading(string text = "", int level = 1) {
		Text = text;
		Level = level;
	}

}

[Serializable] public class Paragraph : UIElement {
	
	public string Text { get; set; }
	public Paragraph(string text = "") { Text = text; }

}

[Serializable] public class HorizontalRule : UIElement;

public delegate void ValueChangedEventHandler(string value);
[Serializable] public class Input : UIElement {
	
	public enum InputType {

		Text,
		Number

	}

	public InputType Type { get; set; }
	public string Text { get; set; }
	public string Placeholder { get; set; }

	public event ValueChangedEventHandler OnValueChanged;
	
	public Input(InputType type = default, string text = "", string placeholder = "") {
		Type = type;
		Text = text;
		Placeholder = placeholder;

		OnValueChanged += value => {
			// Change backing field not to trigger UI update
			string fieldName = "<Text>k__BackingField";
			var field = GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
			field.SetValue(this, value);
		};
	}
	
}

public delegate void ClickEventHandler();
[Serializable] public class Button : UIElement {

	public string Text { get; set; }
	public event ClickEventHandler OnClick;
	
	public Button(string text = "") => Text = text;

}