using System;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace SharedProject1
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct Struct1
    {
        public byte b1;
        public byte b2;
        public byte b3;
        public byte b4;
        public uint i1;
        public uint i2;
        public fixed byte fixedBuffer[1500];
        public uint i3;
    }

    static class Class1
    {
        public static void Test()
        {
            byte[] ba = new byte[] { 1, 2, 3, 4, 5, };

            Span<byte> span1 = new Span<byte>(ba, 1, 2);

            FileStream fs = File.Open(@"c:\tmp\test.txt", FileMode.Open);

            fs.Write(span1.ToArray(), 0, span1.Length);

            TcpClient tc = new TcpClient();
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IPv4);
            s.Connect("1.2.3.4", 1234);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            sw.Stop();
            long tick = sw.ElapsedTicks;

            ref Struct1 s1 = ref Unsafe.As<byte, Struct1>(ref ba[0]);
            s1.b1 = 1;

            TestAsync().Wait();
        }

        public static async Task<int> TestAsync()
        {
            int ret = 0;
            for (int i = 0; i < 10; i++)
            {
                ret += 1;
                ret += await TestAsync2(i);
                ret += 2;
                ret += await TestAsync3(i);
                ret += 3;
            }

            return (int)DateTime.Now.Ticks;
        }

        public static async Task<int> TestAsync2(int i)
        {
            await Task.Delay(1000);

            Task t1 = Task.Delay(1000);
            Task t2 = Task.Delay(1000);

            await Task.WhenAll(t1, t2);

            return i;
        }

        public static async ValueTask<int> TestAsync3(int i)
        {
            await Task.Delay(1000);

            Task t1 = Task.Delay(1000);
            Task t2 = Task.Delay(1000);

            await Task.WhenAll(t1, t2);

            return i;
        }
    }
}
