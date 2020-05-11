using System;

namespace PinochleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server.Instance().Start();
        }
    }
}
