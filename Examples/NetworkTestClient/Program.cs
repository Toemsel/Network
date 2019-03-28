using System;

namespace NetworkTestClient
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int input = 0;

            while (input != 9)
            {
                Console.WriteLine("<1> Async example");
                Console.WriteLine("<2> Lambda example");
                Console.WriteLine("<3> Delegate example");
#if NET46
                Console.WriteLine("<4> Bluetooth example");
#endif
                Console.WriteLine("<5> Object driven example");
                Console.WriteLine("<6> TcpConnection only example");
                Console.WriteLine("<7> RawData example");
                Console.WriteLine("<8> RSA example");
                Console.WriteLine("<9> Exit");
                Console.Write("> ");

                input = 0;
                while (!int.TryParse(Console.ReadLine(), out input) || input < 1 || input > 8)
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
#if NET46
                    case 4:
                        new BluetoothExample().Demo();
                        break;
#endif
                    case 5:
                        new ObjectExample().Demo();
                        break;

                    case 6:
                        new SingleConnectionExample().Demo();
                        break;

                    case 7:
                        new RawDataExample().Demo();
                        break;

                    case 8:
                        new RSAExample().Demo();
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}