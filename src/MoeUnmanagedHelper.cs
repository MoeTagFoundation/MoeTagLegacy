using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag
{
    internal class MoeUnmanagedHelper
    {
        private const int MAX_BUFFER_SIZE = 128;

        static Dictionary<string, byte[]> _buffers = new Dictionary<string, byte[]>();

        public static void AddUnmanagedString(string name)
        {
            _buffers.Add(name, new byte[MAX_BUFFER_SIZE]);
        }

        public static byte[] GetRawBuffer(string name)
        {
            return _buffers[name];
        }

        public static string GetUnmanagedString(string name)
        {
            byte[] buffer = GetRawBuffer(name);
            int length = 0;
            foreach (byte by in buffer)
            {
                if (by == 0) { break; }
                length++;
            }
            string? tmp = Encoding.UTF8.GetString(buffer);
            if (tmp == null || String.IsNullOrWhiteSpace(tmp)) { return ""; }
            return tmp![..length];
        }
        
        public static void SetUnmanagedString(string name, string value)
        {
            if (value.Length > MAX_BUFFER_SIZE - 1) { return; }
            byte[] data = Encoding.UTF8.GetBytes(value + "\0");
            SetUnmanagedBuffer(name, data);
        }

        public static void SetUnmanagedBuffer(string name, byte[] bytes)
        {
            byte[] buffer = GetRawBuffer(name);
            Array.ConstrainedCopy(bytes, 0, buffer, 0, bytes.Length);
        }
    }
}
