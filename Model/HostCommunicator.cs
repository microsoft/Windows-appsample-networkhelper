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

using P2PHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace QuizGame.Model
{
    /// <summary>
    /// Provides a game-oriented adapter to the P2PSessionHost class. 
    /// </summary>
    public sealed class HostCommunicator : IHostCommunicator
    {
        public event EventHandler<PlayerEventArgs> PlayerJoined = delegate { };
        public event EventHandler<PlayerEventArgs> PlayerDeparted = delegate { };
        public event EventHandler<AnswerReceivedEventArgs> AnswerReceived = delegate { };

        private P2PSessionHost Host { get; set; }
        private Action<HostCommandData, Guid>[] CommandActions { get; set; } 
        private Dictionary<string, Guid> PlayerMap { get; } = new Dictionary<string, Guid>();

        public HostCommunicator(P2PSessionHost host)
        {
            this.PlayerMap = new Dictionary<string, Guid>();
            this.CommandActions = 
                new Action<HostCommandData, Guid>[] {
                    async (data, guid) => await OnPlayerJoined(data, guid),
                    OnPlayerDeparted, OnAnswerReceived };
            this.Host = host;
        }

        private async Task OnPlayerJoined(HostCommandData commandData, Guid guid)
        {
            Guid duplicatePlayerID;

            if (!this.PlayerMap.TryGetValue(commandData.PlayerName, out duplicatePlayerID))
            {
                this.PlayerMap.Add(commandData.PlayerName, guid);
                this.PlayerJoined(this, new PlayerEventArgs { PlayerName = commandData.PlayerName });
                await this.SendJoinStatusMessageToPlayer(guid, true);
            }
            else
            {
                await this.SendJoinStatusMessageToPlayer(guid, false);
            }
        }

        public void OnPlayerDeparted(HostCommandData commandData, Guid guid)
        {
            this.PlayerDeparted(this, new PlayerEventArgs { PlayerName = commandData.PlayerName });
            this.PlayerMap.Remove(commandData.PlayerName);
            this.Host.RemoveClient(guid);
        }
    
        public void OnAnswerReceived(HostCommandData commandData, Guid guid)
        {
            this.AnswerReceived(this, new AnswerReceivedEventArgs {
                PlayerName = commandData.PlayerName, AnswerIndex = (int)commandData.Data });
        }

        // Start broadcasting, start accepting players.
        public async Task EnterLobby()
        {
            if (await Host.CreateP2PSession(P2PSession.SessionType.LocalNetwork))
            {
                this.Host.MessageReceived += (s, e) => 
                {
                    var commandData = e.DeserializedMessage<HostCommandData>();
                    this.CommandActions[(int)commandData.Command](commandData, e.ClientID);
                };
                this.Host.StartAcceptingConnections();
            }
        }

        // Stop broadcasting, stop accepting players.
        public void LeaveLobby()
        {
            this.Host.StopAcceptingConnections();
        }

        public async Task SendQuestion(Question question)
        {
            List<Guid> clientList = new List<Guid>();

            foreach (var key in this.PlayerMap.Keys)
            {
                clientList.Add(this.PlayerMap[key]);
            }

            var messageTasks = clientList.Select(client => this.Host.SendMessage(client, 
                new HostMessage { MessageType = HostMessageType.Question, Question = question }, typeof(HostMessage)));

            // When all the tasks complete, return true if they all succeeded. 
            bool status = (await Task.WhenAll(messageTasks)).All(value => { return value; });
        }

        private async Task SendJoinStatusMessageToPlayer(Guid playerID, bool isJoined)
        {
            await this.Host.SendMessage(playerID, 
                new HostMessage { MessageType = HostMessageType.JoinStatus, IsJoined = isJoined }, typeof(HostMessage));
        }

    }
}
