using SDK;
using SDK.InstanceTools;

namespace Plugins;

[InstanceDescription("A system for getting stuff done")]
public class TaskSystem : SystemInstance
{
    [Serializable]
    public class Task
    {
        public string Name { get; set; }
        public CalendarSystem.ProjectCategory ProjectCategory { get; set; }
        public DateTime Do { get; set; }
        public DateTime? Due { get; set; }
    }

    [InstanceStorage] public List<Task> Tasks { get; set; } = new();


    // UI
    private Input _nameInput;
    private Input _doInput;
    private Input _dueInput;
    private Button _createButton;
    private Checkbox _mainProjectCheckbox;
    private Checkbox _secondaryProjectCheckbox;
    private List<UIElement> _listElements = new();


    public override void Open()
    {
        InstanceUI.Add(new Heading("New task", 3));

        _nameInput = new(placeholder: "Task goes here");
        InstanceUI.Add(_nameInput);

        _doInput = new(placeholder: "Do date (dd/MM/yyyy)");
        InstanceUI.Add(_doInput);

        _dueInput = new(placeholder: "Due date (dd/MM/yyyy)");
        InstanceUI.Add(_dueInput);

        InstanceUI.Add(new Heading("Select projects", 4));
        
        _mainProjectCheckbox = new("Main Project");
        InstanceUI.Add(_mainProjectCheckbox);

        _secondaryProjectCheckbox = new("Secondary Project");
        InstanceUI.Add(_secondaryProjectCheckbox);

        _createButton = new("Create");
        _createButton.OnClick += OnCreateClick;
        InstanceUI.Add(_createButton);

        InstanceUI.Add(new HorizontalRule());
        UpdateList();
    }

    private void OnCreateClick()
    {
        var projectSystem = Core.GetSystemInstance<ProjectSystem>();
        var projectCategory = CalendarSystem.ProjectCategory.None;
        
        if (_mainProjectCheckbox.Checked) projectCategory = CalendarSystem.ProjectCategory.MainProject;
        if (_secondaryProjectCheckbox.Checked) projectCategory = CalendarSystem.ProjectCategory.SecondaryProject;
        
        if (!DateTime.TryParse(_doInput.Text, out var doDate)) return;
        var hasDueDate = DateTime.TryParse(_dueInput.Text, out var dueDate);
        
        Tasks.Add(new() {
            Name = _nameInput.Text,
            Do = doDate,
            Due = hasDueDate ? dueDate : null,
            ProjectCategory = projectCategory
        });

        UpdateList();
    }

    private void UpdateList()
    {
        _listElements.ForEach(e => InstanceUI.Remove(e));
        _listElements.Clear();

        List<DateTime> dates = Tasks.GroupBy(e => e.Do).Select(e => e.First().Do).ToList();

        foreach (DateTime date in dates)
        {
            var heading = new Heading(date.ToString("MM/dd/yyyy"), 2);
            _listElements.Add(heading);
            InstanceUI.Add(heading);

            foreach (Task task in Tasks.Where(e => e.Do == date))
            {
                string text = task.Name + (task.Due == null ? "" : $" ({task.Due:MM/dd/yyyy})");
                var projectSystem = Core.GetSystemInstance<ProjectSystem>();
                
                if (task.ProjectCategory == CalendarSystem.ProjectCategory.MainProject && projectSystem.MainProject != null)
                    text = $"{projectSystem.MainProject.Name}: {text}";
                
                if (task.ProjectCategory == CalendarSystem.ProjectCategory.SecondaryProject && projectSystem.SecondaryProject != null)
                    text = $"{projectSystem.SecondaryProject.Name}: {text}";

                var checkbox = new Checkbox(text);

                checkbox.OnChecked += () =>
                {
                    Tasks.Remove(task);
                    UpdateList();
                };

                _listElements.Add(checkbox);
                InstanceUI.Add(checkbox);
            }
        }
    }
}