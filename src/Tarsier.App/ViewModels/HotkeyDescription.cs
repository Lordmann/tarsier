using System.Windows.Input;
using Tarsier.Core.Models;

namespace Tarsier.App.ViewModels;

public static class HotkeyDescription
{
    public const string Unbound = "Not set";

    public static string Describe(Hotkey? hotkey) => hotkey is null
        ? Unbound
        : string.Join(" + ", hotkey.Inputs.OrderBy(Rank).ThenBy(code => code).Select(Name));

    /// <summary>Modifiers read first and the wheel last, the way the combination is pressed.</summary>
    private static int Rank(uint code) => code switch
    {
        InputCode.Control => 0,
        InputCode.Alt => 1,
        InputCode.Shift => 2,
        InputCode.Windows => 3,
        InputCode.WheelUp or InputCode.WheelDown => 5,
        _ => 4
    };

    private static string Name(uint code) => code switch
    {
        InputCode.Control => "Ctrl",
        InputCode.Alt => "Alt",
        InputCode.Shift => "Shift",
        InputCode.Windows => "Win",
        InputCode.LeftButton => "Left Click",
        InputCode.RightButton => "Right Click",
        InputCode.MiddleButton => "Middle Click",
        InputCode.XButton1 => "Mouse 4",
        InputCode.XButton2 => "Mouse 5",
        InputCode.WheelUp => "Wheel Up",
        InputCode.WheelDown => "Wheel Down",
        _ => KeyInterop.KeyFromVirtualKey((int)code).ToString()
    };
}
