using System.Configuration;
using System.Data;
using System.Windows;

namespace MQTT_Power_Manager;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application {
	public static string MQTTTopic = "pcstate";
}

