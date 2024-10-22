namespace ChattingServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Server server = new Server();
            
            server.Init();
            server.Bind();

            server.Run();

        }
    }
}