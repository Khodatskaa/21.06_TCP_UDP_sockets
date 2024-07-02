using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace _21._06_TCP_UDP_sockets
{
    public class ExchangeRateServer
    {
        private static readonly Dictionary<string, double> exchangeRates = new Dictionary<string, double>
        {
            { "USD_EURO", 0.85 },
            { "EURO_USD", 1.18 },
        };

        private static readonly List<string> log = new List<string>();

        public static void Main()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private static void HandleClient(object clientObject)
        {
            TcpClient client = (TcpClient)clientObject;
            IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            string clientInfo = $"{clientEndPoint.Address}:{clientEndPoint.Port}";

            log.Add($"Connected: {clientInfo} at {DateTime.Now}");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                string response = GetExchangeRateResponse(request);
                byte[] responseData = Encoding.UTF8.GetBytes(response);
                stream.Write(responseData, 0, responseData.Length);
            }

            log.Add($"Disconnected: {clientInfo} at {DateTime.Now}");
            client.Close();
        }

        private static string GetExchangeRateResponse(string request)
        {
            if (exchangeRates.TryGetValue(request, out double rate))
            {
                log.Add($"Exchange rate requested: {request} at {DateTime.Now} - Rate: {rate}");
                return rate.ToString();
            }
            else
            {
                return "Invalid currency pair";
            }
        }
    }
}
