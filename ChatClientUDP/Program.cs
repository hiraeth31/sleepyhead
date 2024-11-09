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
            Console.Write("Введите адрес сервера: ");
            string serverAddress = Console.ReadLine();
            int port = 8888;

            try
            {
                UdpClient client = new UdpClient();
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

                    IPEndPoint remoteEP = null;
                    data = client.Receive(ref remoteEP);
                    message = Encoding.UTF8.GetString(data);

                    Console.WriteLine($"Сервер {remoteEP.Address}:{remoteEP.Port}: {message}");
                }

                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}
