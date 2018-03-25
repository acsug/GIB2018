using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Global_Integration_Bootcamp_IoT
{
    public sealed partial class MainPage : Page
    {
        // Initialising variables used by humidity and temperature sensors.
        Random random = new Random();
        double temp;
        int humidity;

        public MainPage()
        {
            this.InitializeComponent();

            // Continuously runs in the background. 
            // Updates the values of the simulated devices (humidity and temperature) that are seen on screen.
            Task.Run(
                 async () => {
                     while (true)
                     {
                         await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => {

                         humidity = random.Next(70,90); // Simulates humidity value between 70 and 90
                         humiditySensor.Text = String.Format("Humidity Sensor: {0}%", humidity.ToString()); // Changes the humidity value on screen.

                         temp = random.NextDouble() + 22.0; // Simulates temperature value by adding a floating-point number between 0 and 1 to 22.0.
                         temperatureSensor.Text = String.Format("Temperature Sensor: {0}°C", temp.ToString().Substring(0, 4)); // Changes the temperature value on screen.
                         });

                         await Task.Delay(2000);
                     }
                 }
                 );
        }

        // Triggered when the Humidity sensor's Send button is clicked
        private void humidity_Click(object sender, RoutedEventArgs e)
        {
            // Calls the SendDeviceToCloudMessageAsync method inside AzureIoTHub
            // Sends current humidity value to IoT Hub
            Task.Run(async () => { await AzureIoTHub.SendDeviceToCloudMessageAsync(humidity); });
        }

        // Triggered when the Temperature sensor's Send button is clicked
        private void temperature_Click(object sender, RoutedEventArgs e)
        {
            // Calls the SendDeviceToCloudMessageAsync2 method inside AzureIoTHub
            // Sends current temperature value to IoT Hub
            Task.Run(async () => { await AzureIoTHub.SendDeviceToCloudMessageAsync2(temp); });
        }

    }
}
