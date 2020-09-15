using DeviceClient.Simulator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DeviceClient
{
    class Program
    {
        private static bool isRunning = false;
        static void Main(string[] args)
        {
            try
            {
                isRunning = true;
                DeviceClient.Simulator.Client cl= new Simulator.Client();
                cl.ConnectToServer();
                Thread mainThread = new Thread(new ThreadStart(MainThread));
                mainThread.Start();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {300} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    // If the time for the next loop is in the past,
                    // aka it's time to execute another tick
                    //GameLogic.Update(); // Execute game logic
                    ThreadManager.UpdateMain();
                    // Calculate at what point in time the next tick should be executed
                    _nextLoop = _nextLoop.AddMilliseconds(300);
                    //Console.WriteLine(DateTime.Now);
                    if (_nextLoop > DateTime.Now)
                    {
                        // If the execution time for the next tick is in the future,
                        // aka the server is NOT running behind
                        // Let the thread sleep until it's needed again.
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }

    }
}
