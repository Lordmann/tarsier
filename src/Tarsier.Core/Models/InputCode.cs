namespace Tarsier.Core.Models;

/// <summary>
/// Identifies one key, mouse button or wheel notch by its Windows virtual-key code. Mouse buttons have codes of
/// their own; the wheel does not, so two are reserved for it above the virtual-key range.
/// </summary>
public static class InputCode
{
    public const uint LeftButton = 0x01;
    public const uint RightButton = 0x02;
    public const uint MiddleButton = 0x04;
    public const uint XButton1 = 0x05;
    public const uint XButton2 = 0x06;
    public const uint Shift = 0x10;
    public const uint Control = 0x11;
    public const uint Alt = 0x12;
    public const uint Windows = 0x5B;
    public const uint WheelUp = 0x100;
    public const uint WheelDown = 0x101;

    public static bool IsWheel(uint code) => code is WheelUp or WheelDown;

    public static bool IsMouseButton(uint code) => code is LeftButton or RightButton or MiddleButton or XButton1 or XButton2;

    /// <summary>Folds left/right variants of the modifier keys into one code so either side satisfies a combination.</summary>
    public static uint Normalize(uint code) => code switch
    {
        0xA0 or 0xA1 => Shift,
        0xA2 or 0xA3 => Control,
        0xA4 or 0xA5 => Alt,
        0x5C => Windows,
        _ => code
    };
}
