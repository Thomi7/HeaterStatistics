﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

namespace PackageFormatConverter
{
    class Program
    {
private const string OUTPUT_FILE_NAME = "parameterConfig.json";

        static void Main(string[] args)
        {
            Console.WriteLine("Legen sie die Parameter Datei (.txt) in das gleiche Verzeichnis wie die EXE und geben sie den Dateinamen an: ");
            //var path = Console.ReadLine();
            var path = "Parameter.txt";

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
               text = text.Replace("\n", " ");
                 text = text.Replace("\r", " ");
                 text = text.Replace("\t", " ");
                var split = text.Split(" ");
                var parameterList = new List<ParameterConfig>();
                var digital = false;
                var digitalComplete = false;
                var analog = false;
                var currentParameterConfig = new ParameterConfig();


                // read Analog Config
                var analogConfig = new List<AnalogConfig>();
                var i = 0;
                var startedReading = false;
                var currentAnalogConfig = new AnalogConfig() {Id = -1};
                try{
                    while(true) {
                        if(i>=split.Length){
                            throw new ReadParameterException("no ending </ANALOG> tag found");
                        } else {
                            var input = split[i];
                            if(input != " " && input!= "")
                            {
                                if(startedReading) {
                                    if(input.StartsWith("id=")) {
                                        int id;
                                        var u = input.Substring(("id=").Length, input.Length-("id=").Length).Replace("'", "");
                                        if(int.TryParse(input.Substring(("id=").Length, input.Length-("id=").Length).Replace("'", ""), out id))
                                        {
                                            if(currentAnalogConfig.Id != -1)
                                                analogConfig.Add(currentAnalogConfig);
                                            currentAnalogConfig = new AnalogConfig();
                                            currentAnalogConfig.Id = id;
                                        }
                                        else throw new ReadParameterException($"Id has to be integer ({input})");
                                    } else if(input.StartsWith("name="))
                                            currentAnalogConfig.Name = input.Substring("name=".Length, input.Length-"name=".Length);
                                        else if(input.StartsWith("unit="))
                                            currentAnalogConfig.Unit = input.Substring("unit=".Length, input.Length-"unit=".Length);
                                        else if(input == "</ANALOG>") {
                                            if(currentAnalogConfig.Id != -1)
                                                analogConfig.Add(currentAnalogConfig);
                                            i++;
                                            break;
                                        } else throw new ReadParameterException($"unexcepted string: {input}");
                                } else if(input == "<ANALOG>") {
                                    startedReading = true;
                                }
                            }
                            i++;
                        }
                    }
                } catch(ReadParameterException e) {
                    Console.WriteLine("Fehler bei der Konvertierung:\n" + e);
                }

                // analogConfig.Sort()
                // analogConfig

var idsf = 0;
                // foreach(var s in split){
                //     Console.WriteLine(s);
                //     if(s != " " && s!= "")
                //     {
                //         if(!digital) {
                //             if( s == "<DIGITAL>") digital= true;
                //             else throw new NotImplementedException();
                //         } else {
                //             if(s=="</DIGITAL>")
                //                 break;
                //             else {
                //                 if(s.StartsWith("id=")) {
                //                     currentParameterConfig = new ParameterConfig();
                //                     currentParameterConfig.Id = int.Parse(s.Substring(("id=").Length, s.Length-("id=").Length).Replace("'", ""));
                //                 } else if(!analog && digitalComplete) {
                //                     if(s == "<ANALOG>") analog = true;
                //                     else throw new NotImplementedException();
                //                 }
                //             }
                //             if(s.StartsWith("id=")) {
                //                 currentParameterConfig = new ParameterConfig();
                //                 currentParameterConfig.Id = int.Parse(s.Substring(("id=").Length, s.Length-("id=").Length).Replace("'", ""));
                //             } else if(!analog && digitalComplete) {
                //                 if(s == "<ANALOG>") analog = true;
                //                 else throw new NotImplementedException();
                //             } else if(analog) {
                //                 if(s.StartsWith("name=")) {
                //                     currentParameterConfig.Name = s.Substring("name=".Length, s.Length-"name=".Length);
                //                     parameterList.Add(currentParameterConfig);
                //                 } else if(s.StartsWith("bit=")){
                //                     currentParameterConfig.Bit = int.Parse(s.Substring("bit=".Length, s.Length-"bit=".Length));
                //                 } else if (s == "</ANALOG>")
                //                     break;
                //                 else throw new NotImplementedException();
                //             } else if(digital){
                //                 if(s.StartsWith("name=")) {
                //                     currentParameterConfig.Name = s.Substring("name=".Length, s.Length-"name=".Length);
                //                 } else if(s.StartsWith("unit=")){
                //                     currentParameterConfig.Unit = s.Substring("unit=".Length, s.Length-"unit=".Length);
                //                     parameterList.Add(currentParameterConfig);
                //                 } else if (s == "</DIGITAL>")
                //                     digitalComplete = false;
                //                 else throw new NotImplementedException();
                //             }
                //         }
                        // if(!digital) {
                        //     if( s == "<DIGITAL>") digital= true;
                        //     else throw new NotImplementedException();
                        // } else {
                        //     if(s.StartsWith("id=")) {
                        //         currentParameterConfig = new ParameterConfig();
                        //         currentParameterConfig.Id = int.Parse(s.Substring(("id=").Length, s.Length-("id=").Length).Replace("'", ""));
                        //     } else if(!analog && digitalComplete) {
                        //         if(s == "<ANALOG>") analog = true;
                        //         else throw new NotImplementedException();
                        //     } else if(analog) {
                        //         if(s.StartsWith("name=")) {
                        //             currentParameterConfig.Name = s.Substring("name=".Length, s.Length-"name=".Length);
                        //             parameterList.Add(currentParameterConfig);
                        //         } else if(s.StartsWith("bit=")){
                        //             currentParameterConfig.Bit = int.Parse(s.Substring("bit=".Length, s.Length-"bit=".Length));
                        //         } else if (s == "</ANALOG>")
                        //             break;
                        //         else throw new NotImplementedException();
                        //     } else if(digital){
                        //         if(s.StartsWith("name=")) {
                        //             currentParameterConfig.Name = s.Substring("name=".Length, s.Length-"name=".Length);
                        //         } else if(s.StartsWith("unit=")){
                        //             currentParameterConfig.Unit = s.Substring("unit=".Length, s.Length-"unit=".Length);
                        //             parameterList.Add(currentParameterConfig);
                        //         } else if (s == "</DIGITAL>")
                        //             digitalComplete = false;
                        //         else throw new NotImplementedException();
                        //     }
                        // }
                // parameterList.Add(new ParameterConfig() {Id = 0, Name="Test", Unit="unit"});
                //  parameterList.Add(new ParameterConfig() {Id = 1, Name="Test1", Unit="unit2"});
                //   parameterList.Add(new ParameterConfig() {Id = 2, Name="Test2", Unit="unit3"});
                File.WriteAllText(OUTPUT_FILE_NAME, JsonSerializer.Serialize(parameterList, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
                // try
                // {
                //     Config = JsonSerializer.Deserialize<Config>(File.ReadAllText(CONFIG_FILE_NAME));
                // }
                // catch (Exception e)
                // {
                //     Console.WriteLine(CONFIG_FILE_NAME + " konnte nicht eingelesen werden\n" + e.ToString());
                // }
            }
            else
            Console.WriteLine("Datei nicht vorhanden");

Console.WriteLine("Beliebige Taste drücken um Anwendung zu beenden");
            Console.ReadKey();
        }
    }
}

