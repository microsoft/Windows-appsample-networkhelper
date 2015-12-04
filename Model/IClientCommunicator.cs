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
    public interface IClientCommunicator
    {
        Task InitializeAsync();
        
        // Adds the specified player to the current game.
        Task JoinGameAsync(string playerName);

        // Removes the specified player from the current game.
        Task LeaveGameAsync(string playerName);

        // Submits an answer to the current question.
        Task AnswerQuestionAsync(string playerName, int option);

        // Occurs when a game is available for joining. 
        event EventHandler GameAvailable;

        // Occurs when new question data has arrived.
        event EventHandler<QuestionEventArgs> NewQuestionAvailable;

        // Occurs when the server has received a join request and either acknowledges it, or denies it contingent on uniqueness of client name.
        event EventHandler<HostJoinStatusMessageReceivedArgs> HostJoinStatusMessageReceived;
    }
}
