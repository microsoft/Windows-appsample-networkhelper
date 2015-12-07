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
        /// The string literal for the question command.
        /// </summary>
        private const string QUESTION_COMMAND = "question";

        /// <summary>
        /// The string literal for the join command.
        /// </summary>
        private const string JOIN_COMMAND = "join";

        /// <summary>
        /// The string literal for the answer command.
        /// </summary>
        private const string ANSWER_COMMAND = "answer";

        /// <summary>
        /// The string literal for a leave command.
        /// </summary>
        private const string LEAVE_COMMAND = "leave";

        /// <summary>
        /// Represents the participant.
        /// </summary>
        private DnsSdParticipant _participant = new DnsSdParticipant();

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

        public event EventHandler<HostJoinStatusMessageReceivedArgs> HostJoinStatusMessageReceived = delegate { };

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
                // Decode the message that the game host sent to us.
                string[] message = e.Message.ToString().Split('-');
                switch (message[0])
                {
                    case QUESTION_COMMAND: // The game host sent a question to us.
                        NewQuestionAvailable(this,
                            new QuestionEventArgs { Question = ParseQuestion(e.Message.ToString()) });
                        break;
                    case JOIN_COMMAND: // The game host sent a confirmation that we are connected.
                        this.HostJoinStatusMessageReceived(this,
                            new HostJoinStatusMessageReceivedArgs { IsJoined = Boolean.Parse(message[1]) });
                        break;
                }
            });
        }

        public async Task InitializeAsync() 
        {
            // Start listening for UDP advertisers.
            await this._participant.StartListeningAsync();

            // Start listening for TCP communications.
            await this._participantCommunicationChannel.StartListeningAsync();
        }

        public async Task JoinGameAsync(string playerName)
        {
            this._participant.ListenerMessage = playerName;

            await this._participant.ConnectToManagerAsync(_managerGuid);
        }

        public async Task LeaveGameAsync(string playerName)
        {
            await this._managerCommunicationChannel
                .SendRemoteMessageAsync($"{LEAVE_COMMAND}-{playerName}");
        }

        public async Task AnswerQuestionAsync(string playerName, int option)
        {
            await this._managerCommunicationChannel
                .SendRemoteMessageAsync($"{ANSWER_COMMAND}-{playerName}-{option}");
        }

        /// <summary>
        /// Parses the question string that was received from the game host.
        /// </summary>
        private static Question ParseQuestion(string question)
        {
            string[] message = question.Split('-');

            return new Question
            {
                Text = message[1],
                Options = new List<string>(message.Skip(4)),
                CorrectAnswerIndex = int.Parse(message[2])
            };
        }

    }
}
