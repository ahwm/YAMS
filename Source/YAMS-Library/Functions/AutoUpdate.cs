using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;

namespace YAMS
{
    public static class AutoUpdate
    {
        //Settings
        public static bool bolUpdateGUI = false;
        public static bool bolUpdateJAR = false;
        public static bool bolUpdateClient = false;
        public static bool bolUpdateAddons = false;
        public static bool bolUpdateSVC = false;
        public static bool bolUpdateWeb = false;
        public static bool UpdatePaused = false;

        //Update booleans
        public static bool bolServerUpdateAvailable = false;
        public static bool bolPreUpdateAvailable = false;
        public static bool bolDllUpdateAvailable = false;
        public static bool bolServiceUpdateAvailable = false;
        public static bool bolGUIUpdateAvailable = false;
        public static bool bolWebUpdateAvailable = false;
        public static bool bolOverviewerUpdateAvailable = false;
        public static bool bolC10tUpdateAvailable = false;
        public static bool bolTectonicusUpdateAvailable = false;
        public static bool bolRestartNeeded = false;
        public static bool bolBukkitUpdateAvailable = false;
        public static bool bolBukkitBetaUpdateAvailable = false;
        public static bool bolBukkitDevUpdateAvailable = false;
        public static bool bolLibUpdateAvailable = false;
        public static bool bolReporterUpdateAvailable = false;
        public static bool bolReporterConfigUpdateAvailable = false;


        //Minecraft URLs
        public static string strMCClientURL = "https://s3.amazonaws.com/MinecraftDownload/launcher/minecraft.jar";
        public static string strMCVersionFile = "https://s3.amazonaws.com/Minecraft.Download/versions/versions.json";

        //YAMS URLs
        public static Dictionary<string, string> strYAMSUpdatePath = new Dictionary<string, string>()
        {
            { "live", "https://s3-eu-west-1.amazonaws.com/yams-dl"},
            { "dev", "https://s3-eu-west-1.amazonaws.com/yams-dl/development" }
        };

