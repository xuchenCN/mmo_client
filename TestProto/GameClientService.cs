using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestProto
{
   class GameClientService
   {
      private BlockingQueue<ChannelMessage> blockingQueue;
      private GameClient client;
      private volatile bool shouldRun;
      private Thread[] threads;
      private TerrainCallback callback;

      public GameClientService(GameClient client)
      {
         this.client = client;
         //this.callback = callback;
         this.blockingQueue = new BlockingQueue<ChannelMessage>(1024);
         int nThread = Math.Max(2, Environment.ProcessorCount / 2);
         threads = new Thread[nThread];
         for (int i = 0; i < nThread; i++)
         {
            Thread thread = new Thread(new ThreadStart(RecvProcessor));
            thread.Name = "Recv Thread-" + i;
            thread.IsBackground = true;
            threads[i] = thread;
         }
      }

      public void addTerrainListener(TerrainCallback callback) 
      {
         this.callback = callback;
      }

      public void Start()
      {
         shouldRun = true;
         foreach (Thread t in threads)
         {
            t.Start();
         }

      }

      public void Stop()
      {
         shouldRun = false;
      }

      public void EnqueueMessage(ChannelMessage message)
      {
         blockingQueue.Enqueue(message);
      }


      protected void RecvProcessor()
      {
         Console.WriteLine("RecvProcessor start");
         while (shouldRun)
         {
            try
            {
               ChannelMessage message = blockingQueue.Dequeue();
               switch (message.GetMessageId())
               {
                  case MessageRegistry.ITEMCREATEEVENT:
                     this.callback.OnItemCreate(ClientItemCreateEvent.ParseFrom(message.GetBody()));
                     break;
                  case MessageRegistry.ITEMDESTROYEVENT:
                     this.callback.OnItemDestroy(ClientItemDestroyEvent.ParseFrom(message.GetBody()));
                     break;
                  case MessageRegistry.ITEMMOVEEVENT:
                     this.callback.OnItemMove(ClientItemMoveEvent.ParseFrom(message.GetBody()));
                     break;
                  case MessageRegistry.CHARACTERCREATEEVENT:
                     this.callback.OnCharacterCreate(ClientCharacterCreateEvent.ParseFrom(message.GetBody()));
                     break;
                  default:
                     Console.WriteLine("Unknow message Id" + message.GetMessageId());
                     break;
               }

               Thread.Sleep(100);
            }
            catch (Exception e)
            {
               Console.WriteLine(e);
            }

         }
         Console.WriteLine("RecvProcessor stop");
      }
   }



}
