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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace NetworkHelper
{
    public class UdpManager : SessionManager
    {
        /// <summary>
        /// The default timing interval (in milliseconds) to send an advertising message.
        /// </summary>
        private const int ADVERTISING_INTERVAL = 500;

        /// <summary>
        /// The default message to send when advertising.
        /// </summary>
        private const string ADVERTISING_MESSAGE = "Advertiser";

        /// <summary>
        /// The default port to use when advertising for UDP multicast messages. 
        /// This port was chosen randomly in the ephemeral port range.
        /// </summary>
        private const string UDP_COMMUNICATION_PORT = "56788";

        /// <summary>
        /// The default IP to use when advertising UDP multicast messages. 
        /// This IP was chosen randomly as part of the multicast IP range.
        /// </summary>
        private const string UDP_MULTICAST_IP = "237.1.3.37";

        /// <summary>
        /// The timer that will cause the advertiser to send a UDP multicast message every AdvertisingInterval milliseconds.
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// The socket of the Advertiser.
        /// </summary>
        public DatagramSocket AdvertiserSocket { get; set; }

        /// <summary>
        /// The port that the Advertiser will be sending UDP messages to and accept message from.
        /// </summary>
        public string AdvertiserPort { get; set; } = UDP_COMMUNICATION_PORT;

        /// <summary>
        /// The multicast group that the Advertiser will be sending UDP messages to.
        /// </summary>
        public HostName AdvertiserGroupHost { get; set; } = new HostName(UDP_MULTICAST_IP);

        /// <summary>
        /// The message that will be sent when advertising.
        /// </summary>
        public string AdvertiserMessage { get; set; } = ADVERTISING_MESSAGE;

        /// <summary>
        /// The timing interval (in milliseconds) to send an advertising message.
        /// </summary>
        public int AdvertiserInterval { get; set; } = ADVERTISING_INTERVAL;

        /// <summary>
        /// Creates a new UDP socket and starts advertising the AdvertisingMessage to the AdvertiserPort and AdvertiserGroupHost
        /// </summary>
        public override async Task<bool> StartAdvertisingAsync()
        {
            if (AdvertiserSocket == null)
            {
                AdvertiserSocket = new DatagramSocket();

                // Attach event handler and start listening on given port.
                AdvertiserSocket.MessageReceived += MessageToConnectReceivedFromParticipantAsync;
                await AdvertiserSocket.BindServiceNameAsync(AdvertiserPort);

                // Start the timer, to send a message every X milliseconds.
                _timer = new Timer(async state => await SendMessageAsync(), null, 0, AdvertiserInterval);

                return true;
            }

            return false;
        }

        public override bool StopAdvertising()
        {
            if (AdvertiserSocket != null)
            {
                AdvertiserSocket.MessageReceived -= MessageToConnectReceivedFromParticipantAsync;
                AdvertiserSocket.Dispose();
                AdvertiserSocket = null;
                _timer.Dispose();
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
            channel.RemoteHostname = ((UdpParticipantInformation)Participants[participant]).Host;
            return channel;
        }

        /// <summary>
        /// The private method that sends an "advertising" message to the multicast group.
        /// </summary>
        private async Task SendMessageAsync()
        {
            // Connect to a multicast group IP and send a message to the group.
            Stream outStream = (await AdvertiserSocket.GetOutputStreamAsync(AdvertiserGroupHost, AdvertiserPort)).AsStreamForWrite();

            using (var writer = new StreamWriter(outStream))
            {
                await writer.WriteLineAsync(AdvertiserMessage);
                await writer.FlushAsync();
            }
        }

        /// <summary>
        /// When a new message is received, the participant is added to the list of Participants.
        /// </summary>
        private async void MessageToConnectReceivedFromParticipantAsync(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var participant = new UdpParticipantInformation { Host = args.RemoteAddress };

            // Read the subscriber's message.
            using (var reader = new StreamReader(args.GetDataStream().AsStreamForRead()))
            {
                string message = await reader.ReadLineAsync();
                
                // Add the participant.
                base.AddParticipant(participant, message);
            }
        }

    }

    public class UdpParticipantInformation
    {
        public HostName Host { get; set; }

        public override bool Equals(object obj)
        {
            var objCast = obj as UdpParticipantInformation;
            return objCast != null ? Host.IsEqual(objCast.Host) : false;
        }

        public override int GetHashCode() => Host.GetHashCode();
    }
}
