using Tarsier.Core.Models;

namespace Tarsier.Core.Tests.Models;

public class HotkeyTests
{
    private const uint F4 = 0x73;
    private const uint T = 0x54;

    [Fact]
    public void TheOrderInputsWerePressedInDoesNotMatter()
    {
        Assert.Equal(new Hotkey(F4, T), new Hotkey(T, F4));
        Assert.Equal(new Hotkey(F4, T).GetHashCode(), new Hotkey(T, F4).GetHashCode());
    }

    [Fact]
    public void EitherSideOfAModifierSatisfiesTheCombination()
    {
        Assert.Equal(new Hotkey(InputCode.Control, T), new Hotkey(0xA2, T));
        Assert.Equal(new Hotkey(InputCode.Control, T), new Hotkey(0xA3, T));
    }

    [Fact]
    public void MouseButtonsAndTheWheelCombineWithKeys()
    {
        var hotkey = new Hotkey(F4, InputCode.WheelUp);

        Assert.Equal(new[] { F4, InputCode.WheelUp }, hotkey.Inputs);
        Assert.NotEqual(hotkey, new Hotkey(F4, InputCode.WheelDown));
    }

    [Fact]
    public void AHotkeyNeedsAnInputAndAtMostOneWheelDirection()
    {
        Assert.Throws<ArgumentException>(() => new Hotkey());
        Assert.Throws<ArgumentException>(() => new Hotkey(InputCode.WheelUp, InputCode.WheelDown));
    }

    [Fact]
    public void TheOriginalModifierFormatMapsOntoModifierKeyCodes()
    {
        var hotkey = Hotkey.FromLegacy(HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x77);

        Assert.Equal(new Hotkey(InputCode.Alt, InputCode.Shift, 0x77), hotkey);
    }
}
