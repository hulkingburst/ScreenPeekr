using System.Runtime.InteropServices;

namespace ScreenPeekr.Services;

internal static class InputSimulator
{
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUnion u;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    public static async Task SendKeysAsync(IEnumerable<System.Windows.Forms.Keys> keys, int delayMs, int holdDurationMs, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var key in keys.Where(key => key != System.Windows.Forms.Keys.None))
            {
                cancellationToken.ThrowIfCancellationRequested();
                SendKeyDown((ushort)key);
                if (holdDurationMs > 0)
                {
                    await Task.Delay(holdDurationMs, cancellationToken);
                }

                SendKeyUp((ushort)key);
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }
    }

    public static void SendKey(ushort virtualKeyCode)
    {
        try
        {
            var inputs = new INPUT[2];

            // Key Down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key Up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        catch
        {
        }
    }

    private static void SendKeyDown(ushort virtualKeyCode)
    {
        try
        {
            SendInput(1, new[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = virtualKeyCode,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            }, Marshal.SizeOf(typeof(INPUT)));
        }
        catch
        {
        }
    }

    private static void SendKeyUp(ushort virtualKeyCode)
    {
        try
        {
            SendInput(1, new[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = virtualKeyCode,
                            dwFlags = KEYEVENTF_KEYUP,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            }, Marshal.SizeOf(typeof(INPUT)));
        }
        catch
        {
        }
    }
}
