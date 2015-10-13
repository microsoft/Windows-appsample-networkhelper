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

using QuizGame.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuizGame.Model
{
    public enum GameState { Lobby, GameUnderway, Results }

    public sealed class Game : BindableBase, QuizGame.Model.IGame
    {
        public event EventHandler<QuestionEventArgs> NewQuestionAvailable = delegate { };
        private int currentQuestionIndex = -1;

        public Game(List<Question> questions)
        {
            if (questions == null) throw new ArgumentNullException("questions");
            this.Questions = questions;
            this.PlayerNames = new ObservableCollection<string>();
            this.SubmittedAnswers = new Dictionary<string, Dictionary<Question, int?>>();
        }

        public void AddPlayer(string playerName)
        {
            if (this.PlayerNames.Contains(playerName)) playerName += ".";
            this.PlayerNames.Add(playerName);
            this.SubmittedAnswers.Add(playerName, 
                new Dictionary<Question, int?>(this.Questions.Count));
            this.OnPropertyChanged(nameof(PlayerNames));
            this.OnPropertyChanged(nameof(SubmittedAnswers));
        }

        public void RemovePlayer(string playerName)
        {
            if (this.PlayerNames.Remove(playerName))
            {
                this.SubmittedAnswers.Remove(playerName);
                this.OnPropertyChanged(nameof(PlayerNames));
                this.OnPropertyChanged(nameof(SubmittedAnswers));
            }
        }

        public bool SubmitAnswer(string playerName, int answerIndex)
        {
            if (playerName == null || this.CurrentQuestion == null) return false;
            this.SubmittedAnswers[playerName][this.CurrentQuestion] = answerIndex;
            this.OnPropertyChanged(nameof(SubmittedAnswers));
            return true;
        }

        public void StartGame()
        {
            this.currentQuestionIndex = 0;
            this.SendCurrentQuestion();
        }

        public void NextQuestion() 
        { 
            this.currentQuestionIndex++;
            this.SendCurrentQuestion();
        }

        private void SendCurrentQuestion()
        {
            // Do this even if this.currentQuestionIndex < this.Questions.Count, because the client needs to know
            // when the current question goes to null so it can update its UI state.
            this.NewQuestionAvailable(this, new QuestionEventArgs { Question = this.CurrentQuestion });

            this.OnPropertyChanged(nameof(CurrentQuestion));
            this.OnPropertyChanged(nameof(IsGameOver));
            this.OnPropertyChanged(nameof(Winner));
        }

        public Dictionary<string, int> GetResults()
        {
            var correctAnswers = this.Questions.Select(question => question.CorrectAnswerIndex);
            var results =
                from playerResults in SubmittedAnswers.AsEnumerable()
                let score = playerResults.Value.AsEnumerable()
                    .Select(kvp => kvp.Value)
                    .Zip(correctAnswers, (playerAnswer, actualAnswer) =>
                        playerAnswer.HasValue && playerAnswer.Value == actualAnswer)
                    .Count(isCorrect => isCorrect)
                select new { PlayerName = playerResults.Key, Score = score };
            return results.ToDictionary(result => result.PlayerName, result => result.Score);
        }

        public GameState GameState
        {
            get { return this.gameState; }
            set { SetProperty(ref this.gameState, value); }
        }
        private GameState gameState;

        public ObservableCollection<String> PlayerNames { get; set; }
        public List<Question> Questions { get; private set; }
        public Question CurrentQuestion 
        { 
            get 
            { 
                return this.currentQuestionIndex > -1 && 
                    this.currentQuestionIndex < this.Questions.Count ? 
                    this.Questions[currentQuestionIndex] : null; 
            } 
        }

        public Dictionary<string, Dictionary<Question, int?>> SubmittedAnswers { get; private set; }
        public bool IsGameOver { get { return this.currentQuestionIndex >= this.Questions.Count; } }
        public string Winner { get { return this.IsGameOver ? 
            this.GetResults().Aggregate((a, b) => a.Value > b.Value ? a : b).Key : null; } }

    }
}
