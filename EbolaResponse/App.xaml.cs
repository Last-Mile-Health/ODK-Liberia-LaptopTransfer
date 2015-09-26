using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LastMileHealth.Helpers;
using System.Diagnostics;

namespace LastMileHealth
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += OnAppDispatcherUnhandledException;
            InitializeApp();
        }

        private static void InitializeApp()
        {
            var path = Constants.ODKPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Constants.FormsPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void OnAppDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            // Get stack trace for the exception with source file information
            var st = new StackTrace(e.Exception, true);
            StackFrame frame = null;
            string fileName = string.Empty;
            int line = 0;

            if(st != null && st.FrameCount > 0)
            {
                // Get the top stack frame
                frame = st.GetFrame(0);

                if (frame != null)
                {
                    fileName = frame.GetFileName();
                    // Get the line number from the stack frame
                    line = frame.GetFileLineNumber();
                }
            }

            Logger.LogEvent(false, "AppException Error= {0} , \nStackTrace= {1} , \nFileName= {2} , \nLine= {3}", 
                e.Exception.Message, 
                e.Exception.StackTrace, 
                fileName, 
                line);

//            Application.Current.Dispatcher.Invoke(new Action(() =>
//            {
//#if DEBUG
//                MessageBox.Show(Application.Current.MainWindow, "Some error has occurred." + string.Format("{0}:\n{1}\nStackTrace:\n{2}", Constants.DebugOnly, e.Exception.Message, e.Exception.StackTrace), string.Empty);
//#else
//                MessageBox.Show(Application.Current.MainWindow, "Some error has occurred.");
//#endif
//            }));
        }
    }
}
