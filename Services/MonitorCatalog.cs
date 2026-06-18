using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenPeekr.Models;
using System.Management;

namespace ScreenPeekr.Services;

internal sealed class MonitorCatalog : IDisposable
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var wmiNames = GetWmiMonitorNames();
        return Screen.AllScreens
            .Select((screen, index) =>
            {
                var friendlyName = TryGetFriendlyMonitorName(screen.DeviceName, wmiNames);
                var displayName = string.IsNullOrWhiteSpace(friendlyName)
                    ? $"Display {index + 1}"
                    : friendlyName;
                return new MonitorInfo(screen.DeviceName, displayName, screen.Bounds);
            })
            .ToList();
    }

    public MonitorInfo GetSelectedOrDefault(string selectedMonitorId)
    {
        if (string.Equals(selectedMonitorId, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return new MonitorInfo("ALL", "All Monitors", SystemInformation.VirtualScreen);
            }
            catch
            {
                // Fallback to single monitor if getting virtual screen fails
            }
        }

        try
        {
            var monitors = GetMonitors();
            var matched = monitors.FirstOrDefault(m => string.Equals(m.Id, selectedMonitorId, StringComparison.OrdinalIgnoreCase))
                ?? monitors.FirstOrDefault();
            if (matched != null)
            {
                return matched;
            }
        }
        catch
        {
            // Fallback to primary screen below
        }

        try
        {
            var primary = Screen.PrimaryScreen;
            return new MonitorInfo(
                primary?.DeviceName ?? "PRIMARY",
                "Primary Display",
                primary?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080));
        }
        catch
        {
            throw new InvalidOperationException("No monitors were detected.");
        }
    }

    private static string TryGetFriendlyMonitorName(string deviceName, IReadOnlyDictionary<string, string> wmiNames)
    {
        var adapter = new DisplayDevice();
        adapter.cb = Marshal.SizeOf(adapter);
        if (!EnumDisplayDevices(deviceName, 0, ref adapter, 0))
        {
            return string.Empty;
        }

        var monitorId = ExtractMonitorHardwareId(adapter.DeviceId);
        if (!string.IsNullOrWhiteSpace(monitorId)
            && wmiNames.TryGetValue(monitorId, out var wmiName)
            && !string.IsNullOrWhiteSpace(wmiName))
        {
            return wmiName;
        }

        return string.IsNullOrWhiteSpace(adapter.DeviceString) ? string.Empty : adapter.DeviceString.Trim();
    }

    private static Dictionary<string, string> GetWmiMonitorNames()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName, UserFriendlyName FROM WmiMonitorID");
            foreach (ManagementObject monitor in searcher.Get())
            {
                var instanceName = monitor["InstanceName"]?.ToString() ?? string.Empty;
                var hardwareId = ExtractMonitorHardwareId(instanceName);
                var friendlyName = DecodeWmiString(monitor["UserFriendlyName"] as ushort[]);
                if (!string.IsNullOrWhiteSpace(hardwareId) && !string.IsNullOrWhiteSpace(friendlyName))
                {
                    names[hardwareId] = friendlyName;
                }
            }
        }
        catch
        {
            return names;
        }

        return names;
    }

    private static string ExtractMonitorHardwareId(string value)
    {
        var parts = value.Split(new[] { '\\', '#' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? $"{parts[0]}\\{parts[1]}" : string.Empty;
    }

    private static string DecodeWmiString(ushort[]? values)
    {
        if (values is null)
        {
            return string.Empty;
        }

        var chars = values
            .TakeWhile(value => value != 0)
            .Select(value => (char)value)
            .ToArray();
        return new string(chars).Trim();
    }

    public void Dispose()
    {
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }
}
