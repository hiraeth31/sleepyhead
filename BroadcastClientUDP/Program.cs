using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введите порт сервера: ");
            if (!int.TryParse(Console.ReadLine(), out int port))
            {
                Console.WriteLine("Неверный формат порта.");
                return;
            }

            string serverAddress = "255.255.255.255";

            try
            {
                using (UdpClient client = new UdpClient())
                {
                    client.EnableBroadcast = true; 
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);

                    while (true)
                    {
                        Console.Write("Клиент: ");
                        string message = Console.ReadLine();

                        if (message.ToLower() == "exit")
                        {
                            break;
                        }

                        byte[] data = Encoding.UTF8.GetBytes(message);
                        client.Send(data, data.Length, serverEndPoint);

                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0); 
                        data = client.Receive(ref remoteEP);
                        message = Encoding.UTF8.GetString(data);

                        Console.WriteLine($"Сервер {remoteEP.Address}:{remoteEP.Port}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}
