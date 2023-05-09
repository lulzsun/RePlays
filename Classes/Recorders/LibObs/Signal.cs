using System;
using System.Runtime.InteropServices;

namespace obs_net {
    using calldata_t = IntPtr;
    using signal_handler_t = IntPtr;

    public partial class Obs {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        public delegate void signal_callback_t(IntPtr data, calldata_t cd);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern void signal_handler_connect(signal_handler_t handler, string signal, signal_callback_t callback, IntPtr data);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern void signal_handler_disconnect(signal_handler_t handler, string signal, signal_callback_t callback, IntPtr data);
    }
}
