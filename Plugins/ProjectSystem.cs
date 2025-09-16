using SDK;
using SDK.InstanceTools;

namespace Plugins;

[InstanceName("Project Manager")]
[InstanceDescription("A system for working on projects that matter")]
public class ProjectSystem : SystemInstance
{
    [Serializable]
    public class Project
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    [InstanceStorage] public Project? MainProject { get; set; }
    [InstanceStorage] public Project? SecondaryProject { get; set; }
    [InstanceStorage] public List<Project> ProjectIdeas { get; set; } = new();

    // UI
    private List<UIElement> _projectIdeaElements = new();
    private List<UIElement> _mainProjectElements = new();
    private List<UIElement> _secondaryProjectElements = new();

    private Input _projectNameInput;
    private Input _projectDescriptionInput;
    private Button _addIdeaButton;

    private int MainProjectIndex => 1;
    private int SecondaryProjectIndex => MainProjectIndex + _mainProjectElements.Count + 1;


    public override void Open()
    {
        InstanceUI.Add(new Heading("Main project", 3));
        InstanceUI.Add(new Heading("Secondary project", 3));

        InstanceUI.Add(new HorizontalRule());
        InstanceUI.Add(new Heading("Project ideas", 2));
        
        _projectNameInput = new(placeholder: "Project name");
        InstanceUI.Add(_projectNameInput);
        
        _projectDescriptionInput = new(placeholder: "Project description");
        InstanceUI.Add(_projectDescriptionInput);

        _addIdeaButton = new("Add idea");
        InstanceUI.Add(_addIdeaButton);

        _addIdeaButton.OnClick += () => {
            ProjectIdeas.Add(new() {
                Name = _projectNameInput.Text,
                Description = _projectDescriptionInput.Text
            });
            
            UpdateProjectIdeas();
        };
        
        InstanceUI.Add(new HorizontalRule());
        
        UpdateProjects();
        UpdateProjectIdeas();
    }

    private void UpdateProjectIdeas()
    {
        _projectIdeaElements.ForEach(e => InstanceUI.Remove(e));
        _projectIdeaElements.Clear();

        foreach (var project in ProjectIdeas) {
            var name = new Heading(project.Name, 4);
            _projectIdeaElements.Add(name);
            InstanceUI.Add(name);

            var description = new Paragraph(project.Description);
            _projectIdeaElements.Add(description);
            InstanceUI.Add(description);

            var ascendToMain = new Button("Ascend to Main project");
            _projectIdeaElements.Add(ascendToMain);
            InstanceUI.Add(ascendToMain);

            ascendToMain.OnClick += () =>
            {
                MainProject = project;
                ProjectIdeas.Remove(project);
                UpdateProjectIdeas();
                UpdateProjects();
            };

            var ascendToSecondary = new Button("Ascend to Secondary project");
            _projectIdeaElements.Add(ascendToSecondary);
            InstanceUI.Add(ascendToSecondary);

            ascendToSecondary.OnClick += () =>
            {
                SecondaryProject = project;
                ProjectIdeas.Remove(project);
                UpdateProjectIdeas();
                UpdateProjects();
            };

            var delete = new Button("Delete project");
            _projectIdeaElements.Add(delete);
            InstanceUI.Add(delete);

            delete.OnClick += () =>
            {
                ProjectIdeas.Remove(project);
                UpdateProjectIdeas();
            };
        }
    }

    private void UpdateProjects()
    {
        _mainProjectElements.ForEach(e => InstanceUI.Remove(e));
        _mainProjectElements.Clear();

        _secondaryProjectElements.ForEach(e => InstanceUI.Remove(e));
        _secondaryProjectElements.Clear();

        if (MainProject != null) _mainProjectElements = InsertProject(MainProject, MainProjectIndex);
        if (SecondaryProject != null) _secondaryProjectElements = InsertProject(SecondaryProject, SecondaryProjectIndex);
    }

    private List<UIElement> InsertProject(Project project, int index)
    {
        var focusButton = new Button($"Focus on {project.Name}");
        InstanceUI.Insert(index, focusButton);

        var description = new Paragraph(project.Description);
        InstanceUI.Insert(index, description);
        
        var name = new Heading(project.Name, 4);
        InstanceUI.Insert(index, name);

        focusButton.OnClick += () => {
            var instance = Core.Open<FocusProtocol>();
            
            instance.ShowTasks(project == MainProject ? 
                CalendarSystem.ProjectCategory.MainProject : 
                CalendarSystem.ProjectCategory.SecondaryProject);
            
            instance.Title = $"Focus on {project.Name}";
        };

        return [name, description, focusButton];
    }

    public InstanceEvents.Event RegisterProjectTimeBlock(CalendarSystem.TimeBlock timeBlock)
    {
        return InstanceEvents.RegisterTimeEvent(timeBlock.StartTime, timeBlock.Days, () =>
        {
            var project = timeBlock.ProjectCategory == CalendarSystem.ProjectCategory.MainProject ? MainProject : SecondaryProject;
            
            var instance = Core.Open<FocusProtocol>();
            instance.ShowTasks(timeBlock.ProjectCategory);
            instance.Title = $"Focus on {project!.Name}";
        });
    }
}