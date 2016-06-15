using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SpotifyManager
{
    internal class Win32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        internal class Constants
        {
            internal const uint WM_APPCOMMAND = 0x0319;
        }
    }

    public enum SpotifyAction : long
    {
        PlayPause = 917504, //E0000
        Mute = 524288, //80000
        VolumeDown = 589824,//90000
        VolumeUp = 655360,//A0000
        Stop = 851968,//D0000
        PreviousTrack = 786432, //C0000
        NextTrack = 720896 // B0000
    }
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        public static bool shiftDown = false, altDown = false, ctrlDown = false;
        public static bool shiftActive, altActive, ctrlActive;
        public static int numactive = 0;
        public static IntPtr hwndSpotify = FindWindow("SpotifyMainWindow", null);
        public static Dictionary<Keys, Commands> bindings = new Dictionary<Keys, Commands>();
        public static Dictionary<Commands, SpotifyAction> commandDict = new Dictionary<Commands, SpotifyAction>();
        public enum Commands
        {
            NEXT, PREVIOUS, VOLUMEUP, VOLUMEDOWN, PAUSEPLAY, MUTE
        }
        static void Main(String[] args)
        {

            StreamReader sr = new StreamReader("appconfig.txt");
            string wholeConfig = sr.ReadToEnd();
            sr.Close();
            setVars(wholeConfig);
            _hookID = SetHook(_proc);
            Thread temp = new Thread(new ThreadStart(running));
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        public static void running()
        {
            while (true)
            {
                Thread.Sleep(1000000);
            }
        }
        public static void setVars(string s)
        {
            commandDict.Add(Commands.NEXT, SpotifyAction.NextTrack);
            commandDict.Add(Commands.PREVIOUS, SpotifyAction.PreviousTrack);
            commandDict.Add(Commands.MUTE, SpotifyAction.Mute);
            commandDict.Add(Commands.PAUSEPLAY, SpotifyAction.PlayPause);
            commandDict.Add(Commands.VOLUMEDOWN, SpotifyAction.VolumeDown);
            commandDict.Add(Commands.VOLUMEUP, SpotifyAction.VolumeUp);
            if (s.ToLower().Contains("ctrl-enable"))
            {
                ctrlActive = true;
                numactive++;
            }
            if (s.ToLower().Contains("shift-enable"))
            {
                shiftActive = true;
                numactive++;
            }
            if (s.ToLower().Contains("alt-enable"))
            {
                altActive = true;
                numactive++;
            }
            s = s.Substring(s.IndexOf("***BINDINGS***") + "***BINDING***".Length + 2);
            bindings.Add((Keys)Enum.Parse(typeof(Keys), trimString(s, s.IndexOf("NEXT-") + 5, s.IndexOf("\r", s.IndexOf("NEXT")))), Commands.NEXT);
            bindings.Add((Keys)Enum.Parse(typeof(Keys), trimString(s, s.IndexOf("PREVIOUS-") + 9, s.IndexOf("\r", s.IndexOf("PREVIOUS")))), Commands.PREVIOUS);
            bindings.Add((Keys)Enum.Parse(typeof(Keys), trimString(s, s.IndexOf("VOLUMEUP-") + 9, s.IndexOf("\r", s.IndexOf("VOLUMEUP")))), Commands.VOLUMEUP);
            bindings.Add((Keys)Enum.Parse(typeof(Keys), trimString(s, s.IndexOf("VOLUMEDOWN-") + 11, s.IndexOf("\r", s.IndexOf("VOLUMEDOWN")))), Commands.VOLUMEDOWN);
            bindings.Add((Keys)Enum.Parse(typeof(Keys), trimString(s, s.IndexOf("PAUSEPLAY-") + 10, s.IndexOf("\r", s.IndexOf("PAUSEPLAY")))), Commands.PAUSEPLAY);
            bindings.Add((Keys)Enum.Parse(typeof(Keys), trimString(s, s.IndexOf("MUTE-") + 5, s.IndexOf("\r", s.IndexOf("MUTE") + 3))), Commands.MUTE);
        }
        public static string trimString(string s, int start, int end)
        {
            return s.Substring(start, end - start);
        }
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        //update
        private static IntPtr HookCallback(
           int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && ((wParam == (IntPtr)WM_KEYDOWN) || wParam == (IntPtr)0x104))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (((Keys)vkCode).Equals(Keys.RShiftKey) || ((Keys)vkCode).Equals(Keys.LShiftKey))
                {
                    shiftDown = true;
                }
                if (((Keys)vkCode).Equals(Keys.RMenu) || ((Keys)vkCode).Equals(Keys.LMenu))
                {
                    altDown = true;
                }
                if ((((Keys)vkCode).Equals(Keys.RControlKey) || ((Keys)vkCode).Equals(Keys.LControlKey)))
                {
                    ctrlDown = true;
                }
                else if (bindings.ContainsKey((Keys)vkCode) || (Keys)vkCode == Keys.X)
                {

                    List<bool> status = new List<bool>();
                    if (shiftActive && shiftDown) status.Add(true);
                    if (ctrlActive && ctrlDown) status.Add(true);
                    if (altActive && altDown) status.Add(true);

                    if (!status.Contains(false) && status.Count >= numactive)
                    {
                        if ((Keys)vkCode == Keys.X) Application.Exit();
                        else
                        {
                            Commands c = bindings[(Keys)vkCode];
                            Win32.SendMessage(hwndSpotify, Win32.Constants.WM_APPCOMMAND,
                                IntPtr.Zero, new IntPtr((long)commandDict[c]));
                        }
                    }
                }
            }

            #region modifierKeyLogic
            else if (nCode >= 0 && ((wParam == (IntPtr)WM_KEYUP) || wParam == (IntPtr)0x105))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if ((((Keys)vkCode).Equals(Keys.RShiftKey) || ((Keys)vkCode).Equals(Keys.LShiftKey)))
                {
                    shiftDown = false;
                }
                if ((((Keys)vkCode).Equals(Keys.RMenu) || ((Keys)vkCode).Equals(Keys.LMenu)))
                {
                    altDown = false;
                }
                if ((((Keys)vkCode).Equals(Keys.RControlKey) || ((Keys)vkCode).Equals(Keys.LControlKey)))
                {
                    ctrlDown = false;
                }
            }
            #endregion
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}
