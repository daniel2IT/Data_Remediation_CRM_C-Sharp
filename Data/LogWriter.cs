using System;
using System.IO;

namespace Data
{
    public class LogWriter
    {
        public static void LogWrite(string logMessage, string className, String csvPath)
        {
            string csvName = string.Format(className + "-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt", DateTime.Now);

            using (StreamWriter txtWriter = File.AppendText(csvPath + "\\" + csvName))
            {
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("<Class Name {0}", className);
                txtWriter.WriteLine("<Messages>");
                txtWriter.WriteLine(logMessage);
                txtWriter.WriteLine("----");
            }
            Console.WriteLine(logMessage);
        }
    }
}
