using MeteoTask;
using System.Globalization;
using System.IO.Ports;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.Write("Enter serial port number: ");
string portName = "COM" + Console.ReadLine();
Console.Clear();

if (!SerialPort.GetPortNames().Any(x => x == portName))
{
    Console.WriteLine("A serial port with this number doesn't exist.");
    return;
}

SerialPort serialPort = new SerialPort(portName, 2400, Parity.None, 8, StopBits.One);
serialPort.ReadTimeout = 10;
serialPort.DataReceived += (sender, args) =>
{
    string currentTime = DateTime.Now.ToLongTimeString();
    string receivedData = serialPort.ReadExisting();

    if (Regex.IsMatch(receivedData, @"^\$\d+\.\d+,\d+\.\d+\r\n"))
    {
        var numbers = Regex.Matches(receivedData, @"\d+\.\d+");

        MeteoInfo meteo = new MeteoInfo()
        {
            Time = currentTime,
            SensorName = "WMT700",
            WindSpeed = float.Parse(numbers[0].Value, CultureInfo.InvariantCulture.NumberFormat),
            WindDirection = float.Parse(numbers[1].Value, CultureInfo.InvariantCulture.NumberFormat)
        };

        string filePath = @".\meteo.json";
        string json = JsonSerializer.Serialize(meteo);
        File.AppendAllText(filePath, json);

        Console.SetCursorPosition(0, 1);
        Console.WriteLine("{0}: Received message from {1} is written to the file \"{2}\"", meteo.Time, meteo.SensorName, filePath);
    }
};

serialPort.Open();

Console.SetCursorPosition(0, 0);
Console.Write("Press ESC to stop program... ");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }

serialPort.Close();