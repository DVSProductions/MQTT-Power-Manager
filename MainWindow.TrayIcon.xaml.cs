using System.ComponentModel;
using System.Windows;

namespace MQTT_Power_Manager;
public partial class MainWindow : Window {
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
			SaveConfig();
		};
		var contextMenu = new ContextMenuStrip { Items = { openItem, exitItem } };
		return contextMenu;
	}
}
