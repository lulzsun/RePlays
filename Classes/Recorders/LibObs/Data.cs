using System;
using System.Runtime.InteropServices;

namespace obs_net {
    using obs_data_array_t = IntPtr;
    using obs_data_t = IntPtr;
    using size_t = UIntPtr;

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

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_data_array_t obs_data_get_array(
            obs_data_t data,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern void obs_data_set_array(
            obs_data_t data,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            obs_data_array_t array);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern obs_data_array_t obs_data_array_create();

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-settings.html?highlight=obs_data_create#c.obs_data_release</para>
        /// <para>Releases a reference to a data object.</para>
        /// </summary>
        /// <param name="data"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_data_release(
            obs_data_t data);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern size_t obs_data_array_push_back(
            obs_data_array_t array,
            obs_data_t obj);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_data_array_release(obs_data_array_t array);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern string obs_data_get_json(obs_data_t data);
    }
}
