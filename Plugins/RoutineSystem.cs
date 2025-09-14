using SDK;
using SDK.InstanceTools;

namespace Plugins;

[ShowOnClient(false)]
public class RoutineSystem : SystemInstance {

	public override void Open() {
		var morningTimeWeekdays = TimeSpan.FromHours(7) + TimeSpan.FromMinutes(20);
		var morningTimeWeekends = TimeSpan.FromHours(9) + TimeSpan.FromMinutes(00);
		var nightTime = TimeSpan.FromHours(23) + TimeSpan.FromMinutes(00);

		InstanceEvents.RegisterTimeEvent(morningTimeWeekdays, InstanceEvents.Days.Weekdays, () => Core.Open<MorningProtocol>());
		InstanceEvents.RegisterTimeEvent(morningTimeWeekends, InstanceEvents.Days.Weekend, () => Core.Open<MorningProtocol>());
		InstanceEvents.RegisterTimeEvent(nightTime, InstanceEvents.Days.All, () => Core.Open<NightProtocol>());
	}

}