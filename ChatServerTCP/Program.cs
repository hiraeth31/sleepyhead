using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static async Task HandleClient(TcpClient client)
        {
            var clientEndPoint = ((IPEndPoint)client.Client.RemoteEndPoint);
            Console.WriteLine($"Клиент {clientEndPoint.Address}:{clientEndPoint.Port} подключился");

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (string.IsNullOrEmpty(message))
                        {
                            break;
                        }

                        Console.WriteLine($"Клиент {clientEndPoint.Address}:{clientEndPoint.Port}: {message}");

                        Console.Write("Сервер: ");
                        string serverMessage = Console.ReadLine();
                        byte[] data = Encoding.UTF8.GetBytes(serverMessage);
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine($"Клиент {clientEndPoint.Address}:{clientEndPoint.Port} отключился");
            }
        }

        static async Task Main(string[] args)
        {
            int port = 8888;
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            listener.Start();
            Console.WriteLine($"Сервер запущен на порту {port}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClient(client); 
            }
        }
    }
}