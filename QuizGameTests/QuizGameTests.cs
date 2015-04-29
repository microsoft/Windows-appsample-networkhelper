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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using QuizGame.Model;
using System.Collections.Generic;

namespace QuizGameTests
{
    [TestClass]
    public class QuizGameTests
    {
        public IGame GetGame()
        {
            var questions = new List<Question>
            {
                new Question {
                    Text = "What's the first letter of the alphabet?",
                    Options = new List<string> { "a", "b", "c", "d" },
                    CorrectAnswerIndex = 0 },
                new Question {
                    Text = "What's the second letter of the alphabet?",
                    Options = new List<string> { "a", "b", "c", "d" },
                    CorrectAnswerIndex = 1 }
            };
            return new Game(questions);
        }

        [TestMethod]
        public void ScoresMatchCorrectAnswerCounts()
        {
            var game = GetGame();
            var player1 = "Player One";
            var player2 = "Player Two";
            game.AddPlayer(player1);
            game.AddPlayer(player2);
            game.StartGame();

            game.SubmitAnswer(player1, 0);
            game.SubmitAnswer(player2, 1);

            var score = game.GetResults();
            Assert.AreEqual(1, score[player1]);
            Assert.AreEqual(0, score[player2]);

            game.NextQuestion();
            game.SubmitAnswer(player1, 1);
            game.SubmitAnswer(player2, 1);
            game.NextQuestion();

            Assert.IsTrue(game.IsGameOver);
            score = game.GetResults();
            Assert.AreEqual(2, score[player1]);
            Assert.AreEqual(1, score[player2]);
        }

        [TestMethod]
        public void WinnerIsPlayerWithHighestScoreWhenGameIsOver()
        {
            var game = GetGame();
            var player1 = "Player One";
            var player2 = "Player Two";
            game.AddPlayer(player1);
            game.AddPlayer(player2);
            game.StartGame();

            game.SubmitAnswer(player1, 0);
            game.SubmitAnswer(player2, 1);

            Assert.IsFalse(game.IsGameOver);
            Assert.IsNull(game.Winner);
            game.NextQuestion();
            game.NextQuestion();

            var score = game.GetResults();
            Assert.AreEqual(1, score[player1]);
            Assert.AreEqual(0, score[player2]);
            Assert.IsTrue(game.IsGameOver);
            Assert.AreSame(player1, game.Winner);
        }

        [TestMethod]
        public void GameIsOverAfterLastQuestion()
        {
            var game = GetGame();
            game.StartGame();
            Assert.AreEqual(2, game.Questions.Count);
            Assert.IsFalse(game.IsGameOver);
            game.NextQuestion();
            Assert.IsFalse(game.IsGameOver);
            game.NextQuestion();
            Assert.IsTrue(game.IsGameOver);
        }

        [TestMethod]
        public void FailingToAnswerResultsInNoScore()
        {
            var game = GetGame();
            var player = "Player";
            game.AddPlayer(player);
            game.StartGame();

            game.SubmitAnswer(player, 0);
            Assert.AreEqual(1, game.GetResults()[player]);

            game.NextQuestion();
            Assert.IsFalse(game.IsGameOver);
            Assert.AreEqual(1, game.GetResults()[player]);

            game.NextQuestion();
            Assert.IsTrue(game.IsGameOver);
            Assert.AreEqual(1, game.GetResults()[player]);
        }

        [TestMethod]
        public void PlayerCannotPlayWithoutFirstJoiningGame()
        {
            var game = GetGame();
            game.StartGame();
            var player = "Player";
            Assert.ThrowsException<KeyNotFoundException>(() => game.SubmitAnswer(player, 0));

            game.AddPlayer(player);
            game.SubmitAnswer(player, 0);
            // No exception this time.
        }

        [TestMethod]
        public void PlayerCannotPlayAfterLeavingGame()
        {
            var game = GetGame();
            var player = "Player";
            game.AddPlayer(player);

            game.StartGame();
            game.SubmitAnswer(player, 0);
            game.NextQuestion();

            game.RemovePlayer(player);
            Assert.ThrowsException<KeyNotFoundException>(() => game.SubmitAnswer(player, 0));
        }

        [TestMethod]
        public void PlayerCannotSubmitAnswerWhenGameIsOver()
        {
            var game = GetGame();
            var player = "Player";
            game.AddPlayer(player);
            game.StartGame();
            Assert.IsFalse(game.IsGameOver);
            var answerSubmitted = game.SubmitAnswer(player, 0);
            Assert.IsTrue(answerSubmitted);
            game.NextQuestion();
            game.NextQuestion();
            Assert.IsTrue(game.IsGameOver);
            answerSubmitted = game.SubmitAnswer(player, 0);
            Assert.IsFalse(answerSubmitted);
        }
    }
}
