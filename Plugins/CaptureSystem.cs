using SDK;
using SDK.InstanceTools;

namespace Plugins;

[InstanceDescription("A system for turning ideas into stuff")]
public class CaptureSystem : SystemInstance
{
    [InstanceStorage] public List<string> CaptureList { get; set; } = new();
    
    // UI
    private Input _captureInput;
    private Button _captureButton;
    private List<UIElement> _listElements = new();

    public override void Open()
    {
        _captureInput = new(placeholder: "Your thoughts go here...");
        InstanceUI.Add(_captureInput);

        _captureButton = new("Capture");
        InstanceUI.Add(_captureButton);

        _captureButton.OnClick += () =>
        {
            CaptureList.Add(_captureInput.Text);
            UpdateList();
        };

        InstanceUI.Add(new HorizontalRule());
    }

    private void UpdateList()
    {
        _listElements.ForEach(e => InstanceUI.Remove(e));
        _listElements.Clear();
        
        foreach (string capture in CaptureList)
        {
            var paragraph = new Paragraph(capture);
            _listElements.Add(paragraph);
            InstanceUI.Add(paragraph);

            var deleteButton = new Button("Done");
            _listElements.Add(deleteButton);
            InstanceUI.Add(deleteButton);

            deleteButton.OnClick += () =>
            {
                CaptureList.Remove(capture);
                UpdateList();
            };

            var horizontalRule = new HorizontalRule();
            _listElements.Add(horizontalRule);
            InstanceUI.Add(horizontalRule);
        }
    }
}