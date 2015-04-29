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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace QuizGame.Model
{
    public interface IGame : INotifyPropertyChanged
    {
        event EventHandler<QuestionEventArgs> NewQuestionAvailable;
        void AddPlayer(string playerName);
        Question CurrentQuestion { get; }
        GameState GameState { get; set; }
        Dictionary<string, int> GetResults();
        bool IsGameOver { get; }
        void NextQuestion();
        ObservableCollection<string> PlayerNames { get; set; }
        List<Question> Questions { get; }
        void RemovePlayer(string playerName);
        void StartGame();
        bool SubmitAnswer(string playerName, int answerIndex);
        Dictionary<string, Dictionary<Question, int?>> SubmittedAnswers { get; }
        string Winner { get; }
    }

    public class QuestionEventArgs : EventArgs { public Question Question { get; set; } }

    public class HostJoinStatusMessageReceivedArgs : EventArgs { public bool IsJoined { get; set; } }
}
