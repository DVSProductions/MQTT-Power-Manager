using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Win32.TaskScheduler;

namespace MQTT_Power_Manager;
public partial class MainWindow : Window {
	private const string TaskName = "MQTT Power Manager";
	private void HandleAutorun(object? sender, PropertyChangedEventArgs e) {
		if(e.PropertyName != nameof(Configuration.StartWithWindows))
			return;
		SetAutorunState(Config.StartWithWindows);
	}
	private void CreateAutorunTask() {
		var task = TaskService.Instance.FindTask(TaskName);
		if(task != null) {
			task.Definition.Actions.Clear();
			task.Definition.Actions.Add(new ExecAction(Environment.ProcessPath, "-service", Path.GetDirectoryName(Environment.ProcessPath)));
			task.RegisterChanges();
		}
		if(task == null) {//create a new autorun task
			var td = TaskService.Instance.NewTask();
			td.RegistrationInfo.Description = "Starts the MQTT power manager whenever you login";
			td.Principal.RunLevel = TaskRunLevel.Highest;
			td.Triggers.Add(new LogonTrigger());
			td.Actions.Add(new ExecAction(Environment.ProcessPath, "-service", Path.GetDirectoryName(Environment.ProcessPath)));
			TaskService.Instance.RootFolder.RegisterTaskDefinition(TaskName, td);
			td.Settings.Enabled = false;
		}
		task = TaskService.Instance.FindTask(TaskName);
		Config.StartWithWindows = task.Enabled;
	}

	private static void SetAutorunState(bool autorun) {
		var task = TaskService.Instance.FindTask(TaskName);
		if(task == null)
			return;
		task.Enabled = autorun;
	}
}
