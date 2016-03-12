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
    /// Provides a game-oriented adapter to the P2PSessionClient class. 
    /// </summary>
    public sealed class ClientCommunicator : IClientCommunicator
    {
        /// <summary>
        /// Represents the participant.
        /// </summary>
        private UdpParticipant _participant = new UdpParticipant();

        /// <summary>
        /// The communication channel to listen for messages from the game host.
        /// </summary>
        private ICommunicationChannel _participantCommunicationChannel;

        /// <summary>
        /// The communication channel to send messages to the game host.
        /// </summary>
        private ICommunicationChannel _managerCommunicationChannel;

        /// <summary>
        /// The guid of the connected game host.
        /// </summary>
        private Guid _managerGuid;

        public event EventHandler GameAvailable = delegate { };

        public event EventHandler<QuestionEventArgs> NewQuestionAvailable = delegate { };

        public event EventHandler<PlayerJoinedEventArgs> PlayerJoined;

        public ClientCommunicator()
        {
            this._participant.ManagerFound += ((sender, e) =>
            {
                // Found a game host.
                this._managerCommunicationChannel = _participant.CreateCommunicationChannel(e.Id);
                this._managerGuid = e.Id;
                this.GameAvailable(this, EventArgs.Empty);
            });

            this._participantCommunicationChannel = new TcpCommunicationChannel();
            this._participantCommunicationChannel.MessageReceived += ((sender, e) =>
            {
                object message = new Question();
                e.GetDeserializedMessage(ref message);

                NewQuestionAvailable(this,
                    new QuestionEventArgs { Question = message as Question });
            });
        }

        public async Task InitializeAsync()
        {
            // Start listening for UDP advertisers.
            await this._participant.StartListeningAsync();

            // Start listening for TCP messages.
            await this._participantCommunicationChannel.StartListeningAsync();
        }

        public async Task JoinGameAsync(string playerName)
        {
            this._participant.ListenerMessage = playerName;
            await this._participant.ConnectToManagerAsync(_managerGuid);

            // Alert the ViewModel that the player has joined the game successfully.
            PlayerJoined(this, new PlayerJoinedEventArgs() { IsJoined = true });
        }

        public async Task LeaveGameAsync(string playerName)
        {
            HostCommand command = new HostCommand()
            {
                Command = Command.Leave,
                PlayerName = playerName
            };

            await this._managerCommunicationChannel
                .SendRemoteMessageAsync(command);
        }

        public async Task AnswerQuestionAsync(string playerName, int option)
        {
            HostCommand command = new HostCommand()
            {
                Command = Command.Answer,
                PlayerName = playerName,
                QuestionAnswer = option
            };

            await this._managerCommunicationChannel
                .SendRemoteMessageAsync(command);
        }
    }
}
