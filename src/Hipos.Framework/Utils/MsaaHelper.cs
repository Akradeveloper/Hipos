using Accessibility;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Serilog;
using System.Runtime.InteropServices;

namespace Hipos.Framework.Utils;

/// <summary>
/// Helper for Microsoft Active Accessibility (MSAA) interactions.
/// MSAA is accessed through native window handles, typically obtained from FlaUI Window objects.
/// FlaUI is used for window management, while MSAA (using these handles) is used for UI element interactions.
/// </summary>
public static class MsaaHelper
{
    private const int ObjIdClient = unchecked((int)0xFFFFFFFC);

    public static MsaaElement GetRootElement(IntPtr windowHandle)
    {
        var iaGuid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
        var hr = AccessibleObjectFromWindow(windowHandle, ObjIdClient, ref iaGuid, out var accessible);
        if (hr != 0 || accessible == null)
        {
            throw new InvalidOperationException(
                $"No se pudo obtener IAccessible desde el handle {windowHandle}. HRESULT: 0x{hr:X8}");
        }

        return new MsaaElement(accessible, 0);
    }

    public static MsaaElement FindByNamePath(IntPtr windowHandle, params string[] namePath)
    {
        var root = GetRootElement(windowHandle);
        return FindByNamePath(root, namePath);
    }

    public static MsaaElement FindByNamePath(MsaaElement root, params string[] namePath)
    {
        if (namePath.Length == 0)
        {
            return root;
        }

        var current = root;
        foreach (var name in namePath)
        {
            var next = current.FindChildByName(name);
            if (next == null)
            {
                throw new InvalidOperationException(
                    $"No se encontró elemento MSAA con nombre '{name}'.");
            }

            current = next;
        }

        return current;
    }

    public static bool ExistsByNamePath(IntPtr windowHandle, params string[] namePath)
    {
        try
        {
            _ = FindByNamePath(windowHandle, namePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(
        IntPtr hwnd,
        int dwObjectId,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IAccessible? ppvObject);

    [DllImport("oleacc.dll")]
    private static extern int AccessibleChildren(
        IAccessible paccContainer,
        int iChildStart,
        int cChildren,
        [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren,
        out int pcObtained);

    public sealed class MsaaElement
    {
        private const int ChildIdSelf = 0;
        private const int SelFlagTakeFocus = 0x1;
        
        // MSAA State flags
        private const int STATE_SYSTEM_UNAVAILABLE = 0x1;

        public IAccessible Accessible { get; }
        public int ChildId { get; }

        public MsaaElement(IAccessible accessible, int childId)
        {
            Accessible = accessible ?? throw new ArgumentNullException(nameof(accessible));
            ChildId = childId;
        }

        public string? GetName()
        {
            try
            {
                return Accessible.get_accName(ChildId == ChildIdSelf ? 0 : ChildId);
            }
            catch
            {
                return null;
            }
        }

        public MsaaElement? FindChildByName(string name)
        {
            foreach (var child in GetChildren())
            {
                var childName = child.GetName();
                if (!string.IsNullOrEmpty(childName) &&
                    childName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        public void SetText(string text)
        {
            try
            {
                Accessible.set_accValue(ChildId == ChildIdSelf ? 0 : ChildId, text);
                return;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "MSAA set_accValue falló, usando foco y teclado");
            }

            try
            {
                Accessible.accSelect(SelFlagTakeFocus, ChildId == ChildIdSelf ? 0 : ChildId);
                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
                Keyboard.Type(VirtualKeyShort.DELETE);
                Keyboard.Type(text);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudo establecer texto con MSAA");
                throw;
            }
        }

        public void Click()
        {
            try
            {
                Accessible.accDoDefaultAction(ChildId == ChildIdSelf ? 0 : ChildId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudo ejecutar acción por defecto con MSAA");
                throw;
            }
        }

        /// <summary>
        /// Gets all child elements of this MSAA element.
        /// </summary>
        /// <returns>Enumerable collection of child elements</returns>
        public IEnumerable<MsaaElement> GetAllChildren()
        {
            return GetChildren();
        }

        /// <summary>
        /// Gets the MSAA state of this element.
        /// </summary>
        /// <returns>State flags as integer</returns>
        public int GetState()
        {
            try
            {
                object? stateObj = Accessible.get_accState(ChildId == ChildIdSelf ? 0 : ChildId);
                if (stateObj is int state)
                {
                    return state;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "No se pudo obtener estado MSAA del elemento");
                return 0;
            }
        }

        /// <summary>
        /// Checks if this element has the "unavailable" state flag.
        /// </summary>
        /// <returns>True if the element is unavailable, false otherwise</returns>
        public bool IsUnavailable()
        {
            var state = GetState();
            return (state & STATE_SYSTEM_UNAVAILABLE) != 0;
        }

        private IEnumerable<MsaaElement> GetChildren()
        {
            if (ChildId != ChildIdSelf)
            {
                yield break;
            }

            int childCount;
            try
            {
                childCount = Accessible.accChildCount;
            }
            catch
            {
                yield break;
            }

            if (childCount <= 0)
            {
                yield break;
            }

            var children = new object[childCount];
            var hr = AccessibleChildren(Accessible, 0, childCount, children, out var obtained);
            if (hr != 0 || obtained <= 0)
            {
                yield break;
            }

            for (var i = 0; i < obtained; i++)
            {
                var child = children[i];
                if (child is IAccessible childAcc)
                {
                    yield return new MsaaElement(childAcc, ChildIdSelf);
                }
                else if (child is int childId)
                {
                    yield return new MsaaElement(Accessible, childId);
                }
            }
        }
    }
}