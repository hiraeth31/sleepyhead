using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 8888;

            try
            {
                UdpClient server = new UdpClient(port);
                Console.WriteLine($"Сервер запущен на порту {port}");

                while (true)
                {
                    IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = server.Receive(ref clientEndPoint);
                    string message = Encoding.UTF8.GetString(data);


                    Console.WriteLine($"Клиент {clientEndPoint.Address}:{clientEndPoint.Port}: {message}");

                    Console.Write("Сервер: ");
                    string serverMessage = Console.ReadLine();
                    if (serverMessage.ToLower() == "exit")
                    {
                        break;
                    }

                    data = Encoding.UTF8.GetBytes(serverMessage);
                    server.Send(data, data.Length, clientEndPoint);
                }

                server.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}
