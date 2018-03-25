using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

class AzureIoTHub
{
    private static void CreateClient()
    {
        if (deviceClient == null)
        {
            // Create Azure IoT Hub client from embedded connection string (humidity)
            deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Http1);
        }
    }

    private static void CreateClient2()
    {
        if (deviceClient2 == null)
        {
            // Create Azure IoT Hub client from embedded connection string (temperature)
            deviceClient2 = DeviceClient.CreateFromConnectionString(deviceConnectionString2, TransportType.Http1);

        }
    }

    static DeviceClient deviceClient = null;
    static DeviceClient deviceClient2 = null;

    const string deviceConnectionString = "{Humidity Connection String}";
    const string deviceConnectionString2 = "{Temperature Connection String}";


    public static async Task SendDeviceToCloudMessageAsync(int humidity)
    {
        // Creates an Azure IoT Hub client for the humidity sensor
        CreateClient();

        // Constructing the message to be sent to IoT Hub
        var str = string.Format("{{\"deviceId\":\"humidity\",\"messageId\":1,\"value\":\"{0}%\"}}", humidity.ToString());
        var message = new Message(Encoding.ASCII.GetBytes(str));

        // Sending the message to IoT Hub using the client connection
        await deviceClient.SendEventAsync(message);


    }

    public static async Task SendDeviceToCloudMessageAsync2(double temp)
    {
        // Creates an Azure IoT Hub client for the temperature sensor
        CreateClient2();

        // Constructing the message to be sent to IoT Hub
        var str = string.Format("{{\"deviceId\":\"temperature\",\"messageId\":1,\"value\":\"{0}°C\"}}", temp.ToString().Substring(0,4));
        var message = new Message(Encoding.ASCII.GetBytes(str));

        // Sending the message to IoT Hub using the client connection
        await deviceClient2.SendEventAsync(message);
    }
}
