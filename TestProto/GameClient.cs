using Google.ProtocolBuffers;
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
      private string host;
      private int port;
      private TcpClient channel;
      private AccountCallback accountCallback;
      private IPEndPoint ip;
      private GameClientService service;
      private bool shouldRun;
      private Thread coreThread;
      private static readonly Object _lock = new Object();
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

      public void addAccountListener(AccountCallback accountCallback)
      {
         this.accountCallback = accountCallback;
      }

      public void addTerrainListener(TerrainCallback callback)
      {
         this.service.addTerrainListener(callback);
      }

      public void Start(string host, int port)
      {
         this.host = host;
         this.port = port;
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
         this.service = new GameClientService(this);
         coreThread = new Thread(new ThreadStart(ChannelCoreHandle));
         coreThread.Name = "Client-Network-Core-Thread";
         coreThread.IsBackground = true;

         this.channel.Connect(ip);
         this.service.Start();
         shouldRun = true;
         coreThread.Start();
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

            while (shouldRun)
            {
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
                        Console.WriteLine("rest data " + fullData.Length);
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
                           Console.WriteLine("body size " + bodySize);
                           //Validate message correct
                           if (bodySize <= 0 || bodySize > fullData.Length)
                           {
                              Console.WriteLine("Wrong data !");
                              continue;
                           }

                           int messageId = (int)stream.ReadRawVarint32();

                           if (bodySize <= (totalMessageSize - stream.Position))
                           {
                              byte[] body = stream.ReadRawBytes(bodySize);

                              //IMessageLite protoMes = null;

                              switch (messageId)
                              {
                                 case (int)MessageRegistry.USERLOGINRESPONSE:
                                    this.accountCallback.OnLoginResponse(UserLoginResponse.ParseFrom(body));
                                    break;
                                 case (int)MessageRegistry.CHARACTERENTERRESPONSE:
                                    this.accountCallback.OnEnterResponse(ClientCharacterEnterEvent.ParseFrom(body));
                                    break;
                                 default:
                                    ChannelMessage channelMessage = new ChannelMessage((MessageRegistry)messageId, body);
                                    this.service.EnqueueMessage(channelMessage);
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

                  Thread.Sleep(500);
               }
               catch (Exception e)
               {
                  Console.WriteLine(e);
               }

            }
         }
      }
   }
}
