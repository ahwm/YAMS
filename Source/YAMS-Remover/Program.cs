using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace YAMS_Remover
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!hasAdministrativeRight)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                if (args.Length > 0) processInfo.Arguments = args.ToString();
                try
                {
                    Process.Start(processInfo);
                }
                catch
                {
                    Environment.Exit(0);
                }
                Environment.Exit(0);
            }            
            
            Console.WriteLine("This program will remove Yet Another Minecraft Server. Use only if the uninstaller has failed.");
            Console.WriteLine("This will not delete database file or individual server files, allowing re-install.");
            Console.WriteLine("If you don't plan to re-install you can safely delete all the instalation folder once this process is complete.");
            Console.WriteLine("Continue? (y/n)");

            string strResponse = Console.ReadLine();
            if (strResponse.ToUpper().Equals("Y"))
            {
                Console.WriteLine("Remove Service? (y/n)");
                strResponse = Console.ReadLine();
                if (strResponse.ToUpper().Equals("Y"))
                {
                    //Try and stop the service nicely
                    Console.WriteLine("Stopping service....");
                    try
                    {
                        ServiceController svcYAMS = new ServiceController("YAMS_Service");
                        while (true)
                        {
                            if (svcYAMS.Status.Equals(ServiceControllerStatus.Stopped))
                            {
                                Console.WriteLine("Service not running");
                                break;
                            }
                            else if (svcYAMS.Status.Equals(ServiceControllerStatus.StartPending))
                            {
                                System.Threading.Thread.Sleep(1000);
                            }
                            else
                            {
                                svcYAMS.Stop();
                                Console.WriteLine("Service stopped");
                                break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {1}", e.Message);
                    }

                    //Check if the process has really gone away, if not kill it.
                    Process[] processes = Process.GetProcessesByName("YAMS-Service.exe");
                    if (processes.Length > 0)
                    {
                        Console.WriteLine("Killing process...");
                        processes[0].Close();
                    }

                    //Now try and remove                    
                    Console.WriteLine("Removing service....");
                    Console.WriteLine("Trying \"sc delete YAMS_Service\"");
                    try
                    {
                        System.Diagnostics.ProcessStartInfo procStartInfo =
                            new System.Diagnostics.ProcessStartInfo("cmd", "/c sc delete YAMS_Service");

                        procStartInfo.RedirectStandardOutput = true;
                        procStartInfo.UseShellExecute = false;
                        procStartInfo.CreateNoWindow = true;
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.StartInfo = procStartInfo;
                        proc.Start();
                        string result = proc.StandardOutput.ReadToEnd();
                        Console.WriteLine(result);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {1}", e.Message);
                    }

                    Console.WriteLine("Checking Registry...");
                    string keyName = @"SYSTEM\ControlSet001\Services";
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName, true))
                    {
                        try
                        {
                            key.DeleteSubKeyTree("YAMS_Service");
                            Console.WriteLine("Registry key deleted");
                        }
                        catch (KeyNotFoundException e)
                        {
                            Console.WriteLine("Registry key already deleted");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception: {1}", e.Message);
                        }
                    }

                }

                Console.WriteLine("Remove Program files? (y/n)");
                strResponse = Console.ReadLine();
                if (strResponse.ToUpper().Equals("Y"))
                {

                }

                Console.WriteLine("Remove add/remove programs entry? (y/n)");
                strResponse = Console.ReadLine();
                if (strResponse.ToUpper().Equals("Y"))
                {

                }
            
            }

        }
    }
}
