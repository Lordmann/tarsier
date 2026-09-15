namespace Tarsier.Core.Display;

/// <summary>
/// Colour saturation for a monitor. This is deliberately separate from <see cref="IDisplayController"/>: the
/// gamma ramp is three independent per-channel curves and can never express saturation, which has to mix the
/// channels. It reaches the display through a different path, and only some hardware offers it at all, so an
/// implementation is free to do nothing.
/// </summary>
public interface IVibranceController
{
    /// <summary>False when no display on this machine can be driven, which is what the UI hides the slider on.</summary>
    bool IsSupported { get; }

    void Apply(string monitorId, int vibrance);

    /// <summary>Puts the monitor back to the level it had before this process first touched it.</summary>
    void Reset(string monitorId);

    void ResetAll();
}
