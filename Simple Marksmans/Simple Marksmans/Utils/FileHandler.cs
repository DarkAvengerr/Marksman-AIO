#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="FileHandler.cs" company="EloBuddy">
// // 
// //  Marksman AIO
// // 
// //  Copyright (C) 2016 Krystian Tenerowicz
// // 
// //  This program is free software: you can redistribute it and/or modify
// //  it under the terms of the GNU General Public License as published by
// //  the Free Software Foundation, either version 3 of the License, or
// //  (at your option) any later version.
// // 
// //  This program is distributed in the hope that it will be useful,
// //  but WITHOUT ANY WARRANTY; without even the implied warranty of
// //  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// //  GNU General Public License for more details.
// // 
// //  You should have received a copy of the GNU General Public License
// //  along with this program.  If not, see http://www.gnu.org/licenses/. 
// //  </copyright>
// //  <summary>
// // 
// //  Email: geroelobuddy@gmail.com
// //  PayPal: geroelobuddy@gmail.com
// //  </summary>
// //  ---------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;

namespace Simple_Marksmans.Utils
{
    internal class FileHandler
    {
        public static string ColorFileName { get; } = "colors.json";

        public static JToken ReadDataFile(string fileName)
        {
            var filePath = Path.Combine(SandboxConfig.DataDirectory, Path.Combine("Marksman AIO", fileName));

            if (File.Exists(filePath) == false)
            {
                Logger.Error("File not found: " + filePath);
                Console.WriteLine("[DEBUG]  File not found");
                return null;
            }
            try
            {
                Console.WriteLine("[DEBUG] Starting new task");
                return Task.Factory.StartNew(() => ReadJsonFile(filePath)).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        private static JToken ReadJsonFile(string path)
        {
            Console.WriteLine("[DEBUG] ReadJsonFile start");
            JToken token;

            using (var streamReader = new StreamReader(path))
            {
                Console.WriteLine("[DEBUG] StreamReader start");
                var file = new JsonTextReader(streamReader);
                Console.WriteLine("[DEBUG] saving token");

                token = GetFileLinesNumber(path) != 0 ? JToken.ReadFrom(file) : null;
                //foreach (var xd in token.First)
                //{
                //    Console.WriteLine("[DEBUG] " + xd);
                //}
            }
            Console.WriteLine("[DEBUG] returning token");
            return token;
        }

        public static void WriteToDataFile(string uniqueId, ColorBGRA color)
        {
            Console.WriteLine("[DEBUG] WriteToDataFile start");
            var filePath = Path.Combine(SandboxConfig.DataDirectory, Path.Combine("Marksman AIO", ColorFileName));

            //Check if directory is created
            if (!Directory.Exists(Path.Combine(SandboxConfig.DataDirectory, "Marksman AIO")))
            {
                Console.WriteLine("[DEBUG] Directory doesnt exist. Creating Marksman AIO folder");
                Directory.CreateDirectory(Path.Combine(SandboxConfig.DataDirectory, "Marksman AIO"));
            }

            if (!File.Exists(filePath))
            {
                using (var file = File.Create(Path.Combine(SandboxConfig.DataDirectory, Path.Combine("Marksman AIO", ColorFileName))))
                {
                    file.Flush();
                }
            }
            
            {
                try
                {
                    Console.WriteLine("[DEBUG] WriteToDataFile try sequence");
                    var file = ReadDataFile(ColorFileName);

                    Dictionary<string, ColorBGRA> dictionary;

                    //Empty file
                    if (file == null)
                    {
                        Console.WriteLine("[DEBUG] Empty file");
                        dictionary = new Dictionary<string, ColorBGRA> {{uniqueId, color}};
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] file not empty");
                        dictionary = file.ToObject<Dictionary<string, ColorBGRA>>();

                        if (dictionary.ContainsKey(uniqueId))
                        {
                            Console.WriteLine("[DEBUG] dic contains key");
                            dictionary[uniqueId] = color;
                        }
                        else
                        {
                            Console.WriteLine("[DEBUG]  dic does not contain a key");
                            dictionary.Add(uniqueId, color);
                        }
                    }
                    Console.WriteLine("[DEBUG] File.Open");
                    var fileStream = File.Open(filePath, FileMode.Open);
                    Console.WriteLine("[DEBUG] StreamWriter start");
                    var streamWriter = new StreamWriter(fileStream);

                    using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        Console.WriteLine("[DEBUG] JsonTextWriter start");
                        jsonWriter.Formatting = Formatting.Indented;
                        Console.WriteLine("[DEBUG] JsonSerializer");
                        new JsonSerializer().Serialize(jsonWriter, dictionary);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }
        }

        public static int GetFileLinesNumber(string filePath)
        {
            using (var streamReader = new StreamReader(filePath))
            {
                var linesNumber = 0;
                while (streamReader.ReadLine() != null)
                {
                    linesNumber++;
                }
                return linesNumber;
            }
        }
    }
}