using System.Windows;
using System.Text.Json;
using System.IO;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using MessageBox = System.Windows.MessageBox;
using CheckBox = System.Windows.Controls.CheckBox;
using System.Security.Principal;
using static System.Net.Mime.MediaTypeNames;

namespace MQTT_Power_Manager;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
	private const string SaveFilePath = "config.json";
	private readonly NotifyIcon _notifyIcon;


	IMqttClient mqttclient;
	DateTime lastSleep = DateTime.MinValue;
	bool hasHibernatedOrSleeped = false;
	public Configuration Config { get; set; }

	public MainWindow() {
#if !DEBUG
		try {
#endif
		mutex = EnsureSigleInstance();
		//ensure admin
		{
			var wi = WindowsIdentity.GetCurrent();
			var wp = new WindowsPrincipal(wi);
			if(!wp.IsInRole(WindowsBuiltInRole.Administrator)) {
				ProcessStartInfo psi = new ProcessStartInfo {
					FileName = Environment.ProcessPath,
					WorkingDirectory = Path.GetDirectoryName(Environment.ProcessPath),
					Verb = "runas",
					UseShellExecute = true
				};
				Process.Start(psi);
				Environment.Exit(0);
			}
		}


		DataContext = this;
		//load configuration
		if(!File.Exists(SaveFilePath)) {
			Config = new Configuration();
			SaveConfig();
		}
		else {
			var test = LoadConfig();
			if(test == null) {
				Config = new Configuration();
				SaveConfig();
			}
			else
				Config = test;
		}
		CreateAutorunTask();
		Config.PropertyChanged += EnableSaveButton;
		Config.PropertyChanged += HandleTrayIcon;
		Config.PropertyChanged += HandleAutorun;
		InitializeComponent();
		_notifyIcon = new NotifyIcon {
			Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
			Visible = Config.TrayIcon,
			ContextMenuStrip = CreateContextMenu()
		};
		_notifyIcon.DoubleClick += ShowWindow;
		System.Windows.Application.Current.Exit += (_, __) => _notifyIcon.Dispose();
		//get startup parameters
		var cmd = Environment.GetCommandLineArgs();
		if(cmd.Length > 1) {
			if(cmd[1] == "-service")
				Hide();
		}
		Closing += (_, e) => {
			e.Cancel = true;
			if(SaveButton.IsEnabled) {
				switch(MessageBox.Show("You hace unsaved changes.\nDo you wish to save them now?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning)) {
					case MessageBoxResult.Yes:
						SaveConfig();
						break;
					case MessageBoxResult.No:
						var newcfg = LoadConfig() ?? new Configuration();
						Config = newcfg;
						Hide();
						break;
					case MessageBoxResult.Cancel:
						return;
				}
			};
			Hide();
		};

		CreateActions();

		MQTTLoop();
#if !DEBUG
		}
		catch(Exception e) {
			MessageBox.Show(e.Message + "\n" + e.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			Environment.Exit(1);
		}

#endif
		static Configuration? LoadConfig() => JsonSerializer.Deserialize<Configuration>(File.ReadAllText(SaveFilePath));
	}

	private void EnableSaveButton(object? sender, PropertyChangedEventArgs e) {
		switch(e.PropertyName) {
			case nameof(Configuration.State):
			case nameof(Configuration.MQTTServerConnectionState):
			case nameof(Configuration.TrayIcon):
			case nameof(Configuration.StartWithWindows):
				break;
			default:
				SaveButton.IsEnabled = true;
				break;
		}
	}

	private void ShowWindow(object? _, EventArgs __) {
		Show();
		Focus();
	}

	private void CreateActions() {
		foreach(var a in Enum.GetValues<PcState>().Where(x => x is not PcState.Running and not PcState.Error)) {
			var cb = new CheckBox() { Content = a.ToString(), IsChecked = Config.AllowedActions.Contains(a) };
			cb.Checked += (_, __) => {
				if(!Config.AllowedActions.Contains(a))
					Config.AllowedActions = Config.AllowedActions.Append(a).ToArray();
			};
			cb.Unchecked += (_, __) => {
				if(Config.AllowedActions.Contains(a))
					Config.AllowedActions = Config.AllowedActions.Where(x => x != a).ToArray();
			};
			spActions.Children.Add(cb);
		}
	}


	public Func<Task> PublishCurrentState;
	async void MQTTLoop() {
		await ConnectMQTT(true);
		var messageBuilder = new MqttApplicationMessageBuilder()
			.WithTopic(Config.MQTTTopic)
			.WithPayload(Config.State.ToString());
		while(!await ConnectMQTT())
			Thread.Sleep(1000);
		await mqttclient.PublishAsync(messageBuilder.Build());
		PublishCurrentState = async () => {
			messageBuilder.WithPayload(Config.State.ToString());
			await mqttclient.PublishAsync(messageBuilder.Build());
		};
		await PublishCurrentState();

		var subscribeoptions = new MqttClientSubscribeOptionsBuilder()
			.WithTopicFilter(Config.MQTTTopic)
			.Build();
		await mqttclient.SubscribeAsync(subscribeoptions);
		Thread t = new(() => {
			while(true) {
				Thread.Sleep(TimeSpan.FromSeconds(60));
				if(hasHibernatedOrSleeped) {
					if(DateTime.Now - lastSleep > TimeSpan.FromSeconds(10)) { //expect at least a 10 second sleep before sending the running status
						hasHibernatedOrSleeped = false;
						Config.State = PcState.Running;
					}
				}
				if(!mqttclient.IsConnected)
					while(!ConnectMQTT().Result)
						Thread.Sleep(1000);
				PublishCurrentState().Wait();
			}
		});
		t.Start();
	}

	MqttClientOptions options;
	object locker = new();
	SemaphoreSlim sem = new(1, 1);
	private async Task<bool> ConnectMQTT(bool reconfigure = false) {
		await sem.WaitAsync();
		try {
			if(reconfigure) {
				var mqttFactory = new MqttFactory();
				if(mqttclient != null) {
					mqttclient.DisconnectedAsync -= disconnector;
					mqttclient.DisconnectAsync().Wait();
					mqttclient.Dispose();
				}
				mqttclient = mqttFactory.CreateMqttClient();
				{
					var builder = new MqttClientOptionsBuilder()
					.WithTcpServer(Config.MQTTServer, Config.MQTTPort)
					.WithClientId("MQTT_PM_" + Environment.MachineName);
					if(Config.MQTTUser != null && Config.MQTTPassword != null)
						builder = builder.WithCredentials(Config.MQTTUser, Config.MQTTPassword);
					options = builder.Build();
				}
				mqttclient.DisconnectedAsync += disconnector;
			}
			if(mqttclient == null)
				return false;
			else if(mqttclient.IsConnected)
				return true;
			Config.MQTTServerConnectionState = "Connecting";
			try {
				switch((await mqttclient.ConnectAsync(options)).ResultCode) {
					case MqttClientConnectResultCode.Success:
						Config.MQTTServerConnectionState = "Connected!";
						mqttclient.ApplicationMessageReceivedAsync += MessageReceived;
						return true;
					case MqttClientConnectResultCode.BadUserNameOrPassword:
						Config.MQTTServerConnectionState = "Bad Username or Password!";
						await Task.Delay(1000);
						return false;
					case MqttClientConnectResultCode.NotAuthorized:
						Config.MQTTServerConnectionState = "Not Authorized!";
						await Task.Delay(1000);
						return false;
					case MqttClientConnectResultCode.UnspecifiedError:
						Config.MQTTServerConnectionState = "Unspecified Error!";
						await Task.Delay(1000);
						return false;
					case MqttClientConnectResultCode.ServerUnavailable:
						Config.MQTTServerConnectionState = "Server Unavailable!";
						await Task.Delay(1000);
						return false;
					default:
						Config.MQTTServerConnectionState = "Unknown Error!";
						await Task.Delay(1000);
						return false;
				}
			}
			catch(MQTTnet.Exceptions.MqttCommunicationTimedOutException e) {
				Config.MQTTServerConnectionState = "Timed out!";
				await Task.Delay(1000);
				return false;
			}
			catch(ObjectDisposedException e) {
				Config.MQTTServerConnectionState = "Object Disposed!";
				return false;
			}
		}
		finally {
			sem.Release();
		}

		Task disconnector(MqttClientDisconnectedEventArgs _) { Config.MQTTServerConnectionState = "Disconnected!"; return Task.CompletedTask; };
	}

	private async Task MessageReceived(MqttApplicationMessageReceivedEventArgs args) {
		var payload = Encoding.ASCII.GetString(args.ApplicationMessage.PayloadSegment);
		if(!Enum.TryParse<PcState>(payload, out var state)) {
			await PublishCurrentState();//inform the sender that we are not accepting the state
			return;
		}
		if(state == PcState.Running || state == Config.State)
			return;
		if(!Config.AllowedActions.Contains(state)) {
			await PublishCurrentState();//inform the sender that we are not accepting the state
			return;
		}
		Config.State = state;
		HandlePowerStates();
	}

	private void SaveConfig() {
		File.WriteAllText(SaveFilePath, JsonSerializer.Serialize(Config));
		File.Encrypt(SaveFilePath);//ensure that nobody can read the passwords stored in the configuration
	}

	private void Button_Click(object sender, RoutedEventArgs e) {
		SaveConfig();
		ConnectMQTT(true);
		SaveButton.IsEnabled = false;
	}
	private void HandlePowerStates() {
		switch(Config.State) {
			case PcState.Hibernating:
				lastSleep = DateTime.Now;
				hasHibernatedOrSleeped = true;
				//hibernate
				Process.Start("shutdown", "/h");
				break;
			case PcState.Sleeping:
				lastSleep = DateTime.Now;
				hasHibernatedOrSleeped = true;
				//sleep
				WindowsDLLs.SetSuspendState(false, false, false);
				break;
			case PcState.Poweroff:
				//shutdown
				Dispatcher.BeginInvoke(() => btShutdownAbort.IsEnabled = true);
				Process.Start("shutdown", $"/s /t {Config.ShutdownTimeout} /c \"MQTT Shutdown request\nShutdown in {Config.ShutdownTimeout} Seconds.\" /d p:0:0");
				break;
			case PcState.PoweroffForced:
				//shutdown forced
				Dispatcher.BeginInvoke(() => btShutdownAbort.IsEnabled = true);
				Process.Start("shutdown", $"/s /f /t {Config.ShutdownTimeout} /c \"MQTT Forced Shutdown request\nForced Shutdown in {Config.ShutdownTimeout} Seconds.\" /d p:0:0");
				break;
			case PcState.Reboot:
				//reboot
				Dispatcher.BeginInvoke(() => btShutdownAbort.IsEnabled = true);
				Process.Start("shutdown", $"/r /soft /t {Config.ShutdownTimeout} /c \"MQTT Reboot request\nReboot in {Config.ShutdownTimeout} Seconds.\" /d p:0:0");
				break;
			case PcState.RebootForced:
				//reboot forced
				Dispatcher.BeginInvoke(() => btShutdownAbort.IsEnabled = true);
				Process.Start("shutdown", $"/r /f /t {Config.ShutdownTimeout} /c \"MQTT Forced Reboot request.\nForced Reboot in {Config.ShutdownTimeout} Seconds.\" /d p:0:0");
				break;
		}
	}

	private void btShutdownAbort_Click(object sender, RoutedEventArgs e) {
		Process.Start("shutdown", "/a");
		btShutdownAbort.IsEnabled = false;
		Config.State = PcState.Running;
		PublishCurrentState();
	}
}