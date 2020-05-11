using System;

namespace PinochleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server running...");
            Server.Instance().Start();
        }
    }
}
