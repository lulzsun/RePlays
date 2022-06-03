using System;
using System.Runtime.InteropServices;

namespace obs_net {
    using obs_data_t = IntPtr;
    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern obs_data_t obs_data_create();

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern void obs_data_set_string(
            obs_data_t data,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string val);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern void obs_data_set_bool(
            obs_data_t data,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            bool val);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern void obs_data_set_int(
            obs_data_t data,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            uint val);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-settings.html?highlight=obs_data_create#c.obs_data_release</para>
        /// <para>Releases a reference to a data object.</para>
        /// </summary>
        /// <param name="data"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_data_release(
            obs_data_t data);
    }
}
