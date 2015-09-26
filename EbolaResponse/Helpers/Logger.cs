using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LastMileHealth.Helpers
{
    public static class Logger
    {
        static StreamWriter sw;
        static object lockPoint = new object();

        private static bool IsDebugVersion = false;

        private static ObservableCollection<string> log = new ObservableCollection<string>();
        private static ObservableCollection<string> logWithoutExceptions = new ObservableCollection<string>();

        public static ObservableCollection<string> GetLog
        {
            get { return log; }
        }

        public static ObservableCollection<string> GetLogWithoutExceptions
        {
            get { return logWithoutExceptions; }
        }

        public static void LogEvent(bool isForDebug, string line, params object[] args)
        {
            if (!string.IsNullOrWhiteSpace(line) && (IsDebugVersion || !isForDebug))
            {
                if (args != null && args.Length > 0 && ( args[0] == null ||  string.IsNullOrWhiteSpace(args[0].ToString())))
                {
                    return;
                }

                string lineF = string.Format("{0} - {1}", DateTime.Now, string.Format(line, args));
                Debug.WriteLine(lineF);

                if (sw == null)
                {
                    lock (lockPoint)
                    {
                        if (sw == null)
                        {
                            string path = Path.Combine(Constants.ODKPath, "Sessions");

                            Directory.CreateDirectory(path);

                            var v = Path.Combine(path, string.Format("{0:yyyy-MM-dd HH-mm-ss}.txt", DateTime.Now));
                            sw = File.CreateText(v);

                            try
                            {
                                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                                sw.WriteLine("App Version: " + version);

                                if (Application.Current != null)
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        log.Add("App Version: " + version);
                                    }));
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                sw.WriteLine(lineF);
                sw.Flush();

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        log.Add(lineF);
                        if (!lineF.Contains("AppException"))
                        {
                            logWithoutExceptions.Add(lineF);
                        }
                        else
                        {

                        }
                    }));
                }
            }
        }
    }
}
