using Meteo;
using MeteoTask;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit.Sdk;

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

            string? fileData = ReadDataFromFile();
            if (fileData == null)
                return;

            try
            {
                if (fileData.Length > 0)
                    meteoList = JsonSerializer.Deserialize<List<MeteoInfo>>(fileData);
            }
            catch
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

        private static string? ReadDataFromFile()
        {
            string? fileData = null;

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    char[] readBuffer = new char[1024];
                    StringBuilder builder = new StringBuilder();

                    while (sr.Read(readBuffer, 0, readBuffer.Length) > 0)
                        builder.Append(readBuffer);

                    fileData = builder.ToString();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reading from file error: {0}", ex.Message);
            }

            return fileData?.Substring(0, fileData.IndexOf(']') + 1);
        }

        private static void WriteDataToFile(string data)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (fs.Length > 0)
                    {
                        string resultData = ',' + data.Substring(1, data.Length - 1);
                        fs.Seek(-3, SeekOrigin.End);                                    //'\r' '\n' ']'                                
                        sw.Write(resultData);
                    }
                    else
                        sw.Write(data);
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to file error: {0}", ex.Message);
            }
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string receivedData = ((SerialPort)sender).ReadExisting();

            buffer.Append(receivedData);
            string bufferData = buffer.ToString();

            if (!bufferData.StartsWith('$'))
                buffer.Remove(0, bufferData.IndexOf('$') + 1);

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

                bufferData = buffer.ToString();
                buffer.Remove(bufferData.IndexOf(message.Value), message.Value.Length);
            }

            if (messages.Count > 0)
            {
                string json = JsonSerializer.Serialize<List<MeteoInfo>>(meteoList.GetRange(meteoList.Count - messages.Count, messages.Count),
                                                                        new JsonSerializerOptions { WriteIndented = true });
                WriteDataToFile(json);
            }
        }
    }
}