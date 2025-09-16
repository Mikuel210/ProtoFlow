using SDK;
using SDK.InstanceTools;

namespace Plugins;

[ShowOnClient(false)]
public class RoutineSystem : SystemInstance
{
    private TimeSpan MorningTimeWeekdays => TimeSpan.FromHours(07) + TimeSpan.FromMinutes(20);
    private TimeSpan NightTimeWeekdays => TimeSpan.FromHours(22) + TimeSpan.FromMinutes(30);

    private TimeSpan MorningTimeWeekends => TimeSpan.FromHours(09) + TimeSpan.FromMinutes(00);
    private TimeSpan NightTimeWeekends => TimeSpan.FromHours(23) + TimeSpan.FromMinutes(30);

    public override void Open()
    {
        var weekdays = InstanceEvents.Days.Weekdays;
        var weekend = InstanceEvents.Days.Weekdays | InstanceEvents.Days.Sunday & ~InstanceEvents.Days.Friday;

        InstanceEvents.RegisterTimeEvent(MorningTimeWeekdays, weekdays, () => Core.Open<WeekdayMorningProtocol>());
        InstanceEvents.RegisterTimeEvent(NightTimeWeekdays, weekdays, () => Core.Open<WeekdayNightProtocol>());

        InstanceEvents.RegisterTimeEvent(MorningTimeWeekends, weekend, () => Core.Open<WeekendMorningProtocol>());
        InstanceEvents.RegisterTimeEvent(NightTimeWeekends, weekend, () => Core.Open<WeekendNightProtocol>());
    }
}

[CanClientOpen(false)]
public abstract class RoutineProtocol : ProtocolInstance
{
    public abstract string[] Messages { get; }

    public override void Open()
    {
        foreach (string message in Messages)
            InstanceUI.Add(new Checkbox(message));

        var button = new Button("Close");
        InstanceUI.Add(button);

        button.OnClick += () => Core.Close(this);
    }
}

[InstanceName("Morning Protocol")]
[CanClientOpen(false)]
public class WeekdayMorningProtocol : RoutineProtocol
{
    public override string[] Messages => [
        "7:20 - Wash face",
        "7:25 - Shower",
        "7:35 - Dress up",
        "7:40 - Breakfast + grab snack",
        "7:50 - Wash teeth",
        "7:55 - Fill bottle",
        "8:00 - Have a good day!"
    ];
}

[InstanceName("Night Protocol")]
[CanClientOpen(false)]
public class WeekdayNightProtocol : RoutineProtocol
{
    public override string[] Messages => [
        "Wash teeth",
        "Wear pyjama",
        "Prepare schoolbag",
        "Read a chapter",
        "Good night!"
    ];
}

[InstanceName("Morning Protocol")]
[CanClientOpen(false)]
public class WeekendMorningProtocol : RoutineProtocol
{
    public override string[] Messages => [
        "Wash face",
        "Shower",
        "Dress up",
        "Breakfast",
        "Wash teeth"
    ];
}

[InstanceName("Night Protocol")]
[CanClientOpen(false)]
public class WeekendNightProtocol : RoutineProtocol
{
    public override string[] Messages => [
        "Wash teeth",
        "Wear pyjama",
        "Read a chapter",
        "Good night!"
    ];
}