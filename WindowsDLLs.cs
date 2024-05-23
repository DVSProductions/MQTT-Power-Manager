using System.Runtime.InteropServices;

namespace MQTT_Power_Manager;
internal static partial class WindowsDLLs {
	[LibraryImport("powrprof.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool SetSuspendState([MarshalAs(UnmanagedType.Bool)] bool hibernate, [MarshalAs(UnmanagedType.Bool)] bool force, [MarshalAs(UnmanagedType.Bool)] bool WakeupEventsDisabled);
}
