using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NamuWikiViewer.Windows.Extensions;

internal static partial class WebView2Extensions
{
    [LibraryImport("user32.dll")]
    private static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_F = 0x46;
    private const byte VK_ESC = 0x1B;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    internal static void ShowFindDialog(this WebView2 webView)
    {
        webView.Focus(FocusState.Programmatic);

        // Needs a slight delay to ensure the WebView2 has focus before sending the key events
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_F, 0, 0, UIntPtr.Zero);
            keybd_event(VK_F, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        });
    }

    internal static void HideFindDialog(this WebView2 webView)
    {
        webView.Focus(FocusState.Programmatic);

        // Needs a slight delay to ensure the WebView2 has focus before sending the key events
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            keybd_event(VK_ESC, 0, 0, UIntPtr.Zero);
            keybd_event(VK_ESC, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        });
    }
}
