using System;
using System.Configuration;

namespace Data
{
    class Program
    {
        static void Main()
        {
            try
            {
                using (var service = HelperClass.getCRMServie())
                {
                    foreach (string taskKey in ConfigurationManager.AppSettings.AllKeys)
                    {
                        if (taskKey.StartsWith("Task", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ITasks task = HelperClass.MagicallyCreateInstance( ConfigurationManager.AppSettings.Get(taskKey));
                            LogWriter.LogWrite(task.StartUp(service).ToString(), ConfigurationManager.AppSettings.Get(taskKey), ConfigurationManager.AppSettings["LogPath"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.LogWrite(ex.ToString(), "Error", ConfigurationManager.AppSettings["LogPath"]);
            }
            Console.ReadLine();
        }
    }
}