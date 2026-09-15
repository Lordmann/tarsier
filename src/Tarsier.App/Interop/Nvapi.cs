using System.Runtime.InteropServices;
using System.Text;

namespace Tarsier.App.Interop;

/// <summary>A monitor's saturation level as the driver reports it, on the driver's own scale.</summary>
internal readonly record struct VibranceRange(int Current, int Min, int Max, int Default);

/// <summary>
/// The digital vibrance entry points. NVIDIA ships these in nvapi64.dll but not in its public headers, so they
/// are reached by asking nvapi_QueryInterface for a function id rather than by name. Everything degrades to
/// unavailable instead of throwing, because the library is simply absent without an NVIDIA driver.
/// </summary>
internal sealed class Nvapi
{
    private const uint InitializeId = 0x0150E828;
    private const uint UnloadId = 0xD22BDD7E;
    private const uint EnumDisplayHandleId = 0x9ABDD40D;
    private const uint GetDisplayNameId = 0x22A78B05;
    private const uint GetDvcInfoExId = 0x0E45002D;
    private const uint SetDvcLevelExId = 0x4A82C2B1;

    private const int Ok = 0;
    private const int MaxDisplays = 16;
    private const int ShortStringMax = 64;
    private const uint PrimaryOutput = 0;

    [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr QueryInterface(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int UnloadDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EnumDisplayHandleDelegate(int index, out IntPtr handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetDisplayNameDelegate(IntPtr handle, StringBuilder name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetDvcInfoExDelegate(IntPtr handle, uint outputId, ref DvcInfoEx info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SetDvcLevelExDelegate(IntPtr handle, uint outputId, ref DvcInfoEx info);

    /// <summary>NV_DISPLAY_DVC_INFO_EX. The setter takes this same struct, not a truncated one.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DvcInfoEx
    {
        public uint Version;
        public int CurrentLevel;
        public int MinLevel;
        public int MaxLevel;
        public int DefaultLevel;
    }

    private readonly EnumDisplayHandleDelegate _enumerate;
    private readonly GetDisplayNameDelegate _getName;
    private readonly GetDvcInfoExDelegate _getVibrance;
    private readonly SetDvcLevelExDelegate _setVibrance;
    private readonly UnloadDelegate? _unload;

    private Nvapi(
        EnumDisplayHandleDelegate enumerate,
        GetDisplayNameDelegate getName,
        GetDvcInfoExDelegate getVibrance,
        SetDvcLevelExDelegate setVibrance,
        UnloadDelegate? unload)
    {
        _enumerate = enumerate;
        _getName = getName;
        _getVibrance = getVibrance;
        _setVibrance = setVibrance;
        _unload = unload;
    }

    /// <summary>Null when the driver is absent or does not offer vibrance control.</summary>
    public static Nvapi? TryLoad()
    {
        try
        {
            if (Bind<InitializeDelegate>(InitializeId) is not { } initialize || initialize() != Ok)
            {
                return null;
            }

            if (Bind<EnumDisplayHandleDelegate>(EnumDisplayHandleId) is not { } enumerate ||
                Bind<GetDisplayNameDelegate>(GetDisplayNameId) is not { } getName ||
                Bind<GetDvcInfoExDelegate>(GetDvcInfoExId) is not { } getVibrance ||
                Bind<SetDvcLevelExDelegate>(SetDvcLevelExId) is not { } setVibrance)
            {
                return null;
            }

            return new Nvapi(enumerate, getName, getVibrance, setVibrance, Bind<UnloadDelegate>(UnloadId));
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>Maps each NVIDIA-driven monitor's GDI device name, such as <c>\\.\DISPLAY1</c>, to its handle.</summary>
    public IReadOnlyDictionary<string, IntPtr> EnumerateDisplays()
    {
        var displays = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < MaxDisplays; index++)
        {
            if (_enumerate(index, out var handle) != Ok)
            {
                break;
            }

            var name = new StringBuilder(ShortStringMax);
            if (_getName(handle, name) == Ok)
            {
                displays[name.ToString()] = handle;
            }
        }

        return displays;
    }

    public bool TryGetVibrance(IntPtr handle, out VibranceRange range)
    {
        var info = NewInfo();
        if (_getVibrance(handle, PrimaryOutput, ref info) != Ok)
        {
            range = default;
            return false;
        }

        range = new VibranceRange(info.CurrentLevel, info.MinLevel, info.MaxLevel, info.DefaultLevel);
        return true;
    }

    public bool SetVibrance(IntPtr handle, int level)
    {
        var info = NewInfo();
        info.CurrentLevel = level;
        return _setVibrance(handle, PrimaryOutput, ref info) == Ok;
    }

    public void Unload() => _unload?.Invoke();

    private static DvcInfoEx NewInfo() => new()
    {
        Version = (uint)Marshal.SizeOf<DvcInfoEx>() | (1u << 16)
    };

    private static T? Bind<T>(uint id) where T : Delegate
    {
        var pointer = QueryInterface(id);
        return pointer == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(pointer);
    }
}
