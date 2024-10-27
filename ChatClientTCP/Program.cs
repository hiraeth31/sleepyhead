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
            // Запрашиваем адрес сервера у пользователя
            Console.Write("Введите адрес сервера: ");
            string serverAddress = Console.ReadLine();

            // Устанавливаем порт сервера
            int port = 8888;

            // Создаем TCP-сокет
            TcpClient client = new TcpClient();

            try
            {
                // Подключаемся к серверу
                client.Connect(serverAddress, port);
                Console.WriteLine($"Подключено к серверу {serverAddress}:{port}");

                // Создаем поток для обмена сообщениями с сервером
                NetworkStream stream = client.GetStream();

                // Цикл отправки и получения сообщений
                while (true)
                {
                    // Читаем сообщение от пользователя
                    Console.Write("Клиент: ");
                    string message = Console.ReadLine();

                    if (message == "exit")
                    {
                        break;
                    }

                    // Отправляем сообщение на сервер
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);

                    // Получаем ответ от сервера
                    data = new byte[1024];
                    int bytesRead = stream.Read(data, 0, data.Length);
                    message = Encoding.UTF8.GetString(data, 0, bytesRead);

                    // Выводим ответ сервера на консоль
                    Console.WriteLine($"Сервер: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                // Закрываем соединение
                client.Close();
            }
        }
    }
}