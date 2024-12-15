using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

public class Traceroute
{
    private const int MAX_PACKET = 1024;
    private const int DEF_PACKET_SIZE = 32; // Размер данных в пакете
    private const int MAX_HOPS = 30; // Максимальное количество хопов

    // Структура IP-заголовка (только необходимые поля)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IpHeader
    {
        public byte ip_verlen; // 4-bit version and 4-bit header length
        public byte ip_tos; // Type of service
        public ushort ip_len; // Total length
        public ushort ip_id; // Identification
        public ushort ip_off; // Flags and fragment offset
        public byte ip_ttl; // Time to live
        public byte ip_p; // Protocol
        public ushort ip_sum; // Checksum
        public uint ip_src; // Source address
        public uint ip_dst; // Destination address
    }


    // Структура ICMP заголовка
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IcmpHeader
    {
        public byte i_type;
        public byte i_code;
        public ushort i_cksum;
        public ushort i_id;
        public ushort i_seq;
        public uint timestamp;
    }

    // Метод для расчета контрольной суммы
    private static ushort checksum(byte[] buffer, int size)
    {
        long chcksm = 0;
        int count = size / 2;
        int i = 0;

        for (; i < count * 2; i += 2)
        {
            chcksm += (buffer[i]) | (((long)buffer[i + 1]) << 8);
        }

        if (i < size)
        {
            chcksm += buffer[i];
        }

        chcksm = (chcksm >> 16) + (chcksm & 0xffff);
        chcksm += (chcksm >> 16);
        return (ushort)(~chcksm);
    }

    // Обработка ответа
    private static void decode_resp(byte[] buf, int bytes, EndPoint remote_ep, uint timestamp, int ttl)
    {
        var from_sin = (IPEndPoint)remote_ep;
        IPAddress ip = from_sin.Address;

        if (bytes < 20)
        {
            Console.WriteLine($"{ttl}: Too short IP message");
            return;
        }

        IpHeader ip_hdr = (IpHeader)ByteArrayToStructure(buf, typeof(IpHeader));

        int ip_header_len = (ip_hdr.ip_verlen & 0x0F) * 4;
        if (bytes < ip_header_len + 8)
        {
            Console.WriteLine($"{ttl}: Too short ICMP message");
            return;
        }


        if (ip_hdr.ip_p == 1) // Protocol ICMP = 1
        {
            byte[] icmp_data = new byte[bytes - ip_header_len];
            Array.Copy(buf, ip_header_len, icmp_data, 0, bytes - ip_header_len);

            var reply_type = (int)icmp_data[0];
            var reply_code = (int)icmp_data[1];
            var icmp_hdr = (IcmpHeader)ByteArrayToStructure(icmp_data, typeof(IcmpHeader));


            if (reply_type == 0)
            {
                // Это ответ на наш запрос
                Console.WriteLine($"{ttl}: Reply from: {ip}  bytes:{bytes} time:{GetTimestampDiff(timestamp, GetTickCount())}ms");
            }
            else if (reply_type == 11)
            {
                // Время истекло
                var ip_addr_from_reply = ((IPEndPoint)remote_ep).Address;
                Console.WriteLine($"{ttl}: Hop: {ip_addr_from_reply}  Time exceeded");

                return;
            }

            else
            {
                Console.WriteLine($"{ttl}: Unknown packet type:{reply_type}  code: {reply_code}");
            }
        }
        else
        {
            Console.WriteLine($"{ttl}: Unknown packet type:{ip_hdr.ip_p}");
        }

    }
    static uint GetTickCount()
    {
        return (uint)Environment.TickCount;
    }
    static long GetTimestampDiff(uint first, uint second)
    {
        return second - first;
    }

    static object ByteArrayToStructure(byte[] bytes, Type type)
    {
        int size = Marshal.SizeOf(type);
        if (size > bytes.Length)
        {
            return null;
        }
        IntPtr buffer = Marshal.AllocHGlobal(size);
        Marshal.Copy(bytes, 0, buffer, size);
        object structure = Marshal.PtrToStructure(buffer, type);
        Marshal.FreeHGlobal(buffer);
        return structure;
    }

    public static void Main(string[] args)
    {

        Console.Write("Enter host name or IP address: ");
        string hostName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(hostName))
        {
            Console.WriteLine("Host name cannot be empty.");
            return;
        }


        IPAddress targetIP;

        try
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            targetIP = hostEntry.AddressList[0];
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error resolving host name: {e.Message}");
            return;
        }

        Console.WriteLine($"Tracing route to {hostName} [{targetIP}]\n");

        try
        {
            using (Socket sockRaw = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
            {
                sockRaw.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

                byte[] icmp_data = new byte[MAX_PACKET];
                IcmpHeader icmp_hdr;

                byte[] datapart;

                int datasize = DEF_PACKET_SIZE;


                datapart = new byte[datasize];


                for (int ttl = 1; ttl <= MAX_HOPS; ttl++)
                {

                    Array.Clear(icmp_data, 0, MAX_PACKET);
                    icmp_hdr = new IcmpHeader();
                    icmp_hdr.i_type = 8; // ICMP_ECHO
                    icmp_hdr.i_code = 0;
                    icmp_hdr.i_id = (ushort)System.Diagnostics.Process.GetCurrentProcess().Id;
                    icmp_hdr.i_cksum = 0;
                    icmp_hdr.i_seq = (ushort)ttl;

                    var icmp_hdr_bytes = StructureToByteArray(icmp_hdr);


                    Array.Copy(icmp_hdr_bytes, 0, icmp_data, 0, icmp_hdr_bytes.Length);

                    // Заполняем что-нибудь в буфер для отправки.

                    Array.Fill(datapart, (byte)'m', 0, datasize - icmp_hdr_bytes.Length);
                    Array.Copy(datapart, 0, icmp_data, icmp_hdr_bytes.Length, datasize - icmp_hdr_bytes.Length);

                    // Вычисление контрольной суммы
                    icmp_hdr.timestamp = GetTickCount();
                    Array.Copy(BitConverter.GetBytes(icmp_hdr.timestamp), 0, icmp_data, 10, 4);
                    icmp_hdr.i_cksum = checksum(icmp_data, datasize);
                    Array.Copy(BitConverter.GetBytes(icmp_hdr.i_cksum), 0, icmp_data, 2, 2);

                    // Устанавливаем TTL
                    sockRaw.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);

                    // Отправляем ICMP эхо запрос
                    IPEndPoint dest_sin = new IPEndPoint(targetIP, 0);
                    uint start_time = GetTickCount();

                    int bytesSent = sockRaw.SendTo(icmp_data, datasize, SocketFlags.None, dest_sin);
                    byte[] recvbuf = new byte[MAX_PACKET];
                    EndPoint remote_ep = new IPEndPoint(IPAddress.Any, 0); // Any port because raw sockets don't have real ports

                    try
                    {
                        // принимаем пакет
                        int bytesReceived = sockRaw.ReceiveFrom(recvbuf, ref remote_ep);
                        decode_resp(recvbuf, bytesReceived, remote_ep, start_time, ttl);
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.TimedOut)
                        {
                            Console.WriteLine($"{ttl}: * Request timed out.");
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"Error receiving: {e.Message}");
                            return;
                        }

                    }

                }
            }
            Console.WriteLine("Trace complete");
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Socket error: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }
    static byte[] StructureToByteArray(object obj)
    {
        int size = Marshal.SizeOf(obj);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
}