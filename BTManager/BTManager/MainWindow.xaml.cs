using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using InTheHand.Net.Sockets;
using System.Diagnostics;
using InTheHand.Net.Bluetooth;
using InTheHand.Net;
using System.Collections.Generic;
using BTManager.Models;

namespace BTManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BluetoothClient _bluetooth;
        BluetoothRadio radio;
        public EventHandler<BluetoothWin32AuthenticationEventArgs> handler;
        BluetoothWin32Authentication auth;
        public const string Pin = "456123";
        List<BluetoothDevice> devices;
        public MainWindow()
        {
            InitializeComponent();
            devices = new List<BluetoothDevice>();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            handler = new EventHandler<BluetoothWin32AuthenticationEventArgs>(HandleRequests);
            auth = new BluetoothWin32Authentication(handler);
            InitBluetooth();
        }

        private void Switch_Checked(object sender, RoutedEventArgs e)
        {
            bool? isChecked = toggleSwitch.IsChecked;

            // This will get the current WORKING directory (i.e. \bin\Debug)
            string workingDirectory = Environment.CurrentDirectory;
            // or: Directory.GetCurrentDirectory() gives the same result

            // This will get the current PROJECT directory
            string filePath = Directory.GetParent(workingDirectory).Parent.FullName + "\\Resources\\bluetooth.ps1";

            string toggle = "Off";
            if (isChecked != null)
            {
                toggle = isChecked.Value ? "On" : "Off";
            }
            string cmd = filePath + " -BluetoothStatus " + toggle;

            var response = RunScript(cmd);
            if (response && _bluetooth == null)
            {
                InitBluetooth();
            }
        }

        private void GetAllDevices()
        {
            devices.Clear();
            var allDevices = _bluetooth.DiscoverDevices();

            for (int i = 0; i < allDevices.Length; i++)
            {
                devices.Add(new BluetoothDevice(allDevices[i]));
            }
            selectedDevice.ItemsSource = devices;
        }

        private void InitBluetooth()
        {
            try
            {
                _bluetooth              = new BluetoothClient();
                radio                   = BluetoothRadio.PrimaryRadio;
                discoverableName.Text   = radio.Name;
                radio.Mode              = RadioMode.Connectable;
                var serviceClass        = BluetoothService.SerialPort;

                GetAllDevices();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }



        public static void PairWithDevice(BluetoothAddress address, String passCode)
        {
            //Set the pin for device.
            //BluetoothSecurity.SetPin(address, Pin);

            //Pair with device.
            bool ret = BluetoothSecurity.PairRequest(address, Pin);
        }

        public static void RemovePairedDevice(BluetoothAddress address)
        {
            //Pair with device.
            bool ret = BluetoothSecurity.RemoveDevice(address);
        }
        public static void HandleRequests(object that, BluetoothWin32AuthenticationEventArgs e)
        {
            int? passKey = e.NumberOrPasskey;
            if (passKey != null)
            {
                string msg = String.Format("Pairing Request : {0}", passKey);
                var response = MessageBox.Show(msg, "Request", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (response == MessageBoxResult.Yes)
                {
                    e.Confirm = true;
                }
            }
        }

        private void asf()
        {
            _bluetooth = new BluetoothClient();

            BluetoothDeviceInfo[] asf = new BluetoothDeviceInfo[10];
            _bluetooth.BeginDiscoverDevices(10, true, false, false, false, new AsyncCallback(ProcessCallBack), asf);
        }

        static void ProcessCallBack(IAsyncResult result)
        {
            var hostName = result.AsyncState;
        }

        private bool checkBluetooth()
        {
            radio = BluetoothRadio.PrimaryRadio;
            if (radio == null)
            {
                return false;
            }
            if (radio.Mode == RadioMode.PowerOff)
            {
                return false;
            }
            radio.Mode = RadioMode.Discoverable;
            return true;
        }

        private bool RunScript(string scriptText)
        {
            try
            {
                string unstrictCommand = "set-executionpolicy unrestricted";

                // create Powershell runspace
                Runspace runspace = RunspaceFactory.CreateRunspace();

                // open it
                runspace.Open();

                // create a pipeline and feed it the script text
                Pipeline pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(unstrictCommand);
                pipeline.Commands.AddScript(scriptText);

                // add an extra command to transform the script output objects into nicely formatted strings
                // remove this line to get the actual objects that the script returns. For example, the script
                // "Get-Process" returns a collection of System.Diagnostics.Process instances.
                pipeline.Commands.Add("Out-String");

                // execute the script
                Collection<PSObject> results = pipeline.Invoke();

                // close the runspace
                runspace.Close();

                // convert the script result into a single string
                StringBuilder stringBuilder = new StringBuilder();
                foreach (PSObject obj in results)
                {
                    stringBuilder.AppendLine(obj.ToString());
                }

                //return stringBuilder.ToString();
                if (results != null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return false;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = selectedDevice.SelectedItems[0] as BluetoothDevice;
            if (selectedItem != null)
            {
                _bluetooth.BeginConnect(selectedItem.DeviceAddress, BluetoothService.SerialPort, new AsyncCallback(BluetoothClientConnectCallback), null);
            }
            else
            {
                MessageBox.Show("No device is selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = selectedDevice.SelectedItems[0] as BluetoothDevice;
            if (selectedItem != null)
            {
            }
            else
            {
                MessageBox.Show("No device is selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BluetoothClientConnectCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted)
            {
                //Dispatcher.BeginInvoke((Action)(() =>
                //{
                //    GetAllDevices();
                //}));
            }
        }
    }
}
