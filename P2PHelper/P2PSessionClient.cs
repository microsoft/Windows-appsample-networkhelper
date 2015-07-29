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
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace P2PHelper
{
    public class P2PSessionClient : P2PSession, IDisposable
    {
        public event EventHandler HostAvailable = delegate { };

        private P2PHost ConnectedHost { get; set; }

        // An instance of the TCP listener, kept for cleanup purposes.
        private StreamSocketListener SessionListener { get; set; }

        public P2PSessionClient(P2PSessionConfigurationData config) : base(config)
        {
            this.SessionListener = new StreamSocketListener();
        }

        public void Dispose()
        {
            this.SessionListener.Dispose();
            this.SessionListener = null;
        }

        public async Task ListenForP2PSession(SessionType sessionType)
        {
            if (this.SessionListener == null) return;
            if (sessionType != SessionType.LocalNetwork) throw new NotSupportedException(
                "SessionType.LocalNetwork is the only SessionType supported.");

            this.SessionHost = false;
            this.SessionListener.ConnectionReceived += async (s, e) => 
                this.OnMessageReceived(await this.RetrieveMessage(e.Socket));
            await this.SessionListener.BindEndpointAsync(null, this.Settings.tcpPort);
            this.InitializeNetworkInfo();

            await this.InitializeMulticast(remoteAddress =>
            {
                if (this.ConnectedHost.hostTcpIP == null)
                {
                    this.ConnectedHost = new P2PHost { hostTcpIP = remoteAddress };
                    this.OnHostAvailable();
                }
            });
        }

        // Send an object.
        public async Task<bool> SendMessage(object message, Type type = null)
        {
            return await base.SendMessage(message, this.ConnectedHost.hostTcpIP, 
                this.Settings.tcpPort, type ?? typeof(object));
        }

        protected void OnHostAvailable()
        {
            this.HostAvailable(this, EventArgs.Empty);
        }

    }
}