# ProtoFlow Guide

This guide will walk you through creating your first system and protocol for ProtoFlow. At the end of this guide, you will have made a Project Management System and a Focus Protocol to help you stay organized.

## Making your first protocol

We're going to make a simple protocol to help you focus. The Focus Protocol first asks the user for a session goal and duration. During the focus session, the protocol displays the goal and the remaining time, and the protocol plays focus music. When the session ends, the protocol shows a notification and closes itself.

### Protocol setup

Protocols and systems live inside of the `Plugins` project inside the ProtoFlow solution. To create a new protocol, create a new file in this directory called `FocusProtocol.cs` and define a new class that inherits from `ProtocolInstance`:

```csharp
using SDK;
using SDK.InstanceTools;

namespace Plugins;

public class FocusProtocol : ProtocolInstance;
```

Let's test if our new protocol shows up in the client. Run `dotnet build` on the `Plugins` directory to compile your protocols and systems. You'll have to do this every time you make changes to your setup. Next, start the server by running `dotnet run` on the `Server` project inside the ProtoFlow solution. You should be able to open your new protocol from the client now.

TODO: Add image

### Adding metadata

Metadata tells ProtoFlow how it should treat your protocols or systems. Metadata is implemented in the form of attributes that you add to your protocol and system classes. Let's take a look at some of the most important attributes.

- `InstanceName(string name)` sets a name for the system or protocol. The instance name defaults to the class name with spaces between words.
- `InstanceDescription(string description)` sets a description for the system or protocol.
- `CanClientOpen(bool canClientOpen)` tells whether or not clients can open the protocol.
- `CanClientClose(bool canClientClose)` tells whether or not clients can close the protocol.

For our protocol, we'll only override the description.

```csharp
[InstanceDescription("A protocol for focusing on stuff that matters")]
public class FocusProtocol : ProtocolInstance;
```

### Adding UI elements

Let's make a user interface that asks for a session goal and duration before starting the focus session. To do this, we have to add new UI elements when the protocol is opened. You can override the `Open` method to perform setup actions such as these:

```csharp
public class FocusProtocol : ProtocolInstance {

    public override void Open() {
        // Initialize UI
        InstanceUI.Add(new Input(Input.InputType.Text, placeholder: "Session goal"));
        InstanceUI.Add(new Input(Input.InputType.Number, placeholder: "Focus minutes"));
        InstanceUI.Add(new Button("Start session"));
    }

}
```

In the previous code, we've added two `Input` elements and one `Button` element to the protocol UI. `Input` elements take an input type <small>(text or number)</small>, a default text and a placeholder. `Button` elements take a text content only.

`InstanceUI` is a collection of `UIElement`s. All elements on the collection are automatically shown on clients. However, if we want to access the properties of the elements we've added <small>(such as the text the user has input, or the click event for the button)</small> we will have to keep a reference to them:

```csharp
public class FocusProtocol : ProtocolInstance {

    private Input _sessionGoalInput;
    private Input _focusMinutesInput;
    private Button _startSessionButton;

    public override void Open() {
        // Create UI elements
        _sessionGoalInput = new Input(Input.InputType.Text, placeholder: "Session goal");
        _focusMinutesInput = new Input(Input.InputType.Number, placeholder: "Focus minutes");
        _startSessionButton = new Button("Start session");

        // Add elements to InstanceUI
        InstanceUI.Add(_sessionGoalInput);
        InstanceUI.Add(_focusMinutesInput);
        InstanceUI.Add(_startSessionButton);
    }

}
```

Let's listen to the click event of our button to start the focus session. When the button is clicked, all UI elements are removed and new ones are added to show the session goal and remaining time.

```csharp
public class FocusProtocol : ProtocolInstance {

    private Input _sessionGoalInput;
    private Input _focusMinutesInput;
    private Button _startSessionButton;

    private bool _sessionStarted;
    private Heading _timeHeading;
    private TimeSpan _focusDuration;
    private DateTime _startTime;

    public override void Open() {
        // Create UI elements
        _sessionGoalInput = new Input(Input.InputType.Text, placeholder: "Session goal");
        _focusMinutesInput = new Input(Input.InputType.Number, placeholder: "Focus minutes");
        _startSessionButton = new Button("Start session");

        // Add elements to InstanceUI
        InstanceUI.Add(_sessionGoalInput);
        InstanceUI.Add(_focusMinutesInput);
        InstanceUI.Add(_startSessionButton);

        // Listen to the click event of the button
        _startSessionButton.OnClick += StartSession;
    }

    private void StartSession() {
        _sessionStarted = true;
        _startTime = DateTime.Now;

        // Get focus minutes from user input and set focus duration
        int minutes = int.Parse(_focusMinutesInput.Text);
        _focusDuration = TimeSpan.FromMinutes(minutes);

        // Clear UI and add new elements
        InstanceUI.Clear();

        string sessionGoal = _sessionGoalInput.Text;
        InstanceUI.Add(new Heading(sessionGoal, 2));

        _timeHeading = new Heading(level: 4);
        InstanceUI.Add(_timeHeading);
    }

}
```

