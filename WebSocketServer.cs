using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace ResolutiServiceClient.WebSocket
{

	// This console application uses `HttpListener` to receive WebSocket connections.
	// It expects to receive binary data and it streams back the data as it receives it.
	// The [source](https://github.com/paulbatum/WebSocket-Samples) for this sample
	// is on GitHub.
	public class WebSocketServer
	{

		public async void Start(string listenerPrefix)
		{

			HttpListener listener = new HttpListener();
			listener.Prefixes.Add(listenerPrefix);
			listener.Start();
			Console.WriteLine("Listening...");

			// loop de novas conexões
			while (true)
			{
				HttpListenerContext listenerContext = await listener.GetContextAsync();
				if (listenerContext.Request.IsWebSocketRequest)
				{
					ProcessRequest(listenerContext);
				}
				else
				{
					listenerContext.Response.StatusCode = 400;
					listenerContext.Response.Close();
				}
			}

		}

		private async void ProcessRequest(HttpListenerContext listenerContext)
		{

			WebSocketContext webSocketContext = null;

			try
			{
				webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
				Interlocked.Increment(ref count);
				Console.WriteLine("Processed: {0}", count);

			}
			catch (Exception e)
			{
				listenerContext.Response.StatusCode = 500;
				listenerContext.Response.Close();
				Console.WriteLine("Exception: {0}", e);
				return;

			}

			System.Net.WebSockets.WebSocket webSocket = webSocketContext.WebSocket;

			try
			{
				byte[] receiveBuffer = new byte[1024];

				while (webSocket.State == WebSocketState.Open)
				{
					receiveBuffer = new byte[1024];
					WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
						new ArraySegment<byte>(receiveBuffer),
						CancellationToken.None // We are not using timeouts
					);

					if (receiveResult.MessageType == WebSocketMessageType.Close)
					{

						await webSocket.CloseAsync(
							WebSocketCloseStatus.NormalClosure, // omits close message
							"",
							CancellationToken.None
						);

						// client sent text
					}
					else if (receiveResult.MessageType == WebSocketMessageType.Text)
					{
						var msgReceive = Encoding.ASCII.GetString(receiveBuffer);
						Console.WriteLine((msgReceive ?? "").Trim());
						var data = Encoding.UTF8.GetBytes($"ola, a sua msg: {msgReceive} [{DateTime.Now:ddd-MMM-yy HH:mm:ss}]");

						await webSocket.SendAsync(
					new ArraySegment<byte>(data, 0, data.Length),
							WebSocketMessageType.Text,
							receiveResult.EndOfMessage,
							CancellationToken.None
						);
					}
					else
					{
						await webSocket.SendAsync(
							new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count),
							WebSocketMessageType.Binary,
							receiveResult.EndOfMessage,
							CancellationToken.None
						);

					}

				}

			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: {0}", e);

			}
			finally
			{
				if (webSocket != null)
				{
					webSocket.Dispose();
				}

			}

		}

		/// <summary>
		/// Number of opened connections
		/// </summary>
		private int count = 0;

	}

	// This extension method wraps the BeginGetContext
	// and EndGetContext methods on HttpListener as a Task
	public static class HelperExtensions
	{
		public static Task GetContextAsync(this HttpListener listener)
		{
			return Task.Factory.FromAsync<HttpListenerContext>(
				listener.BeginGetContext,
				listener.EndGetContext,
				TaskCreationOptions.None
			);
		}
	}

}