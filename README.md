# MQTT Power Manager
This is a simple power manager that uses MQTT to control the power state of your Windows PC.


## Installation
1. Download the latest release from the [releases page]
2. Extract the zip file to a folder of your choice
3. Run the setup.exe 
4. (optional) Check the start with Windows option

## Usage
Enter your MQTT broker IP and port as well as the credentials if required.
You can optionally change the topic, but it is automatically generated based on the name of your PC.
Then hit, save Configuration. The status in the top should now say "Connected".
If you want to restrict the control over your pc you can uncheck the Allowed Actions on the right.
The Shutdown timeout is the time in between a command beeing issued and the actual shutdown. The user will be notified about the shutdown and can abort it.
If a shutdown is pending, the "Abort Shutdown" button will be available. If you click it, the shutdown will be aborted.

## MQTT
The value of the topic indicates the current state of the PC.
If the value is "Running" the PC is on. This will be updated every 60 seconds. (in case the pc crashes you could see this as a timeout)
If the value is changed externally, the new state will be applied.(if the requested state is not allowed, it will be overwritten again)
If a command is allowed, the power manager will execute the action.
The following commands are supported:
- "Poweroff" - Shuts down the PC
- "Reboot" - Reboots the PC
- "Sleep" - Puts the PC to sleep
- "Hibernate" - Hibernates the PC
- "PoweroffForce" - (Disabled by default) Shuts down the PC without letting applications save their state
- "RebootForce" - (Disabled by default) Reboots the PC without letting applications save their state

## Tray icon
The tray icon is an optional feature, which allows you to open the power manager window and see the current state of the PC.
This can be hidden by unchecking the "Show Tray Icon" option.

PS: There is no way to close the power manager without killing the process. This is intended, as the power manager should always be running. Otherwise it would be pointless.

