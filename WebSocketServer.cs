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

				// When calling `AcceptWebSocketAsync` the subprotocol must be specified.
				// This sample assumes that no subprotocol was requested.
				webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
				Interlocked.Increment(ref count);
				Console.WriteLine("Processed: {0}", count);

			}
			catch (Exception e)
			{

				// The upgrade process failed somehow.
				// For simplicity lets assume it was a failure on
				// the part of the server and indicate this using 500.
				listenerContext.Response.StatusCode = 500;
				listenerContext.Response.Close();
				Console.WriteLine("Exception: {0}", e);
				return;

			}

			System.Net.WebSockets.WebSocket webSocket = webSocketContext.WebSocket;

			try
			{

				// The buffer will be reused as we only need to hold on to the data
				// long enough to send it back to the sender (this is an echo server).
				byte[] receiveBuffer = new byte[1024];

				// loop de novos dados
				while (webSocket.State == WebSocketState.Open)
				{

					WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
						new ArraySegment<byte>(receiveBuffer),
						CancellationToken.None // We are not using timeouts
					);

					// client requested the connection to close
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
						var data = Encoding.UTF8.GetBytes("hello!");

						await webSocket.SendAsync(
					new ArraySegment<byte>(data, 0, data.Length),
							WebSocketMessageType.Text,
							receiveResult.EndOfMessage,
							CancellationToken.None
						);
						// we are not handling text in this example so we close the connection
						//await webSocket.CloseAsync(
						//	WebSocketCloseStatus.InvalidMessageType,
						//	"Cannot accept text frame",
						//	CancellationToken.None
						//);
					}
					else
					{
						// Note the use of the `EndOfMessage` flag on the receive result.
						// This means that if this echo server is sent one continuous stream
						// of binary data (with EndOfMessage always false) it will just stream
						// back the same thing.
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

				// Pretty much any exception that occurs when calling `SendAsync`,
				// `ReceiveAsync` or `CloseAsync` is unrecoverable in that it will abort
				// the connection and leave the `WebSocket` instance in an unusable state.
				Console.WriteLine("Exception: {0}", e);

			}
			finally
			{

				// Clean up by disposing the WebSocket once it is closed/aborted.
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