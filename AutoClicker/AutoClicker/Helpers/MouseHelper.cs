using System.Runtime.InteropServices;

namespace AutoClicker.Helpers
{
    public static class MouseHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        public static void ClickAt(int x, int y, int clickDelay = 50, bool rightClick = false)
        {
            SetCursorPos(x, y);
            Thread.Sleep(clickDelay);

            if (rightClick)
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN, x, y, 0, 0);
                Thread.Sleep(clickDelay);
                mouse_event(MOUSEEVENTF_RIGHTUP, x, y, 0, 0);
            }
            else
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
                Thread.Sleep(clickDelay);
                mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
            }
        }

        public static bool IsKeyPressed(int virtualKeyCode)
        {
            return (GetAsyncKeyState(virtualKeyCode) & 0x8000) != 0;
        }

        public static async Task StartRapidClickWhileKeyPressedAsync(int x, int y, int interval, int clickDelay, 
            int virtualKeyCode, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsKeyPressed(virtualKeyCode))
                {
                    ClickAt(x, y, clickDelay);
                    await Task.Delay(interval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // ê≥èÌÇ»ÉLÉÉÉìÉZÉã
            }
        }

        public static async Task ClickMultipleAsync(IEnumerable<(int x, int y, int delay)> points)
        {
            var tasks = points.Select(async point =>
            {
                await Task.Run(() => ClickAt(point.x, point.y, point.delay));
            });
            await Task.WhenAll(tasks);
        }

        public static async Task ClickSequentiallyAsync(IEnumerable<(int x, int y, int interval, int delay)> points)
        {
            foreach (var point in points)
            {
                await Task.Run(() => ClickAt(point.x, point.y, point.delay));
                await Task.Delay(point.interval);
            }
        }
    }
}
