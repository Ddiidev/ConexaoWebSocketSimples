namespace ResolutiServiceClient;

using ResolutiServiceClient.WebSocket;

internal class Program
{
	static void Main(string[] args)
	{
		var server = new WebSocketServer();
		server.Start("http://+:30505/");
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
	}
}
