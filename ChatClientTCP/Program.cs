using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введите адрес сервера: ");
            string serverAddress = Console.ReadLine();

            int port = 8888;

            TcpClient client = new TcpClient();

            try
            {
                client.Connect(serverAddress, port);
                Console.WriteLine($"Подключено к серверу {serverAddress}:{port}");

                NetworkStream stream = client.GetStream();

                while (true)
                {
                    Console.Write("Клиент: ");
                    string message = Console.ReadLine();

                    if (message == "exit")
                    {
                        break;
                    }

                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);

                    data = new byte[1024];
                    int bytesRead = stream.Read(data, 0, data.Length);
                    message = Encoding.UTF8.GetString(data, 0, bytesRead);

                    Console.WriteLine($"Сервер: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}