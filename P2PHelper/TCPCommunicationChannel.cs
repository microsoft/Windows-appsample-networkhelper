//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace P2PHelper
{
    public class TcpCommunicationChannel : ICommunicationChannel
    {
        /// <summary>
        /// The default port.
        /// </summary>
        private const string TCP_COMMUNICATION_PORT = "1338";

        /// <summary>
        /// The socket connection to the remote TCP server.
        /// </summary>
        private StreamSocket _remoteSocket;

        /// <summary>
        /// The local socket connection that will be listening for TCP messages.
        /// </summary>
        private StreamSocketListener _localSocket;

        /// <summary>
        /// The port number that the remote TCP server and local TCP server is listening to.
        /// </summary>
        public string CommunicationPort { get; set; } = TCP_COMMUNICATION_PORT;

        /// <summary>
        /// The hostname of the remote TCP server that is listeneing for TCP connections.
        /// </summary>
        public Windows.Networking.HostName RemoteHostname { get; set; }

        /// <summary>
        /// An event indicating that a message was received from the remote TCP server.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived = delegate { };

        /// <summary>
        /// Sends a message to the connected RemoteSocket.
        /// </summary>
        public async Task SendRemoteMessageAsync(object message)
        {
            // Connect to the remote host to ensure that the connection exists.
            await ConnectToRemoteAsync();

            using (var writer = new StreamWriter(_remoteSocket.OutputStream.AsStreamForWrite()))
            {
                await writer.WriteLineAsync(message.ToString());
                await writer.FlushAsync();
            }

            // Disconnect from the remote host.
            DisconnectFromRemote();
        }

        /// <summary>
        /// Creates a TCP socket and binds to the CommunicationPort.
        /// </summary>
        public async Task StartListeningAsync()
        {
            _localSocket = _localSocket ?? new StreamSocketListener();
            _localSocket.ConnectionReceived += LocalSocketConnectionReceived;
            await _localSocket.BindServiceNameAsync(CommunicationPort);
        }

        /// <summary>
        /// Disposes of the TCP socket and sets it to null.
        /// </summary>
        public void StopListening()
        {
            if (_localSocket == null)
            {
                throw new InvalidOperationException("The TCP Socket is not listening for connections");
            }

            _localSocket.ConnectionReceived -= LocalSocketConnectionReceived;
            _localSocket.Dispose();
            _localSocket = null;
        }

        /// <summary>
        /// The event handler for when a TCP connection has been received.
        /// </summary>
        private async void LocalSocketConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            using (var reader = new StreamReader(args.Socket.InputStream.AsStreamForRead()))
            {
                // Read the message.
                string message = await reader.ReadLineAsync();

                // Notify subscribers that a message was received.
                MessageReceived(this, new MessageReceivedEventArgs { Message = message });
            }
        }

        /// <summary>
        /// Creates a RemoteSocket and establishes a connection to the RemoteHostname on CommunicationPort.
        /// </summary>
        private async Task ConnectToRemoteAsync()
        {
            _remoteSocket = _remoteSocket ?? new StreamSocket();
            await _remoteSocket.ConnectAsync(RemoteHostname, CommunicationPort);
        }

        /// <summary>
        /// Disposes the RemoteSocket.
        /// </summary>
        private void DisconnectFromRemote()
        {
            _remoteSocket.Dispose();
            _remoteSocket = null;
        }
    }
}
