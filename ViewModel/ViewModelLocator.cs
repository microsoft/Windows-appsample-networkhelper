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
using QuizGame.Model;
using System.Collections.Generic;
using Windows.ApplicationModel;

namespace QuizGame.ViewModel
{
    public static class ViewModelLocator
    {
        private static IClientCommunicator clientCommunicator;
#if LOCALTESTMODEON
        private static IClientCommunicator clientCommunicator2;
#endif
        private static IHostCommunicator hostCommunicator;

        static ViewModelLocator()
        {
#if LOCALTESTMODEON
            hostCommunicator = new MockHostCommunicator();
            var mockHostCommunicator = hostCommunicator as MockHostCommunicator;
            clientCommunicator = new MockClientCommunicator { Host = mockHostCommunicator };
            clientCommunicator2 = new MockClientCommunicator { Host = mockHostCommunicator };
            mockHostCommunicator.Client1 = clientCommunicator as MockClientCommunicator;
            mockHostCommunicator.Client2 = clientCommunicator2 as MockClientCommunicator;
#else
            clientCommunicator = new ClientCommunicator();
            hostCommunicator = new HostCommunicator();
#endif
        }

        public static ClientViewModel ClientViewModel
        {
            get
            {
                return clientViewModel ?? (clientViewModel = 
                    new ClientViewModel(clientCommunicator) { IsJoined = DesignMode.DesignModeEnabled });
            }
        }
        private static ClientViewModel clientViewModel;

#if LOCALTESTMODEON
        public static ClientViewModel ClientViewModel2
        {
            get
            {
                return clientViewModel2 ?? (clientViewModel2 = clientViewModel2 =
                    new ClientViewModel(clientCommunicator2) { IsJoined = DesignMode.DesignModeEnabled });
            }
        }
        private static ClientViewModel clientViewModel2;
#endif

        public static HostViewModel HostViewModel
        {
            get
            {
#if LOCALTESTMODEON
                var game = GetSampleGame();
#else
                var game = DesignMode.DesignModeEnabled ? GetSampleGame() : GetSampleGame();
#endif
                return hostViewModel ?? (hostViewModel = new HostViewModel(game, hostCommunicator)
                {
                    GameState = DesignMode.DesignModeEnabled ? GameState.GameUnderway : GameState.Lobby
                });
            }
        }
        private static HostViewModel hostViewModel;

        private static Game GetSampleGame()
        {
            var questions = new List<Question>
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
            return new Game(questions);
        }
    }
}
