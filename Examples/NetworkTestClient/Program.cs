using System;

namespace NetworkTestClient
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int input = 0;

            while (input != 10)
            {
                Console.WriteLine("<1> Async example");
                Console.WriteLine("<2> Lambda example");
                Console.WriteLine("<3> Delegate example");
                Console.WriteLine("<4> Object driven example");
                Console.WriteLine("<5> TcpConnection only example");
                Console.WriteLine("<6> RawData example");
                Console.WriteLine("<7> RSA example");
                Console.WriteLine("<8> Stress-Test");
                Console.WriteLine("<9> IPv6 example");
                Console.Write("> ");

                input = 0;
                while (!int.TryParse(Console.ReadLine(), out input) || input < 1 || input > 10)
                    Console.Write("> ");

                switch (input)
                {
                    case 1:
                        new AsyncExample().Demo();
                        break;

                    case 2:
                        new LambdaExample().Demo();
                        break;

                    case 3:
                        new DelegateExample().Demo();
                        break;
                    case 4:
                        new ObjectExample().Demo();
                        break;

                    case 5:
                        new SingleConnectionExample().Demo();
                        break;

                    case 6:
                        new RawDataExample().Demo();
                        break;

                    case 7:
                        new RSAExample().Demo();
                        break;
                    case 8:
                        new StressTestExample().Demo();
                        break;
                    case 9:
                        new IPv6Example().Demo();
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}