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
        private static readonly int maxRequests = 5;
        private static readonly TimeSpan cooldownPeriod = TimeSpan.FromMinutes(1);
        private static readonly int maxConnections = 10; 

        private static readonly Dictionary<string, ClientInfo> clients = new Dictionary<string, ClientInfo>();
        private static int currentConnections = 0;

        private static readonly Dictionary<string, string> userCredentials = new Dictionary<string, string>
        {
            { "user1", "password1" },
            { "user2", "password2" }
        };

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

            lock (clients)
            {
                if (currentConnections >= maxConnections)
                {
                    NotifyClientMaxCapacity(client);
                    client.Close();
                    return;
                }
            }

            NetworkStream stream = client.GetStream();

            if (!AuthenticateClient(stream))
            {
                log.Add($"Authentication failed for {clientInfo} at {DateTime.Now}");
                client.Close();
                return;
            }

            lock (clients)
            {
                currentConnections++;
            }

            log.Add($"Connected: {clientInfo} at {DateTime.Now}");

            if (!clients.ContainsKey(clientInfo))
            {
                clients[clientInfo] = new ClientInfo { LastRequestTime = DateTime.MinValue, RequestCount = 0 };
            }

            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    ClientInfo clientData = clients[clientInfo];

                    if (clientData.RequestCount >= maxRequests && DateTime.Now < clientData.LastRequestTime + cooldownPeriod)
                    {
                        string cooldownMessage = "Request limit reached. Please try again in a minute.";
                        byte[] responseData = Encoding.UTF8.GetBytes(cooldownMessage);
                        stream.Write(responseData, 0, responseData.Length);
                        log.Add($"Request limit reached for {clientInfo} at {DateTime.Now}");
                        break;
                    }

                    if (DateTime.Now >= clientData.LastRequestTime + cooldownPeriod)
                    {
                        clientData.RequestCount = 0; 
                    }

                    clientData.RequestCount++;
                    clientData.LastRequestTime = DateTime.Now;

                    string response = GetExchangeRateResponse(request);
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);

                    log.Add($"Exchange rate requested: {request} at {DateTime.Now} - Rate: {response}");
                }
            }
            finally
            {
                lock (clients)
                {
                    currentConnections--;
                }
                log.Add($"Disconnected: {clientInfo} at {DateTime.Now}");
                client.Close();
            }
        }

        private static bool AuthenticateClient(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            bytesRead = stream.Read(buffer, 0, buffer.Length);
            string username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            bytesRead = stream.Read(buffer, 0, buffer.Length);
            string password = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (userCredentials.TryGetValue(username, out string storedPassword) && storedPassword == password)
            {
                byte[] response = Encoding.UTF8.GetBytes("Authentication successful");
                stream.Write(response, 0, response.Length);
                return true;
            }
            else
            {
                byte[] response = Encoding.UTF8.GetBytes("Authentication failed");
                stream.Write(response, 0, response.Length);
                return false;
            }
        }

        private static void NotifyClientMaxCapacity(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            string message = "Server is under maximum load. Please try again later.";
            byte[] responseData = Encoding.UTF8.GetBytes(message);
            stream.Write(responseData, 0, responseData.Length);
        }

        private static string GetExchangeRateResponse(string request)
        {
            if (exchangeRates.TryGetValue(request, out double rate))
            {
                return rate.ToString();
            }
            else
            {
                return "Invalid currency pair";
            }
        }

        private class ClientInfo
        {
            public DateTime LastRequestTime { get; set; }
            public int RequestCount { get; set; }
        }
    }
}
