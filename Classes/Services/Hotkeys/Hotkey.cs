using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace RePlays.Classes.Services.Hotkeys
{
    public abstract class Hotkey
    {
        protected Keys _keybind;
        public Keys Keybind => _keybind;

        protected Hotkey()
        {
            SetKeybind();
        }

        public static Keys ParseKeys(string[] keys)
        {
            Keys keybind = Keys.None;

            for (int i = 0; i < keys.Length; i++)
            {
                Keys key;
                Enum.TryParse(keys[i], out key);
                keybind |= key;
            }

            return keybind;
        }

        public abstract void Action();

        protected abstract void SetKeybind();
    }
}
