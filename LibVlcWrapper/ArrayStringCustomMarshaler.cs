using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Globalization;

namespace LibVlcWrapper
{
    class ArrayStringCustomMarshaler : ICustomMarshaler
    {
        private readonly Native m_native = new Native();
        private readonly Managed m_managed = new Managed();

        public ArrayStringCustomMarshaler(String pstrCookie)
        {

        }

        #region ICustomMarshaler Members

        public void CleanUpManagedData(object ManagedObj)
        {
            this.m_managed.CleanUpManagedData(ManagedObj);
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            this.m_native.CleanUpNativeData(pNativeData);
        }

        public int GetNativeDataSize()
        {
            return this.m_native.GetNativeDataSize();
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            return this.m_native.MarshalManagedToNative(ManagedObj);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return this.m_managed.MarshalNativeToManaged(pNativeData);
        }

        public static ICustomMarshaler GetInstance(String pstrCookie)
        {
            return new ArrayStringCustomMarshaler(pstrCookie);
        }

        #endregion

        private class Native
        {
            private readonly object m_lock_native;
            private readonly IDictionary<IntPtr, StringArraySizePair> m_native_data = new Dictionary<IntPtr, StringArraySizePair>();
            private volatile int m_native_data_size = 0;

            public Native()
            {
                this.m_lock_native = ((ICollection)m_native_data).SyncRoot ?? new object();
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                if (pNativeData == IntPtr.Zero)
                {
                }
                else
                {
                    lock (this.m_lock_native)
                    {
                        var size = this.m_native_data[pNativeData].Size;
                        this.m_native_data.Remove(pNativeData);
                        Marshal.FreeHGlobal(pNativeData);
                        this.m_native_data_size -= size;
                    }
                }
            }

            public int GetNativeDataSize()
            {
                lock (this.m_lock_native)
                {
                    return this.m_native_data_size;
                }
            }

            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                string[] strs = ManagedObj as string[];
                if (ManagedObj != null && strs == null)
                {
                    throw new InvalidCastException("ManagedObj to string[]");
                }

                if (ManagedObj == null)
                {
                    return IntPtr.Zero;
                }
                else
                {
                    byte[][] bytess = new byte[strs.Length][];

                    int native_data_size;
                    {
                        int strs_native_data_size = 0;

                        for (int i = 0; i < strs.Length; ++i)
                        {
                            var str = strs[i];
                            byte[] bytes;
                            if (str == null)
                            {
                                bytes = null;
                            }
                            else
                            {
                                bytes = Encoding.UTF8.GetBytes(str);
                                if (bytes == null)
                                    throw new ApplicationException("Encoding.GetBytes(String) returns null");

                                strs_native_data_size += bytes.Length + 1;
                            }
                            bytess[i] = bytes;
                        }

                        native_data_size = (bytess.Length + 1) * IntPtr.Size + strs_native_data_size;
                    }
                    var native_data = Marshal.AllocHGlobal(native_data_size);
                    try
                    {
                        {
                            IntPtr str_native_data = native_data + (bytess.Length + 1) * IntPtr.Size;
                            IntPtr str_ptr_native_data = native_data;
                            for (int i = 0;
                                 i < bytess.Length;
                                             str_native_data += bytess[i] == null ? 0 : bytess[i].Length + 1, str_ptr_native_data += IntPtr.Size, ++i)
                            {
                                if (bytess[i] == null)
                                {
                                    Marshal.WriteIntPtr(str_ptr_native_data, IntPtr.Zero);
                                }
                                else
                                {
                                    Marshal.Copy(bytess[i], 0, str_native_data, bytess[i].Length);
                                    Marshal.WriteByte(str_native_data, bytess[i].Length, 0);
                                    Marshal.WriteIntPtr(str_ptr_native_data, str_native_data);
                                }
                            }
                            Marshal.WriteIntPtr(str_ptr_native_data, IntPtr.Zero);
                        }

                        lock (this.m_lock_native)
                        {
                            this.m_native_data_size += native_data_size;
                            try
                            {
                                this.m_native_data.Add(native_data, new StringArraySizePair(strs, native_data_size));
                            }
                            catch
                            {
                                this.m_native_data_size -= native_data_size;
                                throw;
                            }
                        }
                        return native_data;
                    }
                    catch
                    {
                        Marshal.FreeHGlobal(native_data);
                        throw;
                    }
                }
            }

            private class StringArraySizePair : Tuple<string[], int>
            {
                public StringArraySizePair(string[] strs, int size)
                    : base(strs, size)
                {
                    if (strs == null) throw new ArgumentNullException("strs");
                }

                public string[] Strs
                {
                    get { return this.Item1; }
                }

                public int Size
                {
                    get { return this.Item2; }
                }
            }
        }

        private class Managed
        {
            private readonly object m_lock_managed;
            private readonly IDictionary<string[], IntPtr> m_managed_data = new Dictionary<string[], IntPtr>(new StringArrayComparer());

            public Managed()
            {
                this.m_lock_managed = ((ICollection)m_managed_data).SyncRoot ?? new object();
            }

            public void CleanUpManagedData(object ManagedObj)
            {
                string[] strs = ManagedObj as string[];
                if (ManagedObj != null && strs == null)
                {
                    throw new InvalidCastException("ManagedObj to string[]");
                }

                if (strs == null)
                {
                }
                else
                {
                    lock (this.m_lock_managed)
                    {
                        this.m_managed_data.Remove(strs);
                    }
                }
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                if (pNativeData != IntPtr.Zero)
                {
                    IntPtr[] ptrs = null;
                    {
                        int size = 0;
                        int offset = 0;
                        for (; /*maxSize < 0 || size < maxSize*/; ++size, offset += IntPtr.Size)
                        {
                            IntPtr ptr = Marshal.ReadIntPtr(pNativeData, offset);
                            if (ptr == IntPtr.Zero)
                            {
                                ptrs = new IntPtr[size];
                                break;
                            }
                        }
                        if (ptrs == null)
                        {
                            throw new ArgumentException("String array exceeds maximum limit, probably function returned bad pointer.", "pNativeData");
                        }
                        else
                        {
                            Marshal.Copy(pNativeData, ptrs, 0, size);
                        }
                    }

                    var strs = new string[ptrs.Length];
                    for (int i = 0; i < ptrs.Length; ++i)
                    {
                        IntPtr ptr = ptrs[i];
                        string str;
                        if (ptr == IntPtr.Zero)
                        {
                            str = null;
                        }
                        else
                        {
                            int size = 0;
                            byte[] message = null;
                            for (; /*maxSize < 0 || size < maxSize*/; ++size)
                            {
                                byte b = Marshal.ReadByte(ptr, size);
                                if (b == 0x0)
                                {
                                    message = new byte[size];
                                    break;
                                }
                            }
                            if (message == null)
                            {
                                throw new ArgumentException("Message exceeds maximum limit, probably function returned bad pointer.", "pNativeData");
                            }
                            else
                            {
                                Marshal.Copy(ptr, message, 0, size);
                                str = Encoding.UTF8.GetString(message);
                            }
                        }
                        strs[i] = str;
                    }

                    lock (this.m_lock_managed)
                    {
                        this.m_managed_data.Add(strs, pNativeData);
                    }

                    return strs;
                }
                else
                {
                    return null;
                }
            }

            private class StringArrayComparer : IEqualityComparer<string[]>
            {
                public bool Equals(string[] x, string[] y)
                {
                    return object.ReferenceEquals(x, y);
                }

                public int GetHashCode(string[] obj)
                {
                    if (obj == null)
                        return 0;
                    else
                        return obj.GetHashCode();
                }
            }
        }
    }
}