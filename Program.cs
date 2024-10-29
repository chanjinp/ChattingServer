namespace ChattingServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Server server = new Server();
            
            server.Init();
            server.Bind();

            //server.Run();

            await Task.Run(() => server.Run());
        }
    }
}