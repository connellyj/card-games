using System;

namespace PinochleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server");
            Server.Instance().Start();
            Console.WriteLine("Server running...");
        }
    }
}
