using SDK.InstanceTools;

namespace Plugins;
using SDK;

public class ProjectManagementSystem : SystemInstance {
	
	// Private fields
	[Serializable]
	private struct Project {

		public string name;
		public string description;

	}
	
	[InstanceStorage] private List<Project> _projects = new();

	private Button btn_createProject;
	private Input in_projectName;
	private Input in_projectDescription;
	
	
	public override void Open() {
		// Setup UI
		in_projectName = new(placeholder: "Project name");
		in_projectDescription = new(placeholder: "Project description");
		btn_createProject = new("Create");
		
		InstanceUI.Add(in_projectName);
		InstanceUI.Add(in_projectDescription);
		InstanceUI.Add(btn_createProject);
		InstanceUI.Add(new HorizontalRule());
		
		btn_createProject.OnClick += CreateProjectFromInput;
		
		// Setup existing projects
		foreach (Project project in _projects)
			CreateProject(project.name, project.description, false);
	}

	private void CreateProject(string name, string description, bool add = true) {
		// Add project to list
		var project = new Project {
			name = name,
			description = description
		};
		
		if (add)
			_projects.Add(project);

		// Update UI
		var heading = new Heading(name);
		var paragraph = new Paragraph(description);
		var focusButton = new Button("Focus");
		var deleteButton = new Button("Delete");
		
		InstanceUI.Add(heading);
		InstanceUI.Add(paragraph);
		InstanceUI.Add(focusButton);
		InstanceUI.Add(deleteButton);

		// Add listeners
		focusButton.OnClick += () => Focus(project);
		
		deleteButton.OnClick += () => {
			_projects.Remove(project);
			InstanceUI.Remove(heading);
			InstanceUI.Remove(paragraph);
			InstanceUI.Remove(focusButton);
			InstanceUI.Remove(deleteButton);
		};
	}

	private void CreateProjectFromInput() => CreateProject(in_projectName.Text, in_projectDescription.Text);

	private void Focus(Project project) {
		FocusProtocol instance = Core.Open<FocusProtocol>();
		instance.Title = $"Focus on {project.name}";
	}

}