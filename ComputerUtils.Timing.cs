using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ComputerUtils.Timing
{
    public class TimeDelay
    {
        public static async Task DelayWithoutThreadBlock(int ms)
        {
            var frame = new DispatcherFrame();
            new Thread((ThreadStart)(() =>
            {
                Thread.Sleep(ms);
                frame.Continue = false;
            })).Start();
            Dispatcher.PushFrame(frame);
        }
    }
}