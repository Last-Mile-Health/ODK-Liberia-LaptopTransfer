using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LastMileHealth.Helpers
{
    public static class Utils
    {
        #region Helper methods

        public static byte[] IntToByteArray(int a)
        {
            byte[] ret = new byte[4];
            ret[3] = (byte)(a & 0xFF);
            ret[2] = (byte)((a >> 8) & 0xFF);
            ret[1] = (byte)((a >> 16) & 0xFF);
            ret[0] = (byte)((a >> 24) & 0xFF);
            return ret;
        }

        public static int ByteArrayToInt(byte[] b)
        {
            return (b[3] & 0xFF) + ((b[2] & 0xFF) << 8) + ((b[1] & 0xFF) << 16) + ((b[0] & 0xFF) << 24);
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        #endregion Helper methods

        public static Task Delay(double milliseconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += (obj, args) =>
            {
                tcs.TrySetResult(true);
            };
            timer.Interval = milliseconds;
            timer.AutoReset = false;
            timer.Start();
            return tcs.Task;
        }

        public static void Wait(double milliseconds)
        {
            var frame = new DispatcherFrame();
            new Thread((ThreadStart)(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(milliseconds));
                frame.Continue = false;
            })).Start();
            Dispatcher.PushFrame(frame);
        }
    }
}
