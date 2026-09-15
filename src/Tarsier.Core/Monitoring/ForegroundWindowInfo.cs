namespace Tarsier.Core.Monitoring;

/// <summary>The executable owning the foreground window and the monitor that window sits on.</summary>
public sealed record ForegroundWindowInfo(string ExecutableName, string MonitorId);
