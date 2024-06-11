using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace MQTT_Power_Manager;
public partial class MainWindow : Window {
	private readonly NotifyIcon _notifyIcon;
	private void HandleTrayIcon(object? sender, PropertyChangedEventArgs e) {
		if(e.PropertyName != nameof(Configuration.TrayIcon))
			return;
		_notifyIcon.Visible = Config.TrayIcon;
		SaveConfig();
	}
	private ContextMenuStrip CreateContextMenu() {
		var openItem = new ToolStripMenuItem("Open Configuration");
		openItem.Click += ShowWindow;
		var exitItem = new ToolStripMenuItem("Hide Icon");
		exitItem.Click += (_, __) => {
			Config.TrayIcon = false;
			SaveConfig(false);
		};
		var contextMenu = new ContextMenuStrip { Items = { openItem, exitItem } };
		return contextMenu;
	}
	private NotifyIcon SetupIcon() {
		var ret = new NotifyIcon {
			Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
			Visible = Config.TrayIcon,
			ContextMenuStrip = CreateContextMenu(),
			BalloonTipTitle = "MQTT Power Manager",
			BalloonTipText = "Control shutting down your PC remotely!"
		};
		ret.DoubleClick += ShowWindow;
		System.Windows.Application.Current.Exit += (_, __) => { ret.Visible = false; ret.Dispose(); };
		return ret;
	}
}
