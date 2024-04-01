using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SonarrLinkMonitor
{
    public class settings
    {
        public List<grabbedFile> recentGrabs = new List<grabbedFile>();
        public List<replacement> replacements = new List<replacement>();

        public string sonarrURL; //todo, make sure no trailing '/'
        public string sonarrAPI;
        public string destinationFolder; //todo, make sure no trailing '/'
        public int sonarrMaxHistory = 200;

        public static string foldername = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SonarrLinkMonitor\";
        private static string filename = foldername + "sett.ings";

        public static settings load()
        {
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(settings));
                TextReader textReader = new StreamReader(filename);
                settings setts = (settings)deserializer.Deserialize(textReader);
                textReader.Close();
                setts.trimHistory();
                return setts;
            }




            catch (Exception e)
            {
                if (e is System.IO.FileNotFoundException || e is System.IO.IOException)
                {
                    //create a new one
                    settings sets = new settings();

                    #region populate defaults
                    // TODO remove
                    sets.sonarrURL = "http://davepine:8089";
                    sets.sonarrAPI = "8ef5ec83eaa041299ab5296dcb1ed36a";
                    sets.destinationFolder = @"C:\temp";

                    sets.replacements.Add(new replacement(@"F:\eps\", @"\\davepine\eps2\"));
                    sets.replacements.Add(new replacement(@"D:\eps\", @"\\davepine\eps\"));

                    sets.save();
                    #endregion
                    return sets;

                }
                throw;
            }
        }

        public void save()
        {
            DirectoryInfo di = new DirectoryInfo(foldername);
            if (!di.Exists)
            {
                di.Create();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(settings));
            TextWriter textWriter = new StreamWriter(filename);
            serializer.Serialize(textWriter, this);
            textWriter.Close();
        }

        /// <summary>
        /// Throws away history after 1000 files
        /// </summary>
        public void trimHistory()
        {
            bool quitloop = false;
            while (!quitloop)
            {
                if (recentGrabs.Count > 2000)
                {
                    Console.Out.WriteLine("Removing " + recentGrabs[0].filename + " from history");
                    recentGrabs.RemoveAt(0);
                    
                }
                else
                {
                    quitloop = true;
                }

            }

        }

    }



}
