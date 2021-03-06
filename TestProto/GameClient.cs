﻿using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestProto
{
   class GameClient
   {
      public readonly BlockingQueue<ChannelMessage> accountQueue = new BlockingQueue<ChannelMessage>(8);
      public readonly BlockingQueue<ChannelMessage> terrainQueue = new BlockingQueue<ChannelMessage>(1024);
      public string host;
      public int port;
      public string ticket;
      private TcpClient channel;
     // private AccountCallback accountCallback;
      private IPEndPoint ip;
      private GameClientService service;
      private bool shouldRun;
      private Thread coreThread;
      private static readonly System.Object _lock = new System.Object();
      private static GameClient instance;

      public static GameClient getInstance()
      {
         if (instance == null)
         {
            lock(_lock) 
            {
               if (instance == null)
               {
                  instance = new GameClient();
               }
            }
         }
         return instance;
      }


      private GameClient()
      {
         
      }


      public void Start()
      {
         //this.messageCallback = messageCallback;
         channel = new TcpClient();
         IPAddress ipAddress;
         if (!IPAddress.TryParse(host, out ipAddress))
         {
            ipAddress = Dns.GetHostEntry(host).AddressList[0];

         }
         else {

            ip = new IPEndPoint(ipAddress, port);
         }
        // this.service = new GameClientService(this);
         coreThread = new Thread(new ThreadStart(ChannelCoreHandle));
         coreThread.Name = "Client-Network-Core-Thread";
         coreThread.IsBackground = true;

         this.channel.Connect(ip);
        // this.service.Start();
         shouldRun = true;
         coreThread.Start();
      }

      public void Stop()
      {
        Console.WriteLine("Client Stop");
        shouldRun = false;
         coreThread.Interrupt();
         if (channel != null)
         {
            channel.Close();
         }
      }



      public void Finalize()
      {
         this.Stop();
      }

      public bool isRunning() {
         return this.shouldRun;
      }

      public TcpClient GetChannel()
      {
         return this.channel;
      }


      public void UserLogin(UserLoginRequest request)
      {
         SendMessage((uint)MessageRegistry.USERLOGINREQUEST, request);
      }

      public void CharacterEnter(ClientCharacterEnterRequest request)
      {
         SendMessage((uint)MessageRegistry.CHARACTERENTERREQUEST, request);
      }

      public void CharacterMove(ClientCharacterMove request)
      {
         SendMessage((uint)MessageRegistry.CHARACTERMOVE, request);
      }

      private void SendMessage(uint messageId, IMessageLite message)
      {

         if (!this.isRunning())
         {
            throw new Exception("Client not start !");
         }

         using (MemoryStream stream = new MemoryStream())
         {
            CodedOutputStream os = CodedOutputStream.CreateInstance(stream);
            //Body Length
            os.WriteRawVarint32((uint)message.SerializedSize);

            //MessageId
            os.WriteRawVarint32(messageId);

            //Body
            message.WriteTo(os);

            os.Flush();

            byte[] data = stream.ToArray();
            //client.Send(new ArraySegment<byte>(data));
            channel.GetStream().Write(data, 0, data.Length);
         }
      }


      public void ChannelCoreHandle()
      {
         {

            byte[] buffer = new byte[1024];

            //client.Client.ReceiveTimeout = 20;

            while (shouldRun && Thread.CurrentThread.IsAlive)
            {
               //Console.WriteLine("IsAlive " + Thread.CurrentThread.IsAlive);
               //UnityEngine.Debug.Log("IsAlive " + Thread.CurrentThread.IsAlive);
               try
               {
                  NetworkStream netStream = channel.GetStream();
                  //netStream.ReadTimeout = 200;
                  byte[] fullData = null;
                  using (MemoryStream ms = new MemoryStream())
                  {
                     //Have rest data
                     if (fullData != null)
                     {
                        //Console.WriteLine("rest data " + fullData.Length);
                        ms.Write(fullData, 0, fullData.Length);
                     }

                     if (netStream.CanRead)
                     {
                        int read = 0;
                        do
                        {
                           try
                           {
                              //Blocking read
                              read = channel.GetStream().Read(buffer, 0, buffer.Length);
                              Console.WriteLine(read);

                           }
                           catch (IOException ex)
                           {
                              // if the ReceiveTimeout is reached an IOException will be raised...
                              // with an InnerException of type SocketException and ErrorCode 10060
                              var socketExept = ex.InnerException as SocketException;
                              if (socketExept == null || socketExept.ErrorCode != 10060)
                                 // if it's not the "expected" exception, let's not hide the error
                                 throw ex;
                              // if it is the receive timeout, then reading ended
                              read = 0;
                           }

                           if (read > 0)
                           {
                              ms.Write(buffer, 0, read);
                           }
                           else
                           {
                              // Console.WriteLine("read completed");
                           }

                           // ms.Position = 0;
                           //Console.WriteLine(ms.Length);

                        } while (netStream.DataAvailable && read > 0);

                        fullData = ms.ToArray();
                        int totalMessageSize = fullData.Length;
                        CodedInputStream stream = CodedInputStream.CreateInstance(fullData);

                        while (!stream.IsAtEnd)
                        {
                           long markPos = stream.Position;

                           int bodySize = (int)stream.ReadRawVarint32();
                          // Console.WriteLine("body size " + bodySize);
                           //Validate message correct
                           if (bodySize <= 0 || bodySize > fullData.Length)
                           {
                              //Console.WriteLine("Wrong data !");
                              continue;
                           }

                           int messageId = (int)stream.ReadRawVarint32();

                           if (bodySize <= (totalMessageSize - stream.Position))
                           {
                              byte[] body = stream.ReadRawBytes(bodySize);

                              //IMessageLite protoMes = null;
                              Console.WriteLine((MessageRegistry)messageId);
                              switch (messageId)
                              {
                                 
                                 case (int)MessageRegistry.USERLOGINRESPONSE:
                                    if (this.accountQueue != null)
                                    {

                                       ChannelMessage _channelMessage = new ChannelMessage((MessageRegistry)messageId, UserLoginResponse.ParseFrom(body));
                                       this.accountQueue.Enqueue(_channelMessage);
                                    } 
                                    break;
                                 case (int)MessageRegistry.CHARACTERENTERRESPONSE:
                                    if (this.accountQueue != null)
                                    {
                                       ChannelMessage _channelMessage = new ChannelMessage((MessageRegistry)messageId, UserLoginResponse.ParseFrom(body));
                                       this.accountQueue.Enqueue(_channelMessage);
                                    }
                                    break;
                                 case (int)MessageRegistry.ITEMCREATEEVENT:
                                    if (this.terrainQueue != null)
                                    {
                                       ChannelMessage _channelMessage = new ChannelMessage((MessageRegistry)messageId, ClientItemCreateEvent.ParseFrom(body));
                                       this.terrainQueue.Enqueue(_channelMessage);
                                    }
                                    break;
                                 case (int)MessageRegistry.ITEMDESTROYEVENT:
                                    if (this.terrainQueue != null)
                                    {
                                       ChannelMessage _channelMessage = new ChannelMessage((MessageRegistry)messageId, ClientItemDestroyEvent.ParseFrom(body));
                                       this.terrainQueue.Enqueue(_channelMessage);
                                    }
                                    break;
                                 case (int)MessageRegistry.ITEMMOVEEVENT:
                                    if (this.terrainQueue != null)
                                    {
                                       ChannelMessage _channelMessage = new ChannelMessage((MessageRegistry)messageId, ClientItemMoveEvent.ParseFrom(body));
                                       this.terrainQueue.Enqueue(_channelMessage);
                                    }
                                    break;
                                 case (int)MessageRegistry.CHARACTERCREATEEVENT:
                                    if (this.terrainQueue != null)
                                    {
                                       ChannelMessage _channelMessage = new ChannelMessage((MessageRegistry)messageId, ClientCharacterCreateEvent.ParseFrom(body));
                                       this.terrainQueue.Enqueue(_channelMessage);
                                    }
                                    break;
                                 default:
                                    Console.WriteLine("Unknow message Id" + messageId);
                                    break;
                              }

                              //Console.WriteLine("response " + protoMes);

                           }
                           else
                           {
                              //Rest data not enough
                              long remainSize = totalMessageSize - markPos;
                              byte[] restData = stream.ReadRawBytes((int)remainSize);
                              fullData = restData;
                              break;
                           }


                        }


                     }

                  }

                  Thread.Sleep(1);
               }
               catch (Exception e)
               {
                  Console.WriteLine(e);
               }

            }
            Console.WriteLine("Thread exit!");
         }
      }
   }
}
