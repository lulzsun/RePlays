#if WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using RePlays.Services;

namespace RePlays.Classes.Services.Hotkeys
{
    public abstract class Hotkey
    {
        protected Keys _keybind;
        public Keys Keybind => _keybind;

        private static Dictionary<string, string[]> defaultKeybindings = new Dictionary<string, string[]>() {
            { "StartStopRecording", new string[] { "Control", "F9" } },
            { "CreateBookmark", new string[] { "F8" } }
        };

        protected Hotkey()
        {
            SetKeybind();
        }

        public static Keys ParseKeys(string keyReference, string[] keys)
        {
            Keys keybind = Keys.None;

            if (keys == null) keys = AddMissingHotkey(keyReference);
            

            for (int i = 0; i < keys.Length; i++)
            {
                Keys key;
                Enum.TryParse(keys[i], out key);
                keybind |= key;
            }

            return keybind;
        }

        private static string[] AddMissingHotkey(string keyReference)
        {
            defaultKeybindings.TryGetValue(keyReference, out string[] keys);
            SettingsService.Settings.keybindings.Add(keyReference, keys);
            SettingsService.SaveSettings();
            return keys;
        }

        public abstract void Action();

        //TODO: Refactor
        protected abstract void SetKeybind();
    }
}
#endif