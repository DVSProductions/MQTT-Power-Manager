using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace MQTT_Power_Manager;
public partial class MainWindow : Window {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "We need to keep the mutex alive")]
	private readonly Mutex mutex;
	private const string GUID = "a683c06d-0fc3-4df6-b2f0-7986709b1df9";
	private Mutex EnsureSigleInstance() {
		var mutex = new Mutex(true, GUID, out var createdNew);
		if(!createdNew) {
			using NamedPipeClientStream client = new(".", GUID, PipeDirection.Out, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Anonymous);
			try {
				client.Connect(200);
				Environment.Exit(0);
			}
			catch(TimeoutException) {
				MessageBox.Show("Another instance is already running", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Environment.Exit(1);
			}
		}
		else {
			Thread thread = new(SingleInstanceServer);
			thread.Start();
		}
		return mutex;
	}
	// Creates a PipeSecurity that allows users read/write access
	private void SingleInstanceServer() {
		while(true) {
			var ps = new PipeSecurity();
			// Allow Everyone read and write access to the pipe. 
			ps.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
			using var server=NamedPipeServerStreamAcl.Create(GUID, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous,256,256, ps);
			server.WaitForConnection();
			Dispatcher.Invoke(() => {
				Show();
				Focus();
			});
		}
	}
}
