using System;
using System.Runtime.InteropServices;

namespace obs_net{
    using obs_output_t = IntPtr;
    using obs_data_t = IntPtr;

    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_output_t obs_output_create(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            obs_data_t settings, obs_data_t hotkey_data);

        [DllImport(importLibrary, CallingConvention = importCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool obs_output_start(obs_output_t output);

        /// <summary>
        /// https://obsproject.com/docs/reference-outputs.html#c.obs_output_get_last_error
        /// </summary>
        /// <returns>Gets the translated error message that is presented to a user in case of disconnection, inability to connect, etc.</returns>
        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public static extern string obs_output_get_last_error(obs_output_t output);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_update#c.obs_output_update</para>
        /// <para>Updates the settings for this output context.</para>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="settings"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_update(obs_output_t output, obs_data_t settings);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_update#c.obs_output_set_mixers</para>
        /// <para>Sets the current audio mixer for non-encoded outputs. For multi-track outputs, this would be the equivalent of setting the mask only for the specified mixer index.</para>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="mixers"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_set_mixers(obs_output_t output, UIntPtr mixers);
    }
}
