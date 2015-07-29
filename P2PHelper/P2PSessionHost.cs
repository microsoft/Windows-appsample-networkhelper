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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Networking.Sockets;

namespace P2PHelper
{
    public class P2PSessionHost : P2PSession, IDisposable
    {

        private Dictionary<Guid, P2PClient> ClientMap { get; set; }

        private StreamSocketListener SessionListener { get; set; }

        private Timer Timer { get; set; }

        public P2PSessionHost(P2PSessionConfigurationData config) : base(config)
        {
            this.SessionListener = new StreamSocketListener();
            this.ClientMap = new Dictionary<Guid, P2PClient>();
        }

        public void Dispose()
        {
            this.SessionListener.Dispose();
            this.SessionListener = null;
        }

        public async Task<bool> CreateP2PSession(SessionType type)
        {
            if (this.SessionListener == null) return false;
            if (type != SessionType.LocalNetwork) throw new NotSupportedException(
                "SessionType.LocalNetwork is the only SessionType supported.");

            this.SessionHost = true;
            this.SessionListener.ConnectionReceived += async (s, e) => await OnConnectionReceived(e.Socket);  
            await this.SessionListener.BindEndpointAsync(null, Settings.tcpPort);
            this.InitializeNetworkInfo();
            return await this.InitializeMulticast(null);
        }

        public bool RemoveClient(Guid clientID)
        {
            return this.ClientMap.Remove(clientID);
        }

        private bool AcceptingConnections { get; set; }
        public void StartAcceptingConnections()
        {
            AcceptingConnections = true;
            this.Timer = new Timer(async state => await SendMulticastMessage(""), null, 0, 500);   
        }

        public void StopAcceptingConnections()
        {
            AcceptingConnections = false;
            this.Timer.Dispose();
        }

        private async Task OnConnectionReceived(StreamSocket socket)
        {
            byte[] message = await RetrieveMessage(socket);
            var newClient = new P2PClient { clientTcpIP = socket.Information.RemoteAddress.ToString() };
            if (AcceptingConnections)
            { 
                if (GetGuid(newClient).ToString() == (new Guid()).ToString())
                {
                    Guid newGuid = Guid.NewGuid();
                    this.ClientMap.Add(newGuid, newClient);
                    this.OnConnectionComplete(newGuid);
                }
            }
            this.OnMessageReceived(message, GetGuid(newClient));
        }

        private Guid GetGuid(P2PClient client)
        {
            return this.ClientMap.FirstOrDefault(
                kvp => kvp.Value.clientTcpIP == client.clientTcpIP).Key;
        }

        protected async Task SendMulticastMessage(string output)
        {
            using (var multicastOutput = await this.MulticastSocket.GetOutputStreamAsync(
                new Windows.Networking.HostName(Settings.multicastIP), 
                this.MulticastSocket.Information.LocalPort))
            {
                await multicastOutput.WriteAsync(Encoding.UTF8.GetBytes(output).AsBuffer());
            }
        }

        public async Task<bool> SendMessage(Guid clientID, object message, Type type = null)
        {
            P2PClient client;
            if (this.ClientMap.TryGetValue(clientID, out client))
            {
                return await base.SendMessage(message, client.clientTcpIP, Settings.tcpPort, type ?? typeof(object));
            }
            return false;
        }

        public async Task<bool> SendMessageToAll(object message, Type type = null)
        {
            var messageTasks = this.ClientMap.Keys.Select(guid => this.SendMessage(guid, message, type));

            // When all the tasks complete, return true if they all succeeded. 
            return (await Task.WhenAll(messageTasks)).All(value => { return value; });
        }
    }
}