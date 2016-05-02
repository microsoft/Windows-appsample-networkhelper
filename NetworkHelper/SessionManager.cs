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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkHelper
{
    public abstract class SessionManager : ISessionManager
    {
        public Dictionary<Guid, object> Participants { get; set; } = new Dictionary<Guid, object>();

        public event EventHandler<ParticipantConnectedEventArgs> ParticipantConnected = delegate { };

        public abstract Task<bool> StartAdvertisingAsync();

        public abstract bool StopAdvertising();

        public abstract ICommunicationChannel CreateCommunicationChannel(Guid participant, int flags);

        public bool RemoveParticipant(Guid subscriber) => Participants.Remove(subscriber);

        /// <summary>
        /// Adds a participant.
        /// </summary>
        protected void AddParticipant(object participant, string participantMessage)
	    {                
            // Add the participant if it isn't already in the list of Participants.
		    if (!Participants.Values.Contains(participant))
            {
                // Generate a new GUID so that app developers can reference the participant.
                var guid = Guid.NewGuid();
                Participants.Add(guid, participant);

                // Notify that ParticipantConnected event handlers so app developers are aware when a new participant has been connected.
                ParticipantConnected(this, new ParticipantConnectedEventArgs { Id = guid, Message = participantMessage});
            }
	    }

    }
}
