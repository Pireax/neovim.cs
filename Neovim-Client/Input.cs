using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Neovim
{
    public static class Input
    {
        private static readonly Dictionary<Key, string> InvisibleKeys = new Dictionary<Key, string>()
        { 
            { Key.Escape, "<Esc>" }, { Key.F1, "<F1>" }, { Key.F2, "<F2>" }, { Key.F3, "<F3>" }, { Key.F4, "<F4>" }, { Key.F5, "<F5>" }, { Key.F6, "<F6>" },
            { Key.F7, "<F7>" }, { Key.F8, "<F8>" }, { Key.F9, "<F9>" }, { Key.F10, "<F10>" }, { Key.F11, "<F11>" }, { Key.F12, "<F12>" }, { Key.Back, "<BS>"},
            { Key.Tab, "<Tab>" }, { Key.Enter, "<Enter>" }, { Key.Up, "<Up>" }, { Key.Space, "<Space>" }, { Key.Left, "<Left>" },
            { Key.Down, "<Down>" }, { Key.Right, "<Right>" }
        };


    [DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchbuf, uint wFlags);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        private enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3
        }

        public static string Encode(Key key)
        {
            char ch = '\0';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);

            uint scanCode = MapVirtualKey((uint) virtualKey, MapType.MAPVK_VK_TO_VSC);

            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint) virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);

            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    ch = stringBuilder[0];
                    break;
                default:
                    ch = stringBuilder[0];
                    break;
            }

            string keys = "";
            // Not a writable key
            if (ch == '\0')
            {
                if (InvisibleKeys.TryGetValue(key, out keys))
                    return keys;

                return null;
            }

            return ch.ToString();
        }
    }
}
