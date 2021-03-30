
using IWshRuntimeLibrary;
using System.Text;
using System;
using System.Web;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text.Json;

namespace SonarrLinkMonitor
{
    public class SonarrLinkMonitor
    {


        static void Main(string[] args)
        {
            SonarrLinkMonitor.CreateObject();
        }

        private static void CreateObject()
        {
            settings Settings = settings.load();

            string URL = Settings.sonarrURL + "/api/history?page=1&pageSize=" + Settings.sonarrMaxHistory + "&sortkey=date&sortDir=desc&apikey=" + Settings.sonarrAPI;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "GET";
            request.ContentType = "application/json";
            //request.ContentLength = DATA.Length;
            

            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream())
                {
                    if (webStream != null)
                    {
                        using (StreamReader responseReader = new StreamReader(webStream))
                        {
                           string response = responseReader.ReadToEnd();

                            JsonDocument jo = JsonDocument.Parse(response);

                           


                            //open the response and parse it using JSON. Query for newly imported files
                            foreach (JsonElement element in jo.RootElement.GetProperty("records").EnumerateArray())
                            {
                                try
                                {

                                    string title = element.GetProperty("sourceTitle").ToString();
                                    JsonElement elData = element.GetProperty("data");

                                    JsonElement elImportPath;
                                    bool success = elData.TryGetProperty("importedPath", out elImportPath);
                                    if (!success)
                                    {
                                        Console.Out.WriteLine("No import path for " + title + " - failed / still processing?");
                                    }
                                    else
                                    {

                                        string importPath = elImportPath.ToString();

                                        //go through these and create the link. 
                                        bool found = false;
                                        foreach (grabbedFile g in Settings.recentGrabs)
                                        {
                                            if (g.filename == importPath) { found = true; }
                                        }

                                        if (found == true)
                                        {
                                            Console.Out.WriteLine("Already processed " + importPath);
                                        }
                                        else
                                        {
                                            Console.Out.WriteLine("Processing        " + importPath);
                                            string filename = Path.GetFileNameWithoutExtension(importPath);

                                            //invalid chars
                                            foreach (char invalidchar in System.IO.Path.GetInvalidFileNameChars())
                                            {
                                                filename = filename.Replace(invalidchar, '_');
                                            }

                                            filename = Settings.destinationFolder + @"/" + filename + ".lnk";


                                            //apply any replacements
                                            string destination = importPath;
                                            foreach (replacement r in Settings.replacements)
                                            {
                                                destination = destination.Replace(r.source, r.replace);
                                            }
                                            //replace unix paths with windows ones.
                                            destination = destination.Replace(@"/", @"\");

                                            //TODO - for linux use mslink.sh http://www.mamachine.org/mslink/index.en.html 
                                            //craete the link file. 
                                            var wsh = new IWshShell_Class();
                                            try
                                            {
                                                IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(filename) as IWshRuntimeLibrary.IWshShortcut;
                                                shortcut.TargetPath = destination;
                                                shortcut.Save();
                                            }

                                            catch (Exception e)
                                            {

                                                Console.Out.WriteLine("--------Failed to create shortcut---------");
                                                Console.Out.WriteLine(filename);
                                                Console.Out.WriteLine(e.Message);
                                                Console.Out.WriteLine(e.InnerException);
                                                Console.Out.WriteLine(e.StackTrace);
                                                Console.Out.WriteLine("-----------------");

                                            }
                                            //log it
                                            Settings.recentGrabs.Add(new grabbedFile(importPath));
                                        }
                                    }
                                }
                                catch (Exception e)
                                {

                                    Console.Out.WriteLine("-----Failed to parse record------------");
                                    Console.Out.WriteLine(e.Message);
                                    Console.Out.WriteLine(e.InnerException);
                                    Console.Out.WriteLine(e.StackTrace);
                                    Console.Out.WriteLine("-----------------");

                                }

                            }

                            
                            
                        }
                    }
                }
                Settings.save();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("-----------------");
                Console.Out.WriteLine(e.Message);
                Console.Out.WriteLine(e.InnerException);
                Console.Out.WriteLine(e.StackTrace);
                Console.Out.WriteLine(e.ToString());

            }

        }
    }
}