using System;

namespace NetworkTestServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int input = 0;

            Console.WriteLine("<1> Secure-Server example. (Requires selection <8> in the client)");
            Console.WriteLine("<2> UnSecure-Server example. (Working with every other example)");
            Console.Write("> ");

            input = 0;
            while (!int.TryParse(Console.ReadLine(), out input) || input < 1 || input > 2)
                Console.Write("> ");

            switch (input)
            {
                case 1:
                    new SecureServerExample().Demo();
                    break;

                case 2:
                    new UnSecureServerExample().Demo();
                    break;

                default:
                    throw new ArgumentException();
            }
        }
    }
}