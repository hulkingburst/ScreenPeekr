using System.Drawing;

namespace ScreenPeekr.Models;

internal sealed record MonitorInfo(string Id, string DisplayName, Rectangle Bounds);
