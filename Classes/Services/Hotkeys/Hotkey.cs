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

        protected Hotkey()
        {
            SetKeybind();
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(Keys nVirtKey);
        public static Keys ParseKeys(string[] keys)
        {
            Keys keybind = Keys.None;

            for (int i = 0; i < keys.Length; i++) {
                Keys key = Keys.None;
                Enum.TryParse(keys[i], out key);

                if(i == 0) keybind = key;
                else keybind |= key;
            }

            return keybind;
        }

        public abstract void Action();

        public bool IsPressed()
        {
            int state = GetKeyState(_keybind);
            if (state > 1 || state < -1) return true;
            return false;
        }
        protected abstract void SetKeybind();
    }
}
