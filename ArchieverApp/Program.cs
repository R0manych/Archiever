using ArchieverApp.GZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ArchieverApp
{
    class Program
    {
        private static BaseGzip zipper; 

        static int Main(string[] args)
        {
            try
            {
                //args = new string[] {"compress", "D:\\large32", "D:\\large32test10" };
                Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);
                switch (args[0].ToLower())
                {
                    case "compress":
                        zipper = new Compressor(args[1], args[2]);
                        break;
                    case "decompress":
                        zipper = new Decompressor(args[1], args[2]);
                        break;
                }
                Console.WriteLine("Started. Please wait for a while");
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                zipper.Launch();
                var result = zipper.CallBackResult();
                stopWatch.Stop();
                if (result == 0)
                    Console.WriteLine($"Success. Elapsed time = {stopWatch.Elapsed.TotalMinutes}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                    Console.WriteLine(ex.InnerException.StackTrace);
                }
                return 1;
            }
        }

        static void CancelKeyPress(object sender, ConsoleCancelEventArgs _args)
        {
            if (_args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("\nCancelling...");
                _args.Cancel = true;
                zipper.Cancel();
            }
        }
    }
}
