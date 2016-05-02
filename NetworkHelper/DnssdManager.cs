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
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.ServiceDiscovery.Dnssd;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using System.IO;

namespace NetworkHelper
{
    public class DnssdManager : SessionManager
    {
        /// <summary>
        /// The message that is received indicating a test.
        /// </summary>
        private const string TEST_MESSAGE = "connection_test";

        /// <summary>
        /// The default port to use when connecting back to the manager.
        /// </summary>
        private const string DEFAULT_PORT = "56788";

        /// <summary>
        /// The default instance name when registering the DNS-SD service.
        /// </summary>
        private const string INSTANCE_NAME = "DnssdManager";

        /// <summary>
        /// The network protocol that will be accepting connections for responses.
        /// </summary>
        private const string NETWORK_PROTOCOL = "_tcp";

        /// <summary>
        /// The domain of the DNS-SD registration.
        /// </summary>
        private const string DOMAIN = "local";

        /// <summary>
        /// The service type of the DNS-SD registration.
        /// </summary>
        private const string SERVICE_TYPE = "_p2phelper";

        /// <summary>
        /// The name of the DNS-SD service that is being registered. You can provide any name that you want.
        /// </summary>
        public string InstanceName { get; set; } = INSTANCE_NAME;

        /// <summary>
        /// The DNS-SD service object.
        /// </summary>
        private DnssdServiceInstance _service;

        /// <summary>
        /// The TCP socket that will be accepting connections for the DSN-SD service responses.
        /// </summary>
        private StreamSocketListener _socket;

        /// <summary>
        /// The port to use when connecting back to the host.
        /// </summary>
        public string Port { get; set; } = DEFAULT_PORT;

        /// <summary>
        /// Registers the DNS-SD service.
        /// </summary>
        public override async Task<bool> StartAdvertisingAsync()
        {
            if (_socket == null && _service == null)
            {
                _socket = new StreamSocketListener();
                _socket.ConnectionReceived += MessageToConnectReceivedFromParticipantAsync;
                await _socket.BindServiceNameAsync(Port);

                _service = new DnssdServiceInstance(
                    $"{InstanceName}.{SERVICE_TYPE}.{NETWORK_PROTOCOL}.{DOMAIN}.",
                    NetworkInformation.GetHostNames().FirstOrDefault(x => x.Type == HostNameType.DomainName && x.RawName.Contains("local")),
                    UInt16.Parse(_socket.Information.LocalPort)
                );

                var operationStatus = await _service.RegisterStreamSocketListenerAsync(_socket);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Unregisters and removes the DNS-SD service. If this method is not called, 
        /// the registration will remain discoverable, even if the app is not running.
        /// </summary>
        public override bool StopAdvertising()
        {
            if (_socket != null && _service != null)
            {
                _socket.ConnectionReceived -= MessageToConnectReceivedFromParticipantAsync;
                _socket.Dispose();
                _socket = null;
                _service = null;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a TcpCommunicationChannel object and returns it so that app developers can send custom TCP messages to the manager.
        /// Returns a null remote host name in TcpCommunicationChannel object if the manager didn't exist.
        /// </summary>
        public override ICommunicationChannel CreateCommunicationChannel(Guid participant, int flags = 0)
        {
            TcpCommunicationChannel channel = new TcpCommunicationChannel();
            channel.RemoteHostname = ((DnssdParticipantInformation)Participants[participant]).Host;
            return channel;
        }

        /// <summary>
        /// When a new message is received, the participant is added to the list of Participants.
        /// </summary>
        private async void MessageToConnectReceivedFromParticipantAsync(
            StreamSocketListener sender, 
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var participant = new DnssdParticipantInformation { Host = args.Socket.Information.RemoteAddress };

            // Read the subscriber's message.
            using (var reader = new StreamReader(args.Socket.InputStream.AsStreamForRead()))
            {
                string message = await reader.ReadLineAsync();

                if (message != TEST_MESSAGE)
                {
                    // Add the participant.
                    base.AddParticipant(participant, message);
                }
            }
        }
    }

    public class DnssdParticipantInformation
    {
        public HostName Host { get; set; }

        public override bool Equals(object obj)
        {
            var objCast = obj as DnssdParticipantInformation;
            return objCast != null ? Host.IsEqual(objCast.Host) : false;
        }

        public override int GetHashCode() => Host.GetHashCode();
    }
}
