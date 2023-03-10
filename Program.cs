using Meteo;
using MeteoTask;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Meteo
{
    internal class Program
    {
        private const string filePath = @".\meteo.json";
        private static List<MeteoInfo> meteoList = new List<MeteoInfo>();
        private static StringBuilder buffer = new StringBuilder();

        public static void Main(string[] args)
        {
            Console.Write("Enter serial port number: ");
            string portName = "COM" + Console.ReadLine();
            Console.Clear();

            if (!SerialPort.GetPortNames().Any(x => x == portName))
            {
                Console.WriteLine("The port \"{0}\" doesn't exist.", portName);
                return;
            }

            if (!File.Exists(filePath))
                File.Create(filePath);

            string? fileData = ReadFileData();
            if (fileData == null)
                return;

            try
            {
                if (fileData.StartsWith("[") && fileData.EndsWith("]"))
                    meteoList = JsonSerializer.Deserialize<List<MeteoInfo>>(fileData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The file contains incorrect data.");
                return;
            }

            SerialPort serialPort = new SerialPort(portName, 2400, Parity.None, 8, StopBits.One);
            serialPort.ReadTimeout = 100;
            serialPort.DataReceived += DataReceivedHandler;

            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Port opening error: {0}", ex.Message);
                return;
            }

            Console.WriteLine("The data is written to a file in the program directory.");
            Console.Write("Receiving messages from serial port...\nPress ESC to end... ");
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }

            serialPort.Close();
        }

        private static string? ReadFileData()
        {
            string? fileData = null;
            try
            {
                fileData = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reading from file error: {0}", ex.Message);
            }

            return fileData;
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string receivedData = ((SerialPort)sender).ReadExisting();
            buffer.Append(receivedData);

            var messages = Regex.Matches(buffer.ToString(), @"\$\d+\.\d+,\d+\.\d+\r\n", RegexOptions.Compiled);

            foreach (Match message in messages)
            {
                var numbers = Regex.Matches(message.Value, @"\d+\.\d+", RegexOptions.Compiled);

                meteoList.Add(new MeteoInfo
                {
                    Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    SensorName = "WMT700",
                    WindSpeed = float.Parse(numbers[0].Value, CultureInfo.InvariantCulture.NumberFormat),
                    WindDirection = float.Parse(numbers[1].Value, CultureInfo.InvariantCulture.NumberFormat)
                });

                string bufferData = buffer.ToString();
                buffer.Remove(bufferData.IndexOf(message.Value), message.Value.Length);
            }

            string json = JsonSerializer.Serialize<List<MeteoInfo>>(meteoList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}