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

namespace NetworkHelper
{
    public interface ISessionManager
    {
        /// <summary>
        /// An event that indicates that a participant has been connected.
        /// </summary>
        event EventHandler<ParticipantConnectedEventArgs> ParticipantConnected;

        /// <summary>
        /// Start advertising. 
        /// </summary>
        Task<bool> StartAdvertisingAsync();

        /// <summary>
        /// Stop advertising.
        /// </summary>
        bool StopAdvertising();

        /// <summary>
        /// Creates an ICommunicationChannel object and returns it so that app developers can send custom messages to the participant.
        /// </summary>
        ICommunicationChannel CreateCommunicationChannel(Guid participant, int flags = 0);

        /// <summary>
        /// Removes a participant from a participants list.
        /// </summary>
        bool RemoveParticipant(Guid participant);
    }

    public class ParticipantConnectedEventArgs : EventArgs
    {
        public Guid Id { get; set; }

        public object Message { get; set; }
    }
}
    