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
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace NetworkHelper
{
    public class DnssdParticipant : SessionParticipant
    {
        /// <summary>
        /// The message to send in TestConnectionAsync
        /// </summary>
        private const string TEST_MESSAGE = "connection_test";

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
        /// The protocol ID that identifies DNS-SD.
        /// </summary>
        private const string PROTOCOL_GUID = "{4526e8c1-8aac-4153-9b16-55e86ada0e54}";

        /// <summary>
        /// The host name property.
        /// </summary>
        private const string HOSTNAME_PROPERTY = "System.Devices.Dnssd.HostName";

        /// <summary>
        /// The service name property.
        /// </summary>
        private const string SERVICENAME_PROPERTY = "System.Devices.Dnssd.ServiceName";

        /// <summary>
        /// The instance name property.
        /// </summary>
        private const string INSTANCENAME_PROPERTY = "System.Devices.Dnssd.InstanceName";

        /// <summary>
        /// The IP address property.
        /// </summary>
        private const string IPADDRESS_PROPERTY = "System.Devices.IpAddress";

        /// <summary>
        /// The port number property.
        /// </summary>
        private const string PORTNUMBER_PROPERTY = "System.Devices.Dnssd.PortNumber";

        /// <summary>
        /// All of the properties that will be returned when a DNS-SD instance has been found. 
        /// </summary>
        private string[] _propertyKeys = new String[] {
            HOSTNAME_PROPERTY,
            SERVICENAME_PROPERTY,
            INSTANCENAME_PROPERTY,
            IPADDRESS_PROPERTY,
            PORTNUMBER_PROPERTY
        };

        /// <summary>
        /// The AQS filter string that is used to filter services to DNS-SD only.
        /// </summary>
        private string _aqsQueryString = $"System.Devices.AepService.ProtocolId:={PROTOCOL_GUID} AND " +
            $"System.Devices.Dnssd.Domain:=\"{DOMAIN}\" AND System.Devices.Dnssd.ServiceName:=\"{SERVICE_TYPE}.{NETWORK_PROTOCOL}\"";

        /// <summary>
        /// The device watcher that will be watching for DNS-SD connections.
        /// </summary>
        private DeviceWatcher _watcher;

        /// <summary>
        /// The message that will be sent when connecting to a manager.
        /// </summary>
        public string ListenerMessage { get; set; }

        /// <summary>
        /// Establishes a connection to a DNS-SD instance by connecting to it's TCP socket and sending a message.
        /// </summary>
        public override async Task ConnectToManagerAsync(Guid manager)
        {
            var subscription = base.Managers[manager] as DnssdManagerInformation;

            using (var socket = new StreamSocket())
            {
                await socket.ConnectAsync(subscription.Host, subscription.Port);

                using (var writer = new StreamWriter(socket.OutputStream.AsStreamForWrite()))
                {
                    await writer.WriteLineAsync(ListenerMessage.ToString());
                    await writer.FlushAsync();
                }
            }
        }

        /// <summary>
        /// Creates a TcpCommunicationChannel object for communication with the DNS-SD instance.
        /// </summary>
        public override ICommunicationChannel CreateCommunicationChannel(Guid manager, int flags = 0)
        {
            var managerCast = Managers[manager] as DnssdManagerInformation;
            return new TcpCommunicationChannel { RemoteHostname = managerCast == null ? null : managerCast.Host };
        }

        /// <summary>
        /// Uses a device watcher to monitor for available DSN-SD instances.
        /// </summary>
        public override async Task<bool> StartListeningAsync()
        {
            bool status = false;

            if (_watcher == null)
            {
                _watcher = DeviceInformation.CreateWatcher(
                    _aqsQueryString,
                    _propertyKeys,
                    DeviceInformationKind.AssociationEndpointService);

                _watcher.Added += async (s, a) => await OnFoundDnssdServiceAsync(a.Properties);
                _watcher.Updated += async (s, a) => await OnFoundDnssdServiceAsync(a.Properties);
                _watcher.Start();

                status = true;
            }

            await Task.CompletedTask;
            return status;
        }

        /// <summary>
        /// Stops watching for available DNS-SD instances.
        /// </summary>
        public override bool StopListening()
        {
            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher = null;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the manager to the list of Managers, when a DNS-SD instance is found.
        /// </summary>
        private async Task OnFoundDnssdServiceAsync(IReadOnlyDictionary<string, object> properties)
        {
            var host = new HostName((properties[IPADDRESS_PROPERTY] as String[])[0]);
            var port = properties[PORTNUMBER_PROPERTY].ToString();

            bool isValidConnection = await TestConnectionAsync(host, port);

            if (isValidConnection)
            {
                base.AddManager(
                    new DnssdManagerInformation { Host = host, Port = port },
                    properties[INSTANCENAME_PROPERTY].ToString()
                );
            }
        }

        /// <summary>
        /// Attempts to connect and send a test message to a TCP host for verification the host exists.
        /// </summary>
        private async Task<bool> TestConnectionAsync(HostName host, string port)
        {
            using (var socket = new StreamSocket())
            {
                try
                {
                    await socket.ConnectAsync(host, port);

                    using (var writer = new StreamWriter(socket.OutputStream.AsStreamForWrite()))
                    {
                        await writer.WriteLineAsync(TEST_MESSAGE);
                        await writer.FlushAsync();
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class DnssdManagerInformation
    {
        public HostName Host { get; set; }
        public string Port { get; set; }

        public override bool Equals(object obj)
        {
            var objCast = obj as DnssdManagerInformation;
            return objCast != null ? Host.IsEqual(objCast.Host) && Port == objCast.Port : false;
        }

        public override int GetHashCode() => (Host + Port).GetHashCode();
    }
}
