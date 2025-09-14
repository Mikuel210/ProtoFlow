namespace SDK.InstanceTools;

public static class InstanceEvents {

	public class Event {

		public Func<Event, bool>? Trigger { get; init; }
		public Action Callback { get; init; }
		public DateTime LastEvent { get; set; }

		
		public void Invoke() {
			Callback();
			LastEvent = DateTime.Now;
		}
		
		public void Unregister() => Events.Remove(this);

	}
	public static List<Event> Events { get; } = new();
	
	public static void Tick() {
		foreach (Event @event in Events) {
			if (@event.Trigger == null) continue;
			if (!@event.Trigger(@event)) continue;

			@event.Invoke();
		}
	}
	
	#region Events
	
	[Flags] public enum Days {
		None      = 0,
		Sunday    = 1 << 0,
		Monday    = 1 << 1,
		Tuesday   = 1 << 2,
		Wednesday = 1 << 3,
		Thursday  = 1 << 4,
		Friday    = 1 << 5,
		Saturday  = 1 << 6,
 
		Weekdays  = Monday | Tuesday | Wednesday | Thursday | Friday,
		Weekend   = Saturday | Sunday,
		All       = Weekdays | Weekend
	}
	private static bool EvaluateDays(Days days) {
		switch (DateTime.Now.DayOfWeek) {
			case DayOfWeek.Monday: return days.HasFlag(Days.Monday);
			case DayOfWeek.Tuesday: return days.HasFlag(Days.Tuesday);
			case DayOfWeek.Wednesday: return days.HasFlag(Days.Wednesday);
			case DayOfWeek.Thursday: return days.HasFlag(Days.Thursday);
			case DayOfWeek.Friday: return days.HasFlag(Days.Friday);
			case DayOfWeek.Saturday: return days.HasFlag(Days.Saturday);
			case DayOfWeek.Sunday: return days.HasFlag(Days.Sunday);
			default: return false;
		}
	} 
	
	// Time
	public static Event RegisterTimeEvent(TimeSpan time, Days days, Action callback) {
		Event output = new() {
			Trigger = @event => {
				var now = DateTime.Now;
				bool isTime = now.TimeOfDay > time && @event.LastEvent.Day < now.Day;
				bool isDay = EvaluateDays(days);

				return isTime && isDay;
			},
			Callback = callback
		};
		
		Events.Add(output);
		return output;
	}
	public static Event RegisterIntervalEvent(DateTime start, TimeSpan interval, Action callback) {
		Event output = new() {
			Trigger = @event => DateTime.Now > start + interval && DateTime.Now - @event.LastEvent >= interval,
			Callback = callback
		};
		
		Events.Add(output);
		return output;
	}
	
	// Core
	public static Event RegisterInstanceOpenedEvent(Action callback) {
		Event output = new() {
			Callback = callback
		};

		Core.OnInstanceOpened += _ => output.Invoke();

		Events.Add(output);
		return output;
	}
	public static Event RegisterInstanceOpenedEvent(Type instanceType, Action callback) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to register an Instance Opened Event from a non-instance type");
		
		Event output = new() {
			Callback = callback
		};

		Core.OnInstanceOpened += type => {
			if (type == instanceType)
				output.Invoke();
		};

		Events.Add(output);
		return output;
	}
	public static Event RegisterInstanceOpenedEvent<T>(Action callback) where T : Instance =>
		RegisterInstanceOpenedEvent(typeof(T), callback);
	
	public static Event RegisterInstanceClosedEvent(Action callback) {
		Event output = new() {
			Callback = callback
		};

		Core.OnInstanceClosed += _ => output.Invoke();

		Events.Add(output);
		return output;
	}
	public static Event RegisterInstanceClosedEvent(Type instanceType, Action callback) {
		if (!instanceType.IsAssignableTo(typeof(Instance)))
			throw new ArgumentException("Attempted to register an Instance Opened Event from a non-instance type");
		
		Event output = new() {
			Callback = callback
		};

		Core.OnInstanceClosed += type => {
			if (type == instanceType)
				output.Invoke();
		};

		Events.Add(output);
		return output;
	}
	public static Event RegisterInstanceClosedEvent<T>(Action callback) where T : Instance =>
		RegisterInstanceClosedEvent(typeof(T), callback);
	
	#endregion

}