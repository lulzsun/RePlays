using System;
using System.Runtime.InteropServices;

namespace obs_net {
    using obs_data_t = IntPtr;
    using obs_source_t = IntPtr;

    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public static extern string obs_source_get_display_name(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_source_t obs_source_create(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            obs_data_t settings, obs_data_t hotkey_data);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_source_release(obs_source_t source);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_source_remove(obs_source_t source);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-sources.html?highlight=audio%20mixer#c.obs_source_set_audio_mixers</para>
        /// <para>
        /// Sets/gets the audio mixer channels that a source outputs to, depending on what bits are set. 
        /// Audio mixers allow filtering specific using multiple audio encoders to mix different sources 
        /// together depending on what mixer channel they’re set to.
        /// </para>
        /// <para>For example, to output to mixer 1 and 3, you would perform a bitwise OR on bits 0 and 2: (1<<0) | (1<<2), or 0x5.</para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mixers"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_source_set_audio_mixers(obs_source_t source, uint mixers);


        /// <summary>
        /// https://obsproject.com/docs/reference-sources.html?highlight=obs_source_get_flags#c.obs_source_get_flags
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern uint obs_source_get_flags(obs_source_t source);

        /// <summary>
        /// https://obsproject.com/docs/reference-sources.html?highlight=obs_source_get_flags#c.obs_source_set_flags
        /// </summary>
        /// <param name="source"></param>
        /// <param name="flags">OBS_SOURCE_FLAG_FORCE_MONO Forces audio to mono</param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_source_set_flags(obs_source_t source, uint flags);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-sources.html#c.obs_source_update</para>
        /// <para>
        /// Updates the settings for a source and calls the obs_source_info.update callback of the source. 
        /// If the source is a video source, the obs_source_info.update will be not be called immediately; 
        /// instead, it will be deferred to the video thread to prevent threading issues.
        /// </para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_source_update(obs_source_t source, obs_data_t settings);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern obs_data_t obs_source_get_settings(obs_source_t source);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_data_t obs_get_source_defaults(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id);
    }
}
