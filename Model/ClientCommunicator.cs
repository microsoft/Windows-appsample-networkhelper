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

namespace QuizGame.Model
{
    /// <summary>
    /// Provides a game-oriented adapter to the P2PSessionClient class. 
    /// </summary>
    public sealed class ClientCommunicator : IClientCommunicator
    {
        public event EventHandler GameAvailable = delegate { };
        public event EventHandler<QuestionEventArgs> NewQuestionAvailable = delegate { };
        public event EventHandler<HostJoinStatusMessageReceivedArgs> HostJoinStatusMessageReceived = delegate { };

        private P2PSessionClient Client { get; set; }

        public ClientCommunicator(P2PSessionClient client)
        {
            this.Client = client;
            this.Client.HostAvailable += (s, e) => this.GameAvailable(this, EventArgs.Empty);
            this.Client.MessageReceived += ((s, e) =>
            {
                HostMessage hostMessage = e.DeserializedMessage<HostMessage>();

                switch(hostMessage.MessageType)
                {
                    case HostMessageType.JoinStatus:
                        this.HostJoinStatusMessageReceived(this, 
                            new HostJoinStatusMessageReceivedArgs { IsJoined = hostMessage.IsJoined });
                        break;
                    case HostMessageType.Question:
                        this.NewQuestionAvailable(this,
                            new QuestionEventArgs { Question = hostMessage.Question});
                        break;
                }
            });
        }

        public async void Initialize() 
        { 
            await this.Client.ListenForP2PSession(P2PSession.SessionType.LocalNetwork); 
        }

        public async void JoinGame(string playerName)
        {
            await Client.SendMessage(new HostCommandData { 
                PlayerName = playerName, Command = Command.Join }, typeof(HostCommandData));
        }

        public async void LeaveGame(string playerName)
        {
            await Client.SendMessage(new HostCommandData {
                PlayerName = playerName, Command = Command.Leave }, typeof(HostCommandData));
        }

        public async void AnswerQuestion(string playerName, int option)
        {
            await Client.SendMessage(new HostCommandData { 
                PlayerName = playerName, Command = Command.Answer, Data = option }, typeof(HostCommandData));
        }

    }
}
