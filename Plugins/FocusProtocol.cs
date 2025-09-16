using System.Collections.Generic;
using SDK;
using SDK.InstanceTools;

namespace Plugins;

[InstanceDescription("A protocol for focusing on stuff that matters")]
public class FocusProtocol : ProtocolInstance
{
    private struct Song(string name, string url)
    {
        public string Name { get; } = name;
        public string URL { get; } = url;
    }

    private List<Song> _songs = [
        new("Wintergatan Live at Debaser Strand (45 min)", "https://audiohosting.netlify.app/WintergatanLive.mp3")
    ];

    private TaskSystem.Task? _task;
    private DateTime _startTime;
    private TimeSpan _duration;
    private bool _started;
    private bool _ended;

    // UI
    private List<Checkbox> _songCheckboxes = new();
    private Input _durationInput;
    private Checkbox _flowCheckbox;
    private Heading _timeHeading;
    private Button _closeButton;


    public override void Open()
    {
        var focusOnTask = new Button("Focus on task");
        var focusOnProject = new Button("Focus on project");

        InstanceUI.Add(focusOnTask);
        InstanceUI.Add(focusOnProject);

        focusOnProject.OnClick += ShowProjects;
        focusOnTask.OnClick += () => ShowTasks();
    }

    private void ShowProjects()
    {
        InstanceUI.Clear();
        InstanceUI.Add(new Heading("Select a project", 2));

        var projectSystem = Core.GetSystemInstance<ProjectSystem>();
        var projects = new List<ProjectSystem.Project?> { projectSystem.MainProject, projectSystem.SecondaryProject };

        foreach (var project in projects)
        {
            if (project == null) continue;
            
            var checkbox = new Checkbox(project.Name);
            InstanceUI.Add(checkbox);

            checkbox.OnChecked += () => ShowTasks(project == projectSystem.MainProject ? 
                CalendarSystem.ProjectCategory.MainProject :
                CalendarSystem.ProjectCategory.SecondaryProject);
        }
    }

    public void ShowTasks(CalendarSystem.ProjectCategory projectCategory = default)
    {
        InstanceUI.Clear();
        InstanceUI.Add(new Heading("Select a task", 2));

        var tasks = Core.GetSystemInstance<TaskSystem>().Tasks;
        
        if (projectCategory != CalendarSystem.ProjectCategory.None) 
            tasks = tasks.Where(e => e.ProjectCategory == projectCategory).ToList();

        foreach (var task in tasks)
        {
            var checkbox = new Checkbox(task.Name);
            InstanceUI.Add(checkbox);

            checkbox.OnChecked += () => SelectTask(task);
        }
    }

    private void SelectTask(TaskSystem.Task task)
    {
        _task = task;
        InstanceUI.Clear();

        InstanceUI.Add(new Heading("Select a duration", 2));

        _durationInput = new(placeholder: "Focus minutes");
        InstanceUI.Add(_durationInput);

        _flowCheckbox = new("Flow");
        InstanceUI.Add(_flowCheckbox);

        var continueButton = new Button("Continue");
        InstanceUI.Add(continueButton);

        continueButton.OnClick += Continue;
    }

    private void Continue()
    {
        InstanceUI.Clear();

        InstanceUI.Add(new Heading("Select focus music", 2));

        foreach (var song in _songs)
            _songCheckboxes.Add(new(song.Name));

        _songCheckboxes.ForEach(e => InstanceUI.Add(e));

        var startButton = new Button("Start");
        startButton.OnClick += Start;
        InstanceUI.Add(startButton);
    }

    private void Start() {
        if (!int.TryParse(_durationInput.Text, out var minutes) && !_flowCheckbox.Checked) return;
        
        InstanceUI.Clear();

        var songName = _songCheckboxes.FirstOrDefault(e => e.Checked)?.Text;
        
        if (songName != null) {
            var song = _songs.First(e => e.Name == songName);
            var audio = new Audio(Server.Clients, song.URL);
            InstanceAudio.Register(audio).Play();   
        }

        InstanceUI.Add(new Heading(_task!.Name, 2));

        _timeHeading = new(level: 4);
        InstanceUI.Add(_timeHeading);

        _closeButton = new("Close");
        _closeButton.OnClick += () => Core.Close(this);
        InstanceUI.Add(_closeButton);

        _startTime = DateTime.Now;
        _duration = TimeSpan.FromMinutes(minutes);
        _started = true;
    }

    public override void Loop()
    {
        if (!_started) return;

        var elapsed = DateTime.Now - _startTime;
        var timeLeft = _duration - elapsed;

        if (_flowCheckbox.Checked) {
            _timeHeading.Text = $"T+{elapsed.ToString()}";
            return;
        }
        
        if (timeLeft > TimeSpan.Zero)
            _timeHeading.Text = $"T-{timeLeft.ToString()}";
        else if (!_ended)
        {
            _timeHeading.Text = "LIFTOFF";
            Server.Clients.ForEach(e => e.ShowNotification(_task!.Name, "Focus session ended"));

            _ended = true;
        }
    }
}