        //Checks for available updates
        public static void CheckUpdates(bool bolForce = false, bool bolManual = false)
        {
            if (!UpdatePaused)
            {
                Database.AddLog("Starting update check", "updater");
                
                //What branch are we on?
                string strBranch = Database.GetSetting("UpdateBranch", "YAMS");
                string strYPath = strYAMSUpdatePath[strBranch];

                //Grab latest version file if it needs updating
                UpdateIfNeeded(strYPath + @"/versions.json", YAMS.Core.RootFolder + @"\lib\versions.json");
                string json = File.ReadAllText(YAMS.Core.RootFolder + @"\lib\versions.json");
                //Dictionary<string, string> dicVers = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                JObject jVers = JObject.Parse(json);

                //Reset all the JAR etags so we re-download them
                if (bolForce) YAMS.Database.AddLog("Forced re-download of JAR files", "updater", "warn");
                
                //Check Minecraft server first
                if (bolUpdateJAR || bolManual)
                {
                    UpdateIfNeeded(strMCVersionFile, YAMS.Core.RootFolder + @"\lib\mojang-versions.json");
                    string jsonMojang = File.ReadAllText(YAMS.Core.RootFolder + @"\lib\mojang-versions.json");
                    JObject mojangVers = JObject.Parse(jsonMojang);
                    string releaseVer = (string)mojangVers["latest"]["release"];
                    string snapshotVer = (string)mojangVers["latest"]["snapshot"];
                    string strMCServerURL = "https://s3.amazonaws.com/Minecraft.Download/versions/" + releaseVer + "/minecraft_server." + releaseVer + ".jar";
                    string strMCPreServerURL = "https://s3.amazonaws.com/Minecraft.Download/versions/" + snapshotVer + "/minecraft_server." + snapshotVer + ".jar";

                    if (bolForce)
                    {
                        YAMS.Database.SaveEtag(strMCServerURL, "");
                        YAMS.Database.SaveEtag(strMCPreServerURL, "");
                    }

                    bolServerUpdateAvailable = UpdateIfNeeded(strMCServerURL, YAMS.Core.RootFolder + @"\lib\minecraft_server.jar.UPDATE");
                    bolPreUpdateAvailable = UpdateIfNeeded(strMCPreServerURL, YAMS.Core.RootFolder + @"\lib\minecraft_server_pre.jar.UPDATE");

                    UpdateIfNeeded(strYPath + @"/properties.json", YAMS.Core.RootFolder + @"\lib\properties.json");
                }

                //Have they opted for bukkit? If so, update that too
                if (Convert.ToBoolean(Database.GetSetting("BukkitInstalled", "YAMS")))
                {
                    string strReleaseVersionFile = Util.GetTextHTTP("http://dl.bukkit.org/api/1.0/downloads/projects/craftbukkit/view/latest-rb/?_accept=application/json");
                    if (strReleaseVersionFile != null)
                    {
                        JObject releaseVers = JObject.Parse(strReleaseVersionFile);
                        string strBukkitServerURL = (string)releaseVers["file"]["url"];
                        if (bolForce) YAMS.Database.SaveEtag(strBukkitServerURL, "");
                        bolBukkitUpdateAvailable = UpdateIfNeeded(strBukkitServerURL, Core.RootFolder + @"\lib\craftbukkit.jar.UPDATE", "modified");
                    }
                }
                if (Convert.ToBoolean(Database.GetSetting("BukkitBetaInstalled", "YAMS")))
                {
                    string strBetaVersionFile = Util.GetTextHTTP("http://dl.bukkit.org/api/1.0/downloads/projects/craftbukkit/view/latest-beta/?_accept=application/json");
                    if (strBetaVersionFile != null)
                    {
                        JObject betaVers = JObject.Parse(strBetaVersionFile);
                        string strBukkitBetaServerURL = (string)betaVers["file"]["url"];
                        if (bolForce) YAMS.Database.SaveEtag(strBukkitBetaServerURL, "");
                        bolBukkitBetaUpdateAvailable = UpdateIfNeeded(strBukkitBetaServerURL, Core.RootFolder + @"\lib\craftbukkit-beta.jar.UPDATE", "modified");
                    }
                }
                if (Convert.ToBoolean(Database.GetSetting("BukkitDevInstalled", "YAMS")))
                {
                    string strDevVersionFile = Util.GetTextHTTP("http://dl.bukkit.org/api/1.0/downloads/projects/craftbukkit/view/latest-dev/?_accept=application/json");
                    if (strDevVersionFile != null)
                    {
                        JObject devVers = JObject.Parse(strDevVersionFile);
                        string strBukkitDevServerURL = (string)devVers["file"]["url"];
                        if (bolForce) YAMS.Database.SaveEtag(strBukkitDevServerURL, "");
                        bolBukkitDevUpdateAvailable = UpdateIfNeeded(strBukkitDevServerURL, Core.RootFolder + @"\lib\craftbukkit-dev.jar.UPDATE", "modified");
                    }
                }

                //Now update self
                if (bolUpdateSVC || bolManual)
                {
                    bolDllUpdateAvailable = UpdateIfNeeded(strYPath + @"/YAMS-Library.dll", YAMS.Core.RootFolder + @"\YAMS-Library.dll.UPDATE");
                    if (UpdateIfNeeded(strYPath + @"/YAMS-Service.exe", YAMS.Core.RootFolder + @"\YAMS-Service.exe.UPDATE") || UpdateIfNeeded(strYPath + @"/YAMS-Service.exe.config", YAMS.Core.RootFolder + @"\YAMS-Service.exe.config.UPDATE"))
                    {
                        bolServiceUpdateAvailable = true;
                    }
                    bolWebUpdateAvailable = UpdateIfNeeded(strYPath + @"/web.zip", YAMS.Core.RootFolder + @"\web.zip");
                    bolGUIUpdateAvailable = UpdateIfNeeded(strYPath + @"/YAMS-Updater.exe", YAMS.Core.RootFolder + @"\YAMS-Updater.exe");
                    bolReporterUpdateAvailable = UpdateIfNeeded(strYPath + @"/YAMS-Reporter.exe", YAMS.Core.RootFolder + @"\YAMS-Reporter.exe");
                    bolReporterConfigUpdateAvailable = UpdateIfNeeded(strYPath + @"/YAMS-Reporter.exe.config", YAMS.Core.RootFolder + @"\YAMS-Reporter.exe.config");

                    //Update External libs
                    foreach (JProperty j in jVers["libs"])
                    {
                        if (UpdateIfNeeded(strYAMSUpdatePath[strBranch] + @"/lib/" + j.Name, Core.RootFolder + @"\lib\" + j.Name + ".UPDATE")) bolLibUpdateAvailable = true;
                    }
                }

                if (bolUpdateAddons || bolManual)
                {
                    //Update add-ons if they have elected to have them
                    //Update overviewer
                    if (Convert.ToBoolean(Database.GetSetting("OverviewerInstalled", "YAMS"))) {
                        string strOverviewerURL = (string)jVers["apps"]["overviewer-" + YAMS.Util.GetBitness()];
                        if (UpdateIfNeeded(strOverviewerURL, YAMS.Core.RootFolder + @"\apps\overviewer.zip", "modified"))
                        {
                            bolOverviewerUpdateAvailable = true;
                            if (!Directory.Exists(YAMS.Core.RootFolder + @"\apps\overviewer-new\\")) Directory.CreateDirectory(YAMS.Core.RootFolder + @"\apps\overviewer-new\\");
                            ExtractZip(YAMS.Core.RootFolder + @"\apps\overviewer.zip", YAMS.Core.RootFolder + @"\apps\overviewer-new\\");
                            File.Delete(YAMS.Core.RootFolder + @"\apps\overviewer.zip");
                            if (Directory.Exists(YAMS.Core.RootFolder + @"\apps\overviewer\")) Directory.Delete(YAMS.Core.RootFolder + @"\apps\overviewer\", true);
                            Directory.Move(YAMS.Core.RootFolder + @"\apps\overviewer-new", YAMS.Core.RootFolder + @"\apps\overviewer");
                        }
                    }
                }

                //Now check if we can auto-restart anything
                if ((bolDllUpdateAvailable || bolServiceUpdateAvailable || bolWebUpdateAvailable || bolRestartNeeded || bolLibUpdateAvailable) && Convert.ToBoolean(Database.GetSetting("RestartOnSVCUpdate", "YAMS")))
                {
                    //Check there are no players on the servers
                    bool bolPlayersOn = false;
                    foreach (KeyValuePair<int, MCServer> kvp in Core.Servers)
                    {
                        if (kvp.Value.Players.Count > 0) bolPlayersOn = true;
                    }
                    if (bolPlayersOn)
                    {
                        Database.AddLog("Deferring update until free");
                        bolRestartNeeded = true;
                    }
                    else
                    {
                        Database.AddLog("Restarting Service for updates");
                        System.Diagnostics.Process.Start(YAMS.Core.RootFolder + @"\YAMS-Updater.exe", "/restart");
                    }
                }

                //Restart individual servers?
                if ((bolServerUpdateAvailable || bolBukkitUpdateAvailable || bolPreUpdateAvailable) && Convert.ToBoolean(Database.GetSetting("RestartOnJarUpdate", "YAMS")))
                {
                    foreach (KeyValuePair<int, MCServer> kvp in Core.Servers)
                    {
                        if (((kvp.Value.ServerType == "vanilla" && bolServerUpdateAvailable) || (kvp.Value.ServerType == "bukkit" && bolBukkitUpdateAvailable)
                            || (kvp.Value.ServerType == "bukkit-beta" && bolBukkitBetaUpdateAvailable) || (kvp.Value.ServerType == "bukkit-dev" && bolBukkitDevUpdateAvailable)))
                        {
                            kvp.Value.RestartIfEmpty();
                        }
                    }
                }

                Database.AddLog("Finished update check", "updater");
            }
            else
            {
                Database.AddLog("Updating Paused", "updater", "warn");
            }
        }

        public static void ExtractZip(string strZipFile, string strPath)
        {
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(strZipFile)))
            {

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {

                    Console.WriteLine(theEntry.Name);

                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);

                    // create directory
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(strPath + directoryName);
                    }

                    if (fileName != String.Empty)
                    {
                        using (FileStream streamWriter = File.Create(strPath + theEntry.Name))
                        {

                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0)
                                {
                                    streamWriter.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

        }

        public static bool UpdateIfNeeded(string strURL, string strFile, string strType = "etag")
        {
            //Get our stored eTag for this URL
            string strETag = "";
            strETag = YAMS.Database.GetEtag(strURL);

            try
            {
                //Set up a request and include our eTag
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(strURL);
                request.UserAgent = "YAMS Downloader (http://yams.in)";
                request.Method = "GET";
                if (strETag != "")
                {
                    if (strType == "etag")
                    {
                        request.Headers[HttpRequestHeader.IfNoneMatch] = strETag;
                    }
                    else
                    {
                        try
                        {
                            strETag = strETag.Replace("UTC", "GMT"); //Fix for weird servers not sending correct formate datetime
                            request.IfModifiedSince = Convert.ToDateTime(strETag);
                        }
                        catch (Exception e) { Database.AddLog("Unable to set modified date for URL: " + strURL + "; " + e.Message, "updater", "warn"); }
                    }
                }
                //if (strETag != null) request.Headers[HttpRequestHeader.IfModifiedSince] = strETag;

                //Grab the response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Database.AddLog("Downloading " + strFile, "updater");

                //Stream the file
                Stream strm = response.GetResponseStream();
                FileStream fs = new FileStream(strFile, FileMode.Create, FileAccess.Write, FileShare.None);
                const int ArrSize = 10000;
                Byte[] barr = new Byte[ArrSize];
                while (true)
                {
                    int Result = strm.Read(barr, 0, ArrSize);
                    if (Result == -1 || Result == 0)
                        break;
                    fs.Write(barr, 0, Result);
                }
                fs.Flush();
                fs.Close();
                strm.Close();
                response.Close();

                //Save the etag
                if (strType == "etag")
                {
                    if (response.Headers[HttpResponseHeader.ETag] != null) YAMS.Database.SaveEtag(strURL, response.Headers[HttpResponseHeader.ETag]);
                }
                else
                {
                    if (response.Headers[HttpResponseHeader.LastModified] != null) YAMS.Database.SaveEtag(strURL, response.Headers[HttpResponseHeader.LastModified]);
                }

                YAMS.Database.AddLog(strFile + " update downloaded", "updater");

                return true;
            }
            catch (System.Net.WebException ex)
            {
                if (ex.Response != null)
                {
                    using (HttpWebResponse response = ex.Response as HttpWebResponse)
                    {
                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            //304 means there is no update available
                            //YAMS.Database.AddLog(strFile + " is up to date", "updater");
                            return false;
                        }
                        else
                        {
                            // Wasn't a 200, and wasn't a 304 so let the log know
                            YAMS.Database.AddLog(string.Format("Failed to check " + strURL + ". Error Code: {0}", response.StatusCode), "updater", "error");
                            return false;
                        }
                    }
                }
                else return false;
            }
            catch (Exception e)
            {
                YAMS.Database.AddLog(string.Format("Failed to update " + strFile + ". Error: {0}", e.Message), "updater", "error");
                return false;
            }
        }
    }
}
