/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    UdpInputTransport.cs
   CONTEXT: UDP I/O.

   TASK:
   Implement UDP transport. CRITICAL: Include the 'SIO_UDP_CONNRESET' (-1744830452) fix for Windows to prevent host crash on client disconnect. Use zero-alloc buffers.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;

    public class UdpInputTransport : IDisposable
    {
        private const int SIO_UDP_CONNRESET = -1744830452;
        private const int BUFFER_SIZE = 1024;
        private const int MAX_PACKET_SIZE = 512;

        private UdpClient udpClient;
        private IPEndPoint localEndPoint;
        private IPEndPoint remoteEndPoint;
        private byte[] receiveBuffer;
        private bool isDisposed;

        public UdpInputTransport(int localPort, string remoteAddress, int remotePort)
        {
            receiveBuffer = new byte[BUFFER_SIZE];
            localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);

            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(localEndPoint);

            // Apply Windows-specific fix to prevent crash on client disconnect
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    uint dummy = 0;
                    byte[] inValue = BitConverter.GetBytes(false);
                    byte[] outValue = BitConverter.GetBytes(false);
                    udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, inValue, outValue);
                }
                catch (SocketException)
                {
                    // Ignore if not supported
                }
            }
        }

        public bool SendInputs(byte[] inputs, int frame)
        {
            if (inputs == null || inputs.Length > MAX_PACKET_SIZE)
                return false;

            try
            {
                // Create packet: frame (4 bytes) + inputs
                byte[] packet = new byte[4 + inputs.Length];
                BitConverter.GetBytes(frame).CopyTo(packet, 0);
                inputs.CopyTo(packet, 4);

                udpClient.Send(packet, packet.Length, remoteEndPoint);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public bool TryReceive(out byte[] inputs, out int frame, out int playerIndex)
        {
            inputs = null;
            frame = 0;
            playerIndex = 0;

            try
            {
                if (udpClient.Available > 0)
                {
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref sender);

                    if (data.Length >= 8) // Minimum: frame(4) + playerIndex(4) + at least 1 byte of inputs
                    {
                        frame = BitConverter.ToInt32(data, 0);
                        playerIndex = BitConverter.ToInt32(data, 4);

                        int inputLength = data.Length - 8;
                        if (inputLength > 0)
                        {
                            inputs = new byte[inputLength];
                            Buffer.BlockCopy(data, 8, inputs, 0, inputLength);
                            return true;
                        }
                    }
                }
            }
            catch (SocketException)
            {
                // Connection reset or other network error
            }

            return false;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                udpClient?.Close();
                udpClient?.Dispose();
            }
        }

        ~UdpInputTransport()
        {
            Dispose();
        }
    }
}
