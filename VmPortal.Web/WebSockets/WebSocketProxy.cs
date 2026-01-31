using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace VmPortal.Web.WebSockets
{
    public static class WebSocketProxy
    {
        public static async Task ProxyAsync(
            WebSocket clientSocket,
            WebSocket proxmoxSocket,
            CancellationToken cancellationToken)
        {
            if (clientSocket == null)
            {
                throw new ArgumentNullException(nameof(clientSocket));
            }

            if (proxmoxSocket == null)
            {
                throw new ArgumentNullException(nameof(proxmoxSocket));
            }

            byte[] buffer = new byte[8192];

            Task clientToProxmox = PumpAsync(clientSocket, proxmoxSocket, buffer, cancellationToken);
            Task proxmoxToClient = PumpAsync(proxmoxSocket, clientSocket, buffer, cancellationToken);

            await Task.WhenAny(clientToProxmox, proxmoxToClient);

            try
            {
                if (clientSocket.State == WebSocketState.Open)
                {
                    await clientSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Proxy closing",
                        cancellationToken);
                }
            }
            catch
            {
                // ignore close errors
            }

            try
            {
                if (proxmoxSocket.State == WebSocketState.Open)
                {
                    await proxmoxSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Proxy closing",
                        cancellationToken);
                }
            }
            catch
            {
                // ignore close errors
            }
        }

        private static async Task PumpAsync(
            WebSocket source,
            WebSocket destination,
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

            while (!cancellationToken.IsCancellationRequested &&
                   source.State == WebSocketState.Open &&
                   destination.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await source.ReceiveAsync(segment, cancellationToken);
                }
                catch
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        await destination.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            result.CloseStatusDescription,
                            cancellationToken);
                    }
                    catch
                    {
                        // ignore
                    }

                    break;
                }

                ArraySegment<byte> outSegment = new ArraySegment<byte>(buffer, 0, result.Count);

                try
                {
                    await destination.SendAsync(
                        outSegment,
                        result.MessageType,
                        result.EndOfMessage,
                        cancellationToken);
                }
                catch
                {
                    break;
                }
            }
        }
    }
}
