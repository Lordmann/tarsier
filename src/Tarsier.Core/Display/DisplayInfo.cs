namespace Tarsier.Core.Display;

/// <summary>A monitor the app can drive. <paramref name="Id"/> is the stable GDI device name.</summary>
public sealed record DisplayInfo(string Id, string Name, bool IsPrimary);
