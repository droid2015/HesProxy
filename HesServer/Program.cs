using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HesServer.Simulator;

namespace HesServer
{
    class Program
    {
        private static bool isRunning = false;
        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(50, 1331);
           
        }
        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    // If the time for the next loop is in the past,
                    // aka it's time to execute another tick
                    GameLogic.Update(); // Execute game logic
                    // Calculate at what point in time the next tick should be executed
                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK); 
                  
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
