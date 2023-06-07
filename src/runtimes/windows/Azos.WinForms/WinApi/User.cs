/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Azos.WinApi
{


  /// <summary>
  /// Provides managed wrappers to Windows User.dll
  /// </summary>
  public static class UserApi
  {
    private const string USER32 = "USER32.DLL";

    [DllImport(USER32)]
    public static extern IntPtr SetCapture(IntPtr hWnd);

    [DllImport(USER32)]
    public static extern bool ReleaseCapture();



    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;

      public RECT(int left_, int top_, int right_, int bottom_)
      {
        Left = left_;
        Top = top_;
        Right = right_;
        Bottom = bottom_;
      }

      public override string ToString()
      {
        return String.Format("({0};{1}) ({2};{3})", Left, Top, Right, Bottom);
      }

      public static RECT FromRectangle(Rectangle rectangle)
      {
        return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
      }

      public Rectangle ToRectangle()
      {
        return new Rectangle(
            this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
      }
    }



    public delegate int EnumDelegate(IntPtr hwnd, int LParam);

    [DllImport(USER32)]
    public static extern int EnumWindows(EnumDelegate d, int lParm);

    [DllImport(USER32)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport(USER32)]
    public extern static int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);

    public const int SW_SCROLLCHILDREN = 0x01;
    public const int SW_INVALIDATE     = 0x02;
    public const int SW_ERASE          = 0x04;
    public const int SW_SMOOTHSCROLL   = 0x10;

    public const int WM_SETTEXT        = 0X000C;
    public const int WM_KEYDOWN        = 0x0100;
    public const int WM_KEYUP          = 0x0101;

    [DllImport(USER32)]
    public static extern int ScrollWindowEx(
        IntPtr hWnd,
        int dx, int dy,
        ref RECT lprcScroll,
        ref RECT lprcClip,
        int hrgnUpdate,
        ref RECT lprcUpdate,
        int fuScroll);

    [DllImport(USER32)]
    public static extern bool MessageBeep(int uType);

    [DllImport(USER32)]
    public static extern IntPtr GetFocus();



    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

    [DllImport("user32.dll", EntryPoint="FindWindow", SetLastError = true)]
    public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

    // You can also call FindWindow(default(string), lpWindowName) or FindWindow((string)null, lpWindowName)


    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int SendMessage(HandleRef hWnd,  uint uMsg, int wParam, string lParam);
    [DllImport("user32.dll")]
    public static extern int SendMessage(HandleRef hWnd,  uint uMsg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern int SendMessage(HandleRef hWnd,  uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Auto)]
    public static extern uint RegisterWindowMessage(string lpString);

  }

}