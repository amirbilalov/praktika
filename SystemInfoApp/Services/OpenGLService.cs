using System.Runtime.InteropServices;

namespace SystemInfoApp.Services;

internal static class OpenGLService
{
    [DllImport("gdi32.dll")] private static extern int  ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);
    [DllImport("gdi32.dll")] private static extern bool SetPixelFormat   (IntPtr hdc, int fmt, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int w, int h,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")] private static extern bool   DestroyWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetDC        (IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int    ReleaseDC    (IntPtr hWnd, IntPtr hDC);

    [DllImport("opengl32.dll")] private static extern IntPtr wglCreateContext(IntPtr hdc);
    [DllImport("opengl32.dll")] private static extern bool   wglMakeCurrent  (IntPtr hdc, IntPtr hglrc);
    [DllImport("opengl32.dll")] private static extern bool   wglDeleteContext (IntPtr hglrc);
    [DllImport("opengl32.dll")] private static extern IntPtr glGetString      (uint name);

    [StructLayout(LayoutKind.Sequential)]
    private struct PIXELFORMATDESCRIPTOR
    {
        public ushort nSize, nVersion;
        public uint   dwFlags;
        public byte   iPixelType, cColorBits,
                      cRedBits,   cRedShift,
                      cGreenBits, cGreenShift,
                      cBlueBits,  cBlueShift,
                      cAlphaBits, cAlphaShift,
                      cAccumBits, cAccumRedBits,
                      cAccumGreenBits, cAccumBlueBits,
                      cAccumAlphaBits, cDepthBits,
                      cStencilBits, cAuxBuffers,
                      iLayerType, bReserved;
        public uint   dwLayerMask, dwVisibleMask, dwDamageMask;
    }

    private const uint GL_VERSION         = 0x1F02;
    private const uint PFD_DRAW_TO_WINDOW = 0x00000004;
    private const uint PFD_SUPPORT_OPENGL = 0x00000020;
    private const uint PFD_DOUBLEBUFFER   = 0x00000001;
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const uint WS_EX_TOOLWINDOW   = 0x00000080;

    public static string GetVersion()
    {
        IntPtr hwnd = CreateWindowEx(
            WS_EX_TOOLWINDOW, "Static", "GL_Probe",
            WS_OVERLAPPEDWINDOW,
            0, 0, 1, 1,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        if (hwnd == IntPtr.Zero)
            return FallbackFromRegistry();

        IntPtr hdc   = IntPtr.Zero;
        IntPtr hglrc = IntPtr.Zero;

        try
        {
            hdc = GetDC(hwnd);
            if (hdc == IntPtr.Zero) return FallbackFromRegistry();

            var pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize      = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion   = 1,
                dwFlags    = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
                iPixelType = 0,
                cColorBits = 32,
                cDepthBits = 24,
                iLayerType = 0
            };

            int fmt = ChoosePixelFormat(hdc, ref pfd);
            if (fmt == 0 || !SetPixelFormat(hdc, fmt, ref pfd))
                return FallbackFromRegistry();

            hglrc = wglCreateContext(hdc);
            if (hglrc == IntPtr.Zero) return FallbackFromRegistry();

            if (!wglMakeCurrent(hdc, hglrc)) return FallbackFromRegistry();

            IntPtr versionPtr = glGetString(GL_VERSION);
            return versionPtr != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(versionPtr) ?? "Unknown"
                : "Unknown";
        }
        catch
        {
            return FallbackFromRegistry();
        }
        finally
        {
            if (hglrc != IntPtr.Zero)
            {
                wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                wglDeleteContext(hglrc);
            }
            if (hdc  != IntPtr.Zero) ReleaseDC(hwnd, hdc);
            if (hwnd != IntPtr.Zero) DestroyWindow(hwnd);
        }
    }

    private static string FallbackFromRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\OpenGLDrivers");

            if (key is not null)
            {
                var sub = key.GetSubKeyNames().FirstOrDefault();
                if (sub is not null)
                {
                    using var subKey = key.OpenSubKey(sub);
                    var drvVersion = subKey?.GetValue("Version")?.ToString();
                    if (!string.IsNullOrEmpty(drvVersion))
                        return $"(реестр) {drvVersion}";
                }
            }
        }
        catch { }

        return "Не удалось определить";
    }
}
