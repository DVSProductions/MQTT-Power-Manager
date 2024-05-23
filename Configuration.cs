using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MQTT_Power_Manager;
[JsonSerializable(typeof(Configuration))]
public class Configuration : INotifyPropertyChanged {
	private PcState state = PcState.Running;
	private string mQTTServer = "192.168.178.69";
	private int mQTTPort = 1883;
	private string? mQTTUser;
	private string? mQTTPassword;
	private string mQTTTopic = $"{App.MQTTTopic}/{Environment.MachineName}";
	private int shutdownTimeout = 60;
	private string mQTTServerConnectionState = "Disconnected";
	private bool startWithWindows = false;
	private bool trayIcon = true;
	private PcState[] allowedActions = [PcState.Hibernating, PcState.Sleeping, PcState.Poweroff, PcState.Reboot];

	#region Internal State
	[JsonIgnore]
	public PcState State { get => state; set { state = value; RPC(nameof(State)); } }
	[JsonIgnore]
	public string MQTTServerConnectionState { get => mQTTServerConnectionState; set { mQTTServerConnectionState = value; RPC(nameof(MQTTServerConnectionState)); } }
	[JsonIgnore]
	public bool StartWithWindows { get => startWithWindows; set { startWithWindows = value; RPC(nameof(StartWithWindows)); } }
	#endregion
	public bool TrayIcon { get => trayIcon; set { trayIcon = value; RPC(nameof(TrayIcon)); } }
	public string MQTTServer { get => mQTTServer; set { mQTTServer = value; RPC(nameof(MQTTServer)); } }
	public int MQTTPort { get => mQTTPort; set { mQTTPort = value; RPC(nameof(MQTTPort)); } }
	public string? MQTTUser { get => mQTTUser; set { mQTTUser = string.IsNullOrWhiteSpace(value) ? null : value; RPC(nameof(MQTTUser)); } }
	public string? MQTTPassword { get => mQTTPassword; set { mQTTPassword = string.IsNullOrWhiteSpace(value) ? null : value; RPC(nameof(MQTTPassword)); } }
	public string MQTTTopic { get => mQTTTopic; set { mQTTTopic = value; RPC(nameof(MQTTTopic)); } }
	public int ShutdownTimeout { get => shutdownTimeout; set { shutdownTimeout = value; RPC(nameof(ShutdownTimeout)); } }
	public PcState[] AllowedActions { get => allowedActions; set { allowedActions = value; RPC(nameof(AllowedActions)); } }
	public event PropertyChangedEventHandler? PropertyChanged;
	private void RPC(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
