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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace QuizGame.ViewModel
{
    public class PlayerViewModel : BindableBase
    {
        /// <summary>
        /// A static helper function to run code on the UI thread.
        /// </summary>
        private static Func<DispatchedHandler, Task> callOnUiThread = async (handler) 
            => await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);

        /// <summary>
        /// Represents the participant.
        /// </summary>
        private UdpParticipant _participant = new UdpParticipant();

        /// <summary>
        /// The communication channel to listen for messages from the game host.
        /// </summary>
        private ICommunicationChannel _participantCommunicationChannel;

        /// <summary>
        /// The communication channel at which messages are sent 
        /// </summary>
        private ICommunicationChannel _managerCommunicationChannel;

        private bool _isQuestionAnswered { get; set; }

        public PlayerViewModel()
        {
            // When a game host is found.
            _participant.ManagerFound += (async (sender, e) =>
            {
                var host = new GameHost() { Name = e.Message, Id = e.Id, CommChannel = _participant.CreateCommunicationChannel(e.Id) };
                await callOnUiThread(() => AvailableGames.Add(host));
            });

            _participantCommunicationChannel = new TcpCommunicationChannel();

            // When a new question is received from the game host.
            _participantCommunicationChannel.MessageReceived += (async (sender, e) =>
            {
                object message = new Question();
                e.GetDeserializedMessage(ref message);
                await callOnUiThread(() => CurrentQuestion = message as Question);
            });
        }

        /// <summary>
        /// The text that indicates the current state of the app displayed in the UI.
        /// </summary>
        public string StateName
        {
            get
            {
                return _stateName;
            }
            set
            {
                SetProperty(ref _stateName, value);
            }
        }
        private string _stateName;

        /// <summary>
        /// The name of the player.
        /// </summary>
        public string PlayerName
        {
            get
            {
                return _playerName ?? string.Empty;
            }
            set
            {
                if (SetProperty(ref _playerName, value))
                {
                    JoinGameCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private string _playerName;

        /// <summary>
        /// Indicates whether the player can join a game. Preconditions are that an available game has been  
        /// selected and the player has entered a name.
        /// </summary>
        public bool CanJoin
        {
            get
            {
                return _canJoin && PlayerName != null && PlayerName.Length != 0 && SelectedGame != null;
            }
            set
            {
                if (SetProperty(ref _canJoin, value))
                {
                    OnPropertyChanged(nameof(CanJoin));
                    JoinGameCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private bool _canJoin;

        /// <summary>
        /// Indicates whether a player has joined a game.
        /// </summary>
        public bool IsJoined
        {
            get
            {
                return _isJoined;
            }
            set
            {
                if (SetProperty(ref _isJoined, value))
                {
                    OnPropertyChanged(nameof(JoinVisibility));
                    OnPropertyChanged(nameof(GameUnderwayVisibility));
                    OnPropertyChanged(nameof(QuestionAvailableVisibility));
                }
                StateName = _isJoined ? "stand by..." : "lobby";
            }
        }
        private bool _isJoined;

        /// <summary>
        /// The current question that the game host has sent.
        /// </summary>
        public Question CurrentQuestion
        {
            get
            {
                return _currentQuestion;
            }
            set
            {
                if (IsJoined && SetProperty(ref _currentQuestion, value))
                {
                    OnPropertyChanged(nameof(QuestionAvailableVisibility));
                    StateName = _currentQuestion == null ? "lobby" : "make a selection...";
                    _isQuestionAnswered = false;
                    AnswerQuestionCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private Question _currentQuestion;

        /// <summary>
        /// The list of available game hosts.
        /// </summary>
        public ObservableCollection<GameHost> AvailableGames { get; } = new ObservableCollection<GameHost>();

        /// <summary>
        /// The game host that has been selected in the UI.
        /// </summary>
        public object SelectedGame
        {
            get
            {
                return _selectedGame;
            }
            set
            {
                if (SetProperty(ref _selectedGame, value))
                {
                    OnPropertyChanged(nameof(SelectedGame));
                    CanJoin = true;
                    JoinGameCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private object _selectedGame;

        /// <summary>
        /// The command associated with joining a game.
        /// </summary>
        public DelegateCommand JoinGameCommand
        {
            get
            {
                return _joinGameCommand ?? (_joinGameCommand = new DelegateCommand(
                    async () => await JoinGameAsync(PlayerName, ((GameHost)SelectedGame).Id),
                    () => CanJoin));
            }
        }
        private DelegateCommand _joinGameCommand;

        /// <summary>
        /// The command associated with leaving a game.
        /// </summary>
        public DelegateCommand LeaveGameCommand
        {
            get
            {
                return _leaveGameCommand ?? (_leaveGameCommand = new DelegateCommand(
                    async () =>
                    {
                        await LeaveGameAsync(PlayerName);
                        IsJoined = false;
                        CurrentQuestion = null;
                    }));
            }
        }
        private DelegateCommand _leaveGameCommand;

        /// <summary>
        /// The command associated with sending an answer to the game host.
        /// </summary>
        public DelegateCommand<string> AnswerQuestionCommand
        {
            get
            {
                return _answerQuestionCommand ?? (_answerQuestionCommand = new DelegateCommand<string>(
                    async option =>
                    {
                        await AnswerQuestionAsync(PlayerName, Int32.Parse(option));
                        _isQuestionAnswered = true;
                        AnswerQuestionCommand.RaiseCanExecuteChanged();
                        StateName = "thank you";
                    },
                    option => !_isQuestionAnswered));
            }
        }
        private DelegateCommand<string> _answerQuestionCommand;

        /// <summary>
        /// Indicates whether the join game screen is visible in the UI.
        /// </summary>
        public Visibility JoinVisibility => !IsJoined ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Indicates whether the game underway screen in visible in the UI.
        /// </summary>
        public Visibility GameUnderwayVisibility => IsJoined ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Indicates whether the question screen is visible in the UI.
        /// </summary>
        public Visibility QuestionAvailableVisibility => IsJoined && CurrentQuestion != null ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Start listening for available games and messages from a game host.
        /// </summary>
        public async Task StartListeningAsync()
        {
            // Start listening for UDP advertisers.
            await _participant.StartListeningAsync();

            // Start listening for TCP messages.
            await _participantCommunicationChannel.StartListeningAsync();
        }

        /// <summary>
        /// Stop listening for available games and messages from a game host.
        /// </summary>
        public async Task StopListeningAsync()
        {
            _participant.StopListening();
            await _participantCommunicationChannel.StopListening();
        }

        /// <summary>
        /// Helper method to join a host game.
        /// </summary>
        private async Task JoinGameAsync(string playerName, Guid host)
        {
            _participant.ListenerMessage = playerName;
            await _participant.ConnectToManagerAsync(host);
            _managerCommunicationChannel = _participant.CreateCommunicationChannel(host);

            // Alert the ViewModel that the player has joined the game successfully.
            await callOnUiThread(() =>
            {
                IsJoined = true;
            });
        }

        /// <summary>
        /// Helper method to leave a game by sending a leave message to the joined game host.
        /// </summary>
        private async Task LeaveGameAsync(string playerName)
        {
            PlayerMessage command = new PlayerMessage()
            {
                Command = PlayerMessageType.Leave,
                PlayerName = playerName
            };

            await _managerCommunicationChannel
                .SendRemoteMessageAsync(command);
        }

        /// <summary>
        /// Helper method to send an answer to the joined game host for the current question.
        /// </summary>
        private async Task AnswerQuestionAsync(string playerName, int option)
        {
            PlayerMessage command = new PlayerMessage()
            {
                Command = PlayerMessageType.Answer,
                PlayerName = playerName,
                QuestionAnswer = option
            };

            await _managerCommunicationChannel
                .SendRemoteMessageAsync(command);
        }
    }
}
