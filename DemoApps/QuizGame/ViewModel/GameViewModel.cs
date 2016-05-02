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

using NetworkHelper;
using QuizGame.Common;
using QuizGame.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace QuizGame.ViewModel
{
    public class PlayerProgress
    {
        public SolidColorBrush AnsweredCurrentQuestionBrush { get; internal set; }
        public FontWeight AnsweredCurrentQuestionFontWeight { get; internal set; }
        public string Name { get; internal set; }
    }

    public class PlayerResult
    {
        public string Name { get; internal set; }
        public Int32 Score { get; internal set; }
    }

    public enum GameState { PreGame, Lobby, GameUnderway, Results }

    /// <summary>
    /// This is the ViewModel for the HostPage, it ties all the UI logic with the game logic.
    /// </summary>
    public class GameViewModel : BindableBase
    {
        /// <summary>
        /// A static helper function to run code on the UI thread. This is used in callbacks from the NetworkHelper library.
        /// </summary>
        private static Func<DispatchedHandler, Task> callOnUiThread = async (handler) => await
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, handler);

        /// <summary>
        /// A dictionary of submitted answers from all the players.
        /// </summary>
        private Dictionary<string, Dictionary<Question, int?>> SubmittedAnswers { get; set; }

        /// <summary>
        /// The player names stored for the UI.
        /// </summary>
        public ObservableCollection<String> PlayerNames { get; set; }

        /// <summary>
        /// The list of questions that will be sent to players.
        /// </summary>
        private List<Question> Questions { get; set; } = new List<Question>()
            {
                new Question
                {
                    Text = "In which year was Microsoft founded?",
                    Options = new List<string> { "1971", "1973", "1975", "1977" },
                    CorrectAnswerIndex = 2
                },
                new Question
                {
                    Text = "What was the Microsoft slogan in 2005?",
                    Options = new List<string>
                    {
                        "A computer on every desktop.",
                        "Where do you want to go today?",
                        "Your Potential. Our Passion.",
                        "Be what's next."
                    },
                    CorrectAnswerIndex = 1
                },
                new Question
                {
                    Text = "Including Clippy, how many Office Assistants were in Office 97?",
                    Options = new List<string> { "5", "7", "9", "Wait...there were others?" },
                    CorrectAnswerIndex = 2
                },
                new Question
                {
                    Text = "The dog Rover, in what 1995 Microsoft product, could be considered a precursor to Cortana?",
                    Options = new List<string>
                    {
                        "Microsoft Encarta",
                        "Microsoft Bob",
                        "Microsoft Live One Care",
                        "Microsoft Live Mesh"
                    },
                    CorrectAnswerIndex = 1
                }
            };

        /// <summary>
        /// The position of the CurrentQuestion in the Questions list.
        /// </summary>
        private int currentQuestionIndex = -1;

        /// <summary>
        /// Maps the player names to their guids.
        /// </summary>
        private Dictionary<string, Guid> _playerToParticipantMap = new Dictionary<string, Guid>();

        /// <summary>
        /// The manager that sends UDP advertisement messages and manages a list of participants.
        /// </summary>
        private UdpManager _manager = new UdpManager();

        /// <summary>
        /// The list of communication channels to send messages to participants.
        /// </summary>
        private List<ICommunicationChannel> _participantCommunicationChannels = new List<ICommunicationChannel>();

        /// <summary>
        /// The communication channel to listen for messages from participants.
        /// </summary>
        private ICommunicationChannel _managerCommunicationChannel = new TcpCommunicationChannel();

        public GameViewModel()
        {
            // When a player connects to the host.
            _manager.ParticipantConnected += OnParticipantConnected;

            // When a message is received from a player.
            _managerCommunicationChannel.MessageReceived += OnMessageReceived;

            // Attach an event handler to the PropertyChanged event so that 
            // PlayerProgress and NextButtonText are updated correctly.
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals("SubmittedAnswers"))
                {
                    OnPropertyChanged(nameof(PlayerProgress));
                }
                if (e.PropertyName.Equals("CurrentQuestion") &&
                    Questions.Last() == CurrentQuestion)
                {
                    NextButtonText = "Show results";
                }
            };

            NextButtonText = "Next question";

            // Set up game information.
            PlayerNames = new ObservableCollection<string>();
            SubmittedAnswers = new Dictionary<string, Dictionary<Question, int?>>();
        }

        /// <summary>
        /// Command for creating a game. After this command is executed, 
        /// the host will enter the lobby screen and start broadcasting itself
        /// to listening players.
        /// </summary>
        public DelegateCommand CreateGameCommand
        {
            get
            {
                return _createGameCommand ?? (_createGameCommand = new DelegateCommand(
                    async () =>
                    {
                        GameState = GameState.Lobby;
                        await StartAdvertisingGameAsync(GameName);
                    }));
            }
        }
        private DelegateCommand _createGameCommand;

        /// <summary>
        /// Command for starting a game. After this command is executed,
        /// the host will send the first question to all the players.
        /// </summary>
        public DelegateCommand StartGameCommand
        {
            get
            {
                return _startGameCommand ?? (_startGameCommand = new DelegateCommand(
                    async () =>
                    {
                        // Start game.
                        currentQuestionIndex = 0;
                        await SendCurrentQuestion();

                        // On Question changed.
                        OnPropertyChanged(nameof(CurrentQuestion));
                        OnPropertyChanged(nameof(PlayerProgress));

                        GameState = GameState.GameUnderway;
                        StopAdvertisingGame();
                    },
                    () => GameState == GameState.Lobby));
            }
        }
        private DelegateCommand _startGameCommand;

        /// <summary>
        /// Command for either sending the next question to all players, or displaying the results
        /// if the last question was already sent.
        /// </summary>
        public DelegateCommand NextQuestionCommand
        {
            get
            {
                return _nextQuestionCommand ?? (_nextQuestionCommand = new DelegateCommand(
                    async () =>
                    {
                        currentQuestionIndex++;
                        await SendCurrentQuestion();

                        // Notify the UI that the current question has changed.
                        OnPropertyChanged(nameof(CurrentQuestion));
                        OnPropertyChanged(nameof(PlayerProgress));

                        // If the game is over, then change the game state.
                        if (currentQuestionIndex >= Questions.Count())
                        {
                            GameState = GameState.Results;
                        }
                    },
                    // Can execute if the game is underway and the current question is not null.
                    () => GameState == GameState.GameUnderway && CurrentQuestion != null));
            }
        }
        private DelegateCommand _nextQuestionCommand;

        /// <summary>
        /// Command for ending the game and going back to the lobby. When returning to the lobby,
        /// the host will start broadcasting itself to other players.
        /// </summary>
        public DelegateCommand EndGameCommand
        {
            get
            {
                return _endGameCommand ?? (_endGameCommand = new DelegateCommand(async () =>
                {
                    await StartAdvertisingGameAsync(GameName);
                    GameState = GameState.Lobby;
                    NextButtonText = "Next question";
                },
                // This command can only execute if the state of the game is showing the results.
                () => GameState == GameState.Results));
            }
        }
        private DelegateCommand _endGameCommand;

        /// <summary>
        /// The current state of the game.
        /// </summary>
        public GameState GameState
        {
            get
            {
                return _gameState;
            }

            set
            {
                if (SetProperty(ref _gameState, value))
                {
                    // Alert the UI that all the visiblity properties may have changed, because the game state changed.
                    OnPropertyChanged(nameof(PreGameVisibility));
                    OnPropertyChanged(nameof(StartVisibility));
                    OnPropertyChanged(nameof(GameUnderwayVisibility));
                    OnPropertyChanged(nameof(ResultsVisibility));
                    OnPropertyChanged(nameof(PlayerResults));

                    StartGameCommand.RaiseCanExecuteChanged();
                    NextQuestionCommand.RaiseCanExecuteChanged();
                    EndGameCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private GameState _gameState;

        /// <summary>
        /// The name of the game that players will see when selecting the game they want to join.
        /// </summary>
        public string GameName
        {
            get
            {
                return _gameName;
            }
            set
            {
                if (SetProperty(ref _gameName, value))
                {
                    OnPropertyChanged(nameof(GameName));
                    StartGameCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private string _gameName;

        /// <summary>
        /// The text for the "next" button. This allows text to change based on the current
        /// game state.
        /// </summary>
        public string NextButtonText
        {
            get
            {
                return _nextButtonText;
            }
            set
            {
                SetProperty(ref _nextButtonText, value);
            }
        }
        private string _nextButtonText;

        /// <summary>
        /// Gets the progress for each player.
        /// </summary>
        public List<object> PlayerProgress
        {
            get
            {
                var players = SubmittedAnswers.AsEnumerable().Select(kvp => new PlayerProgress
                {
                    Name = kvp.Key,
                    AnsweredCurrentQuestionFontWeight =
                        CurrentQuestion != null &&
                        kvp.Value.ContainsKey(CurrentQuestion) &&
                        kvp.Value[CurrentQuestion].HasValue ?
                            FontWeights.ExtraBold : FontWeights.Normal,
                    AnsweredCurrentQuestionBrush =
                        CurrentQuestion != null &&
                        kvp.Value.ContainsKey(CurrentQuestion) &&
                        kvp.Value[CurrentQuestion].HasValue ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.LightGray)
                });
                return players.ToList<object>();
            }
        }

        /// <summary>
        /// Gets the results for each player.
        /// </summary>
        public List<object> PlayerResults
        {
            get
            {
                var correctAnswers = Questions.Select(question => question.CorrectAnswerIndex);
                var results =
                    from playerResults in SubmittedAnswers.AsEnumerable()
                    let score = playerResults.Value.AsEnumerable()
                        .Select(kvp => kvp.Value)
                        .Zip(correctAnswers, (playerAnswer, actualAnswer) =>
                            playerAnswer.HasValue && playerAnswer.Value == actualAnswer)
                        .Count(isCorrect => isCorrect)
                    select new { PlayerName = playerResults.Key, Score = score };

                return results.ToDictionary(result => result.PlayerName, result => result.Score).Select(
                    kvp => new PlayerResult { Name = kvp.Key, Score = kvp.Value }).ToList<object>();
            }
        }

        /// <summary>
        /// Gets the current question that players see.
        /// </summary>
        public Question CurrentQuestion => currentQuestionIndex > -1 &&
            currentQuestionIndex < Questions.Count ? Questions[currentQuestionIndex] : null;

        /// <summary>
        /// Used to determine if the PreGame screen is visible or not.
        /// </summary>
        public Visibility PreGameVisibility => 
            GameState == GameState.PreGame ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Used to determine if the Lobby screen is visible or not.
        /// </summary>
        public Visibility StartVisibility => 
            GameState == GameState.Lobby ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Used to determine if the Questions screen is visible or not.
        /// </summary>
        public Visibility GameUnderwayVisibility => 
            GameState == GameState.GameUnderway ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Used to determine if the scores screen is visible or not.
        /// </summary>
        public Visibility ResultsVisibility => 
            GameState == GameState.Results ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Stops advertising and listening for answer
        /// </summary>
        public async Task StopListeningAsync()
        {
            _manager.StopAdvertising();
            await _managerCommunicationChannel.StopListening();
        }

        /// <summary>
        /// Updates the UI when a player has left.
        /// </summary>
        /// <param name="playerName"></param>
        private void OnPlayerLeft(string playerName)
        {
            _manager.RemoveParticipant(_playerToParticipantMap[playerName]);
            _playerToParticipantMap.Remove(playerName);

            if (PlayerNames.Remove(playerName))
            {
                SubmittedAnswers.Remove(playerName);
                OnPropertyChanged(nameof(PlayerNames));
                OnPropertyChanged(nameof(SubmittedAnswers));
            }
        }

        /// <summary>
        /// Updates the UI when a player has joined.
        /// </summary>
        /// 
        private void OnPlayerJoined(PlayerMessage message, Guid guid)
        {
            Guid duplicatePlayerID;

            if (!_playerToParticipantMap.TryGetValue(message.PlayerName, out duplicatePlayerID))
            {
                _playerToParticipantMap.Add(message.PlayerName, guid);

                if (PlayerNames.Contains(message.PlayerName))
                {
                    message.PlayerName += ".";
                }

                PlayerNames.Add(message.PlayerName);

                SubmittedAnswers.Add(message.PlayerName,
                    new Dictionary<Question, int?>(Questions.Count));

                OnPropertyChanged(nameof(PlayerNames));
                OnPropertyChanged(nameof(SubmittedAnswers));
            }
        }

        /// <summary>
        /// When a message is received from a player.
        /// </summary>
        private async void OnMessageReceived(object sender, IMessageReceivedEventArgs e)
        {
            // Deserialize the message
            object data = new PlayerMessage();
            e.GetDeserializedMessage(ref data);
            var message = data as PlayerMessage;

            switch (message.Command)
            {
                case PlayerMessageType.Answer:
                    await callOnUiThread(() => OnAnswerReceived(message.PlayerName, message.QuestionAnswer));
                    break;
                case PlayerMessageType.Leave:
                    await callOnUiThread(() => OnPlayerLeft(message.PlayerName));
                    break;
            }
        }

        /// <summary>
        /// When a player has been connected to the host.
        /// </summary>
        private async void OnParticipantConnected(object sender, ParticipantConnectedEventArgs e)
        {
            // A player has joined the game.
            _participantCommunicationChannels.Add(_manager.CreateCommunicationChannel(e.Id));

            PlayerMessage data = new PlayerMessage
            {
                PlayerName = e.Message.ToString(),
                QuestionAnswer = 0,
                Command = PlayerMessageType.Join
            };

            await callOnUiThread(() => OnPlayerJoined(data, e.Id));
        }

        /// <summary>
        /// Updates the UI when an answer has been received from a player.
        /// </summary>
        private bool OnAnswerReceived(string playerName, int answerIndex)
        {
            if (playerName != null && CurrentQuestion != null)
            {
                SubmittedAnswers[playerName][CurrentQuestion] = answerIndex;
                OnPropertyChanged(nameof(SubmittedAnswers));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sends the current question to all players that have joined.
        /// </summary>
        /// <returns></returns>
        private async Task SendCurrentQuestion()
        {
            // Do this even if currentQuestionIndex < Questions.Count, because the client needs to know
            // when the current question goes to null so it can update its UI state.
            var clientList = new List<Guid>();
            clientList.AddRange(_playerToParticipantMap.Values);

            var messageTasks = clientList.Select(client =>
                _manager.CreateCommunicationChannel(client).SendRemoteMessageAsync(CurrentQuestion ?? new Question()));

            await Task.WhenAll(messageTasks);

            OnPropertyChanged(nameof(CurrentQuestion));
        }

        /// <summary>
        /// Begins broadcasting the game to listening players.
        /// </summary>
        private async Task StartAdvertisingGameAsync(string name)
        {
            _manager.AdvertiserMessage = name;
            await _manager.StartAdvertisingAsync();
            await _managerCommunicationChannel.StartListeningAsync();
        }

        /// <summary>
        /// Stops broadcasting the game to listening players.
        /// </summary>
        private void StopAdvertisingGame() => _manager.StopAdvertising();
    }
}
