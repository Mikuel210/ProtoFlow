using System.Text.Json.Serialization;
using SDK;
using SDK.InstanceTools;

namespace Plugins;

[InstanceDescription("A system for turning time into stuff")]
public class CalendarSystem : SystemInstance
{
    public enum ProjectCategory
    {
        None,
        MainProject,
        SecondaryProject
    }

    [Serializable]
    public class TimeBlock
    {
        public string Name { get; set; } = string.Empty;
        public ProjectCategory ProjectCategory { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public InstanceEvents.Days Days { get; set; }
        [JsonIgnore] public InstanceEvents.Event? ProjectEvent { get; set; }
    }

    [InstanceStorage] public List<TimeBlock> TimeBlocks { get; set; } = new();

    // UI
    private Input _nameInput;
    private Checkbox _mainProjectCheckbox;
    private Checkbox _secondaryProjectCheckbox;
    private List<Checkbox> _dayCheckboxes = new();
    private Input _startTimeInput;
    private Input _endTimeInput;
    private List<UIElement> _elements = new();

    public override void Deserialize() {
        if (Core.GetOpenInstance<ProjectSystem>() == null)
            InstanceEvents.RegisterInstanceOpenedEvent<ProjectSystem>(SetupProjectEvents);
        else
            SetupProjectEvents();
    }

    private void SetupProjectEvents() {
        var projectSystem = Core.GetSystemInstance<ProjectSystem>();
        
        TimeBlocks
            .Where(e => e.ProjectCategory != ProjectCategory.None)
            .ToList()
            .ForEach(e => e.ProjectEvent = projectSystem.RegisterProjectTimeBlock(e));
    }

    public override void Open()
    {
        InstanceUI.Add(new Heading("New time block", 3));

        _nameInput = new(placeholder: "Time block name");
        InstanceUI.Add(_nameInput);

        InstanceUI.Add(new Heading("Select project", 4));

        _mainProjectCheckbox = new("Main project");
        InstanceUI.Add(_mainProjectCheckbox);

        _secondaryProjectCheckbox = new("Secondary project");
        InstanceUI.Add(_secondaryProjectCheckbox);

        InstanceUI.Add(new Heading("Select days", 4));

        string[] days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

        foreach (string day in days)
        {
            var checkbox = new Checkbox(day);
            _dayCheckboxes.Add(checkbox);
            InstanceUI.Add(checkbox);
        }

        _startTimeInput = new(placeholder: "Start time (hh:mm)");
        InstanceUI.Add(_startTimeInput);

        _endTimeInput = new(placeholder: "End time (hh:mm)");
        InstanceUI.Add(_endTimeInput);

        var createButton = new Button("Create");
        createButton.OnClick += Create;
        InstanceUI.Add(createButton);
        
        InstanceUI.Add(new HorizontalRule());
        
        UpdateTimeBlocks();
    }

    private void Create()
    {
        var projectCategory = ProjectCategory.None;

        if (_mainProjectCheckbox.Checked)
            projectCategory = ProjectCategory.MainProject;
        else if (_secondaryProjectCheckbox.Checked)
            projectCategory = ProjectCategory.SecondaryProject;

        var days = InstanceEvents.Days.None;

        for (int i = 0; i < 7; i++) {
            if (!_dayCheckboxes[i].Checked) continue;

            switch (i) {
                case 0: days |= InstanceEvents.Days.Monday; break;
                case 1: days |= InstanceEvents.Days.Tuesday; break;
                case 2: days |= InstanceEvents.Days.Wednesday; break;
                case 3: days |= InstanceEvents.Days.Thursday; break;
                case 4: days |= InstanceEvents.Days.Friday; break;
                case 5: days |= InstanceEvents.Days.Saturday; break;
                case 6: days |= InstanceEvents.Days.Sunday; break;
            }
        }

        var startTime = StringToTimeSpan(_startTimeInput.Text);
        var endTime = StringToTimeSpan(_endTimeInput.Text);
        if (startTime == null || endTime == null) return;

        var timeBlock = new TimeBlock {
            Name = _nameInput.Text,
            ProjectCategory = projectCategory,
            StartTime = (TimeSpan)startTime,
            EndTime = (TimeSpan)endTime,
            Days = days
        };

        TimeBlocks.Add(timeBlock);

        if (projectCategory != ProjectCategory.None)
            Core.GetSystemInstance<ProjectSystem>().RegisterProjectTimeBlock(timeBlock);

        UpdateTimeBlocks();
    }

    private TimeSpan? StringToTimeSpan(string input) {
        string[] values = input.Split(':');

        if (!int.TryParse(values[0], out var hours)) return null;
        if (!int.TryParse(values[1], out var minutes)) return null;
        
        return new(hours, minutes, 0);
    }

    private void UpdateTimeBlocks()
    {
        _elements.ForEach(e => InstanceUI.Remove(e));
        _elements.Clear();

        string[] dayStrings = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

        for (int i = 0; i < 7; i++)
        {
            var dayString = dayStrings[i];
            var Heading = new Heading(dayString, 2);
            _elements.Add(Heading);
            InstanceUI.Add(Heading);

            InstanceEvents.Days day;

            switch (i)
            {
                default: day = InstanceEvents.Days.Monday; break;
                case 1: day = InstanceEvents.Days.Tuesday; break;
                case 2: day = InstanceEvents.Days.Wednesday; break;
                case 3: day = InstanceEvents.Days.Thursday; break;
                case 4: day = InstanceEvents.Days.Friday; break;
                case 5: day = InstanceEvents.Days.Saturday; break;
                case 6: day = InstanceEvents.Days.Sunday; break;
            }

            foreach (var timeBlock in TimeBlocks.Where(e => e.Days.HasFlag(day)))
            {
                var projectString = string.Empty;
                var projectSystem = Core.GetSystemInstance<ProjectSystem>();

                if (timeBlock.ProjectCategory == ProjectCategory.MainProject && projectSystem.MainProject != null)
                    projectString = $" ({projectSystem.MainProject.Name})";

                if (timeBlock.ProjectCategory == ProjectCategory.SecondaryProject &&
                    projectSystem.SecondaryProject != null)
                    projectString = $" ({projectSystem.SecondaryProject.Name})";

                string startTime = $"{timeBlock.StartTime:hh\\:mm}";
                string endTime = $"{timeBlock.EndTime:hh\\:mm}";
                var element = new Checkbox($"{startTime} - {endTime}: {timeBlock.Name}{projectString}");

                element.OnChecked += () => {
                    TimeBlocks.Remove(timeBlock);
                    timeBlock.ProjectEvent?.Unregister();
                    UpdateTimeBlocks();
                };

                _elements.Add(element);
                InstanceUI.Add(element);
            }
        }
    }

}