The previous code listened to the button `OnClick` event to start the focus session. Then, it retrieved the session goal and focus duration from the `Text` property in `Input`s. Finally it used `InstanceUI.Clear()` to remove all existing UI elements and it added new ones. The `Heading` element takes a text content and a level from 1 to 6.

Now, let's make the time Heading update continuously to show the remaining focus time. To do that, you can override the `Loop` method.

```csharp
public override void Loop() {
    if (!_sessionStarted) return;

    var elapsed = DateTime.Now - _startTime;
    var timeLeft = _focusDuration - elapsed;
    
    _timeHeading.Text = timeLeft.ToString();
}
```

Finally, let's show a notification and close the protocol when the session ends.

```csharp
public override void Loop() {
    if (!_sessionStarted) return;

    TimeSpan elapsed = DateTime.Now - _startTime;
    TimeSpan timeLeft = _focusDuration - elapsed;
    
    _timeHeading.Text = timeLeft.ToString();

    if (timeLeft > TimeSpan.Zero) return;

    // Focus session has ended
    foreach (Client client in Server.Clients)
        client.ShowNotification("Focus session ended", "Take a break now");
    
    Core.Close(this);
}
```

In the previous code, we have iterated through every connected client, listed in `Server.Clients`. On each of them, we have called the `ShowNotification` method that takes a title and an optional body. You can consult the documentation to see all available methods on clients.

Finally, we have called `Core.Close(this)` to close the current protocol. `Core` holds methods for everything related to instance management. Consult the documentation to see all available methods.

### Adding focus music

To add focus music, we have to create an `Audio` object, register it on `InstanceAudio` and call the `Play` method on it. `Audio` takes a list of clients to play the audio on and a source URL. Note that hosting audios online is the only supported method of playing them as of now.

```csharp
private void StartSession() {
    _sessionStarted = true;
    _startTime = DateTime.Now;

    // Get focus minutes from user input and set focus duration
    int minutes = int.Parse(_focusMinutesInput.Text);
    _focusDuration = TimeSpan.FromMinutes(minutes);

    // Clear UI and add new elements
    InstanceUI.Clear();

    string sessionGoal = _sessionGoalInput.Text;
    InstanceUI.Add(new Heading(sessionGoal, 2));

    _timeHeading = new Heading(level: 4);
    InstanceUI.Add(_timeHeading);

    // Play music
    Audio music = new Audio(Server.Clients, "https://audiohosting.netlify.app/WintergatanLive.mp3");
    InstanceAudio.Register(music);

    music.Play();
}
```

`Audio`s also feature `Pause` and `Stop` methods. Consult the documentation to see all available methods. 

### Finished code

```csharp
using SDK;
using SDK.InstanceTools;

namespace Plugins;

public class FocusProtocol : ProtocolInstance {

    private Input _sessionGoalInput;
    private Input _focusMinutesInput;
    private Button _startSessionButton;

    private bool _sessionStarted;
    private Heading _timeHeading;
    private TimeSpan _focusDuration;
    private DateTime _startTime;

    public override void Open() {
        // Create UI elements
        _sessionGoalInput = new Input(Input.InputType.Text, placeholder: "Session goal");
        _focusMinutesInput = new Input(Input.InputType.Number, placeholder: "Focus minutes");
        _startSessionButton = new Button("Start session");

        // Add elements to InstanceUI
        InstanceUI.Add(_sessionGoalInput);
        InstanceUI.Add(_focusMinutesInput);
        InstanceUI.Add(_startSessionButton);

        // Listen to the click event of the button
        _startSessionButton.OnClick += StartSession;
    }

    private void StartSession() {
        _sessionStarted = true;
        _startTime = DateTime.Now;

        // Get focus minutes from user input and set focus duration
        int minutes = int.Parse(_focusMinutesInput.Text);
        _focusDuration = TimeSpan.FromMinutes(minutes);

        // Clear UI and add new elements
        InstanceUI.Clear();

        string sessionGoal = _sessionGoalInput.Text;
        InstanceUI.Add(new Heading(sessionGoal, 2));

        _timeHeading = new Heading(level: 4);
        InstanceUI.Add(_timeHeading);

        // Play music
        Audio music = new Audio(Server.Clients, "https://audiohosting.netlify.app/WintergatanLive.mp3");
        InstanceAudio.Register(music);

        music.Play();
    }

    public override void Loop() {
        if (!_sessionStarted) return;

        TimeSpan elapsed = DateTime.Now - _startTime;
        TimeSpan timeLeft = _focusDuration - elapsed;
        
        _timeHeading.Text = timeLeft.ToString();

        if (timeLeft > TimeSpan.Zero) return;

        // Focus session has ended
        foreach (Client client in Server.Clients)
            client.ShowNotification("Focus session ended", "Take a break now");
        
        Core.Close(this);
    }

}
```