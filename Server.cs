using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ChattingServer
{
    public class Server
    {
        private Socket m_Socket;
        private int m_Port;
        private string m_Host;
        private bool isOpen;
        private int tryAccept = 0;
        private List<Socket> c_Sockets;
        private byte[] m_Buffer;

        public IPAddress GetIPAdress()
        {
            /*IPHostEntry hostInfo = Dns.GetHostEntry(m_Host);

            foreach (IPAddress ip in hostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }*/
            return IPAddress.Parse("127.0.0.1");
        }

        public void Init()
        {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Port = 9999;
            m_Host = Dns.GetHostName();
            c_Sockets = new List<Socket>();
            m_Buffer = new byte[1024];
            isOpen = false;
        }

        public void Bind(int backlog = 10)
        {
            IPAddress m_IP = GetIPAdress();
            IPEndPoint s_Ep = new IPEndPoint(m_IP, m_Port);

            Console.WriteLine("================= [Server Bind] =================");
            m_Socket.Bind(s_Ep);

            Console.WriteLine("================= [Server IP: {0} / PORT: {1}] =================", m_IP.ToString(), m_Port);

            Console.WriteLine("================= [Listen: {0}] =================", backlog);
            m_Socket.Listen(backlog);

            isOpen = true;
        }

        public void Run()
        {
            while (isOpen)
            {
                Task<Socket> task = Task.Run(() => AcceptClient()); //Thread를 할당 받아서 Accept 완료 후
                Socket clientSocket = task.Result; // Accept한 client를 활용 -> 할당된 Thread는 회수

                while (clientSocket.Connected)
                {
                    Task.Run(() => ReceiveMessage(clientSocket)); //연결된 클라이언트를 활용
                    //TODO 소켓이 수신한 메시지에 따라 닫히더라도 브로드 캐스트가 진행됨 -> 문제 해결 해야함
                    Task.Run(() => BroadCastMessage());
                }

                Console.WriteLine("Socket closed");
            }

            Console.WriteLine("================= [Server Down] =================");
            Close();
        }

        public Socket AcceptClient()
        {
            Socket client = null;
            try
            {
                if (tryAccept == c_Sockets.Count)
                {
                    tryAccept++;
                    Console.WriteLine("[클라이언트 대기중 ...]");
                    client = m_Socket.Accept(); //서버 소켓에서 수신
                    Console.WriteLine("[클라이언트 수신 완료...]");
                    c_Sockets.Add(client); //리스트에 추가
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("[Accept Error]: " + ex.Message);

            }
            return client;

        }

        public Socket FindBySocketIP(string ip)
        {
            Socket target = null;

            foreach (Socket socket in c_Sockets)
            {
                string socketIP = socket.RemoteEndPoint.ToString();
                if (socketIP != null && ip == socketIP)
                {
                    target = socket;
                    break;
                }

            }
            return target;
        }

        public int FindSocketIndex(string ip)
        {
            for (int i = 0; i < c_Sockets.Count; i++)
            {
                Socket socket = c_Sockets[i];

                string socketIP = socket.RemoteEndPoint.ToString();

                if (socketIP != null && ip == socketIP)
                {
                    return i;
                }
            }
            return -1;
        }

        public async Task ReceiveMessage(Socket client)
        {

            await Task.Run(() =>
            {
                int receive = client.Receive(m_Buffer);
                if (receive > 0)
                {
                    string msg = Encoding.UTF8.GetString(m_Buffer, 0, receive);
                    Console.WriteLine("[ClientMsg]: " + msg);

                    if (msg.Equals("Exit"))
                    {
                        c_Sockets.Remove(client); //관리 리스트에서 삭제
                        Console.WriteLine("[Client {0}: Close]", client.RemoteEndPoint.ToString());
                        client.Close();//클라이언트 소켓 종료
                        Console.WriteLine("[End ReceiveMessage]");
                    }
                }
            });

        }

        public async Task BroadCastMessage()
        {
            string msg = await Console.In.ReadLineAsync();
            await Task.Run(() =>
            {
                foreach (Socket socket in c_Sockets)
                {
                    Send(msg, socket);
                    Console.WriteLine("[Send Message]");
                }
            });


        }
        public void ClientToMessage(Socket socket)
        {

        }

        public void Send(string msg, Socket client)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(msg);

            NetworkStream ns = new NetworkStream(client);

            ns.Write(bytes);

            ns.Close();
            ns.Dispose();
        }

        public void Close()
        {
            m_Socket.Close();
            m_Socket.Dispose();
        }
    }
}
