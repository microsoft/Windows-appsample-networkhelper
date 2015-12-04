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
using System.Threading.Tasks;

namespace QuizGame.Model
{
    public class MockHostCommunicator : IHostCommunicator
    {
        internal MockClientCommunicator Client1 { get; set; }
        internal MockClientCommunicator Client2 { get; set; }

        private List<string> clientList = new List<string>();

        public async Task EnterLobbyAsync()
        {
            // Simulate a periodic broadcast. Loops until the ClientViewModel adds a handler to the GameAvailable event. 
            while (!this.Client1.OnGameAvailable() || !this.Client2.OnGameAvailable()) { await Task.Delay(100); }
        }

        public void LeaveLobby()
        {
            // No need to do anything in the mock version.
        }

        public Task SendQuestionAsync(Question question)
        {
            this.Client1.OnNewQuestionAvailable(question);
            this.Client2.OnNewQuestionAvailable(question);
            return Task.CompletedTask;
        }

        public event EventHandler<PlayerEventArgs> PlayerJoined = delegate { };

        internal void OnPlayerJoined(MockClientCommunicator client, string playerName)
        {
            if (!this.clientList.Contains(playerName))
            {
                this.clientList.Add(playerName);
                this.PlayerJoined(this, new PlayerEventArgs { PlayerName = playerName });
                client.OnHostJoinStatusMessageReceived(true);
            }
            else
            {
                client.OnHostJoinStatusMessageReceived(false);
            }
        }

        public event EventHandler<PlayerEventArgs> PlayerDeparted = delegate { };

        internal void OnPlayerDeparted(string playerName)
        {
            if (this.clientList.Remove(playerName))
            {
                this.PlayerDeparted(this, new PlayerEventArgs { PlayerName = playerName });
            }
        }

        public event EventHandler<AnswerReceivedEventArgs> AnswerReceived = delegate { };

        internal void OnAnswerReceived(string playerName, int answerIndex)
        {
            this.AnswerReceived(this, new AnswerReceivedEventArgs { 
                PlayerName = playerName, AnswerIndex = answerIndex });
        }

    }
}
