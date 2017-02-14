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
    class Program
    {
        public static void Main(string[] args)
        {
            UserLoginRequest request = UserLoginRequest.CreateBuilder().SetUname("tester1").SetUpwd("tester1").Build();

            Console.WriteLine(request);

            TcpClient tc = new TcpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 26666);

            tc.Connect(ip);
            Console.WriteLine("Connected.");

            sendMessage(tc, request);

            receiveMessage(tc);

            Thread.Sleep(30000);
        }

        public static void sendMessage(TcpClient client, UserLoginRequest request)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                CodedOutputStream os = CodedOutputStream.CreateInstance(stream);
                //一定要去看它的代码实现，
                os.WriteMessageNoTag(request);
                /**
                * WriteMessageNoTag 等价于 WriteVarint32, WriteByte(byte[])
                * 也就是：变长消息头 + 消息体
                */

                os.Flush();

                byte[] data = stream.ToArray();
                //client.Send(new ArraySegment<byte>(data));
                client.GetStream().Write(data, 0, data.Length);

            }
        }

        public static void receiveMessage(TcpClient client)
        {

            byte[] buffer = new byte[1024];

            //client.Client.ReceiveTimeout = 20;

            while (true)
            {
                NetworkStream netStream = client.GetStream();
                //netStream.ReadTimeout = 200;
                byte[] fullData = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    //Have rest data
                    if(fullData != null)
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
                                read = client.GetStream().Read(buffer, 0, buffer.Length);
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
                            } else
                            {
                                Console.WriteLine("read completed");
                            }

                            // ms.Position = 0;
                            //Console.WriteLine(ms.Length);

                        } while (netStream.DataAvailable && read > 0);



                        fullData = ms.ToArray();
                        int totalMessageSize = fullData.Length;
                        CodedInputStream stream = CodedInputStream.CreateInstance(fullData);

                        while(!stream.IsAtEnd)
                        {
                            int bodySize = (int)stream.ReadRawVarint32();
                            Console.WriteLine("body size " + bodySize);
                            //Validate message correct
                            if (bodySize <= 0 || bodySize > fullData.Length)
                            {
                                Console.WriteLine("Wrong data !");
                                continue;
                            }

                            if(bodySize <= (totalMessageSize - stream.Position))
                            {
                                byte[] body = stream.ReadRawBytes(bodySize);

                                UserLoginResponse response = UserLoginResponse.ParseFrom(body);

                                Console.WriteLine("response " + response);
                            } else
                            {
                                //Rest data not enough
                                long remainSize = totalMessageSize - stream.Position;
                                byte[] restData = stream.ReadRawBytes((int)remainSize);
                                fullData = restData;
                                break;
                            }


                        }

                       


                    }

                }

                Thread.Sleep(500);
            }

        }
    }
}
