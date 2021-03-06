﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using NetworkPacketConfigToJson.Models;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using HeaterListener.Models;
using System.Globalization;

namespace HeaterListener
{
    class Program
    {
        private static TcpClient TcpClient = new TcpClient();
        private static StreamReader StreamReader;
        private static ConfigModel Config;
        private static List<NetworkPacketModel> NetworkPacketConfig;
        private const string CONFIG_FILE_NAME = "config.json";
        private const string PACKET_CONFIG_FILE_NAME = "network_packet_config.json";
        private const string DATA_FILE_NAME = "data.json";

        static void Main(string[] args)
        {
            if (File.Exists("config.json"))
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(CONFIG_FILE_NAME));
                }
                catch (Exception e)
                {
                    Console.WriteLine(CONFIG_FILE_NAME + " konnte nicht eingelesen werden\n" + e.ToString());
                }
            }
            else
            {
                var input = ConsoleHelper.AskUserForIPAndPort();
                Config = new ConfigModel { IpAddress = input.Item1.ToString(), Port = input.Item2 };

                File.WriteAllText(CONFIG_FILE_NAME, JsonConvert.SerializeObject(Config, Formatting.Indented));
                Console.WriteLine(CONFIG_FILE_NAME + " angelegt. Bitte anpassen und die Anwendung neu starten.");
                ConsoleHelper.ExitDialog();
            }

            // check package configuration file
            if (!File.Exists(PACKET_CONFIG_FILE_NAME))
            {
                Console.WriteLine("Bitte " + PACKET_CONFIG_FILE_NAME + "ins Verzeichnis der Anwendung legen und Anwendung neu starten.");
                ConsoleHelper.ExitDialog();
            }

            // read package configuration file
            try
            {
                NetworkPacketConfig = JsonConvert.DeserializeObject<List<NetworkPacketModel>>(File.ReadAllText(PACKET_CONFIG_FILE_NAME), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e);
                Console.WriteLine(PACKET_CONFIG_FILE_NAME + " lässt sich nicht einlesen. Bitte prüfen Sie die Datei und starten die Anwendung neu.");
                ConsoleHelper.ExitDialog();
            }

            // check for faulted ipaddress
            if (!IPAddress.TryParse(Config.IpAddress, out IPAddress ip))
            {
                Console.WriteLine($"Format der IP-Adresse in {CONFIG_FILE_NAME} falsch. Bitte anpassen und neustarten");
                ConsoleHelper.ExitDialog();
            }

            // try to register for the port
            try
            {
                TcpClient = new TcpClient(Config.IpAddress, Config.Port);
                StreamReader = new StreamReader(TcpClient.GetStream()); ;
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e);
                Console.WriteLine("Fehler beim Registrieren des Listeners. Eine andere Anwendung hört bereits auf Port {0}.", Config.Port);
                ConsoleHelper.ExitDialog();
            }

            Console.WriteLine("Paket-Empfang gestartet...");
            Task.Run(() =>
            {
                var previousInput = "";
                while (true)
                {
                    try
                    {
                        var message = StreamReader.ReadLine();

                        if (message != previousInput)
                        {
                            previousInput = message;
                            if (ProcessData(message))
                                Console.WriteLine("\nVerarbeitet: \n" + message);
                            else previousInput = "";
                        }
                        // else
                        // {
                        //     Console.WriteLine("\nNur Empfangen (keine Veränderung der Daten): \n" + message);
                        // }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Folgender Fehler ist beim Lesen ankommender Pakete von {Config.IpAddress} aufgetreten:");
                        Console.WriteLine(e);
                        Console.WriteLine();
                    }
                }
            });

            if (GetLocalIPAddress() == Config.IpAddress)
                SendTestPackets(Config.Port);

            // keep app running
            while (true)
            {
                Console.ReadKey();
            }
        }

        private static bool ProcessData(string input)
        {
            if (input.StartsWith("pm "))
            {
                var data = new List<CapturedDataModel>();

                input = input.Substring("pm ".Length, input.Length - "pm ".Length);
                var split = input.Split(" ");

                for (int i = 0; i < split.Length; i++)
                {
                    var filtered = NetworkPacketConfig.Where(p => p.Id == i);
                    if (filtered.Count() != 0)
                    {
                        if (filtered.First().GetType() == typeof(AnalogNetworkPacketModel))
                        {
                            var analog = filtered.First() as AnalogNetworkPacketModel;

                            var c = new CapturedDataModel() { Id = i, Name = analog.Name, Unit = analog.Unit };
                            double v;
                            if (double.TryParse(split[i].Replace(".", ","), out v))
                                c.Value = v.ToString();
                            else
                            {
                                Console.WriteLine("\nPaket ungültig. Double erwartet jedoch anderen Wert empfangen:\ntryparse: " + split[i] + "\n" + input);
                                return false;
                            }
                            data.Add(c);
                        }
                        else
                        {
                            var digital = filtered.First() as DigitalNetworkPacketModel;

                            // go through bits
                            int o;
                            if (int.TryParse(split[i], NumberStyles.HexNumber, null, out o))
                            {
                                int[] bits = Convert.ToString(o, 2).PadLeft(digital.Bits.Last().Bit + 1, '0')
                                             .Select(c => int.Parse(c.ToString()))
                                             .ToArray();

                                for (int b = 0; b < digital.Bits.Count; b++)
                                    data.Add(new CapturedDataModel() { Id = i, Value = bits[digital.Bits[b].Bit].ToString(), Name = digital.Bits[b].Name, Unit = "bool" });
                            }
                            else
                            {
                                Console.WriteLine("\nPaket ungültig. Hexadezimalnummer erwartet jedoch anderen Wert empfangen:\ntryparse: " + split[i] + "\n" + input);
                                return false;
                            }
                        }
                    }
                }

                // add timestamp
                data.Add(new CapturedDataModel() { Id = -1, Name = "timestamp", Unit = "", Value = DateTime.Now.ToString() });

                File.WriteAllText(DATA_FILE_NAME, JsonConvert.SerializeObject(data, Formatting.Indented));
                return true;
            }
            else
            {
                Console.WriteLine("\nUngültiges Paket empfangen:\nPaket fängt nicht mit 'pm ' an\n" + input);
                return false;
            }
        }

        // probably doesnt work anymore
        private static void SendTestPackets(int port)
        {
            var localIp = GetLocalIPAddress();
            Task.Run(() =>
            {
                while (true)
                {
                    var data = Encoding.UTF8.GetBytes($"pm {DateTime.Now.Second} 190.9 6.0 72.6 70.5 37.5 6.6 53.6 120.0 20.0 20.0 64.0 53.0 0.0 23.5 20.0 15 14 14 14 71.5 100 6 6.0 21.0 190.0 0.0 79.0 -20.0 -20.0 20.0 20.0 -20.0 0.0 0.0 20.0 20.0 -20.0 -20.0 20.0 20.0 -20.0 0.0 0.0 20.0 20.0 70 38.7 59.4 73 0.0 0.0 0.0 0.0 0.0 0.0 0.0 121.0 60.0 5.9 0.0 0.0 0.0 0.00 0.00 0.00 0.00 -20.0 0.0 20.0 20.0 -20.0 1 3 1 1 1 1 1 20.0 20.0 20.0 20.0 20.0 20.0 20.0 -20.0 1 15.9 0.0 0 0 2 -20 60 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 4 1 0 0 0 0 0 0 0 0 0 0 0 0 1 53 53 57332.2 24183.1 4678.9 2162.4 69.6 8277.8 0.0 0.0 0.0 0.0 0 1 0 0 0 0 0 0 0 0 0 -20.0 0.0 0.0 0.0 9999 0003 0203 9300 0245 0102 0050 0203");

                    new UdpClient().Send(data, data.Length, localIp, port);
                    Thread.Sleep(1000);
                }
            });
        }

        private static string GetLocalIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
    }
}

