﻿using System;
using System.Net.Sockets;
using System.Text;

namespace _21._06_TCP_UDP_sockets
{
    public class ExchangeRateClient
    {
        private static void Main()
        {
            while (true)
            {
                try
                {
                    TcpClient client = new TcpClient("localhost", 5000);
                    NetworkStream stream = client.GetStream();

                    while (true)
                    {
                        Console.Write("Enter currency pair (e.g., USD_EURO) or 'exit' to quit: ");
                        string input = Console.ReadLine();

                        if (input.ToLower() == "exit")
                        {
                            break;
                        }

                        byte[] request = Encoding.UTF8.GetBytes(input);
                        stream.Write(request, 0, request.Length);

                        byte[] responseBuffer = new byte[1024];
                        int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                        if (response.Contains("Server is under maximum load") || response.Contains("Request limit reached"))
                        {
                            Console.WriteLine(response);
                            break;
                        }

                        Console.WriteLine($"Exchange Rate: {response}");
                    }

                    stream.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
