using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace obs_net {
    class Helpers {


    }

    /*
 * P/Invoke helpers
 */

    /// <summary>
    /// Marshals strings between unmanaged and managed heaps, and
    /// converts them from UTF-8 strings to UTF-16 and vice versa.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public class UTF8StringMarshaler : ICustomMarshaler {
        IntPtr allocatedPtr;

        public static ICustomMarshaler GetInstance(string cookie) {
            //return instance;
            return new UTF8StringMarshaler();
        }

        public object MarshalNativeToManaged(IntPtr ptr) {
            if (ptr == IntPtr.Zero)
                return null;

            var bytes = new List<byte>();
            int offset = 0;
            byte chr = 0;

            do {
                if ((chr = Marshal.ReadByte(ptr, offset++)) != 0)
                    bytes.Add(chr);
            } while (chr != 0);

            return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
        }

        public IntPtr MarshalManagedToNative(object obj) {
            string str = obj as string;
            if (str == null)
                return IntPtr.Zero;

            byte[] bytes = new byte[System.Text.Encoding.UTF8.GetByteCount(str) + 1];
            System.Text.Encoding.UTF8.GetBytes(str, 0, str.Length, bytes, 0);

            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            allocatedPtr = ptr;
            return ptr;
        }

        public int GetNativeDataSize() {
            return -1;
        }

        public void CleanUpNativeData(IntPtr ptr) {
            // Clean up is called even though no native data were allocated
            // by us. Since we always assume the caller itself allocated
            // the memory, we don't need to release it.

            if (ptr != IntPtr.Zero && allocatedPtr == ptr) {
                Marshal.FreeHGlobal(ptr);
                allocatedPtr = IntPtr.Zero;
            }
        }

        public void CleanUpManagedData(object obj) {
        }
    }
}
