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

namespace QuizGame.Model
{
    public class MockClientCommunicator : IClientCommunicator
    {
        internal MockHostCommunicator Host { get; set; }

        public async Task JoinGameAsync(string playerName)
        {
            this.Host.OnPlayerJoined(this, playerName);
            await Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            // No need to do anything in the mock version.
            await Task.CompletedTask;
        }
        
        public async Task LeaveGameAsync(string playerName)
        {
            this.Host.OnPlayerDeparted(playerName);
            await Task.CompletedTask;
        }

        public async Task AnswerQuestionAsync(string playerName, int answerIndex)
        {
            this.Host.OnAnswerReceived(playerName, answerIndex);
            await Task.CompletedTask;
        }

        public event EventHandler GameAvailable;

        internal bool OnGameAvailable()
        {
            var gameAvailable = this.GameAvailable;
            if (gameAvailable != null)
            {
                gameAvailable(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public event EventHandler<QuestionEventArgs> NewQuestionAvailable = delegate { };
        public event EventHandler<PlayerJoinedEventArgs> PlayerJoined;

        internal void OnNewQuestionAvailable(Question newQuestion)
        {
            this.NewQuestionAvailable(this, new QuestionEventArgs { Question = newQuestion });
        }

        internal void OnHostJoinStatusMessageReceived(bool isJoined)
        {
            this.PlayerJoined(this, new PlayerJoinedEventArgs { IsJoined = isJoined });
        }
    }
}
