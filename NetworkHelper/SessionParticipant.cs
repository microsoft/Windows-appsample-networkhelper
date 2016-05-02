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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetworkHelper
{
    public abstract class SessionParticipant : ISessionParticipant
    {
        /// <summary>
        /// The managers that are available.
        /// </summary>
        public ConcurrentDictionary<Guid, object> Managers { get; set; } = new ConcurrentDictionary<Guid, object>();

        public event EventHandler<ManagerFoundEventArgs> ManagerFound = delegate { };

        public abstract Task<bool> StartListeningAsync();

        public abstract bool StopListening();

        public abstract Task ConnectToManagerAsync(Guid manager);

        public abstract ICommunicationChannel CreateCommunicationChannel(Guid manager, int flags);

        public bool RemoveManager(Guid manager)
        {
            object obj;
            return Managers.TryRemove(manager, out obj);
        }

        /// <summary>
        /// Adds a manager to the AvailableManagers list.
        /// </summary>
        protected void AddManager(object manager, string managerMessage)
        {
            // Add the manager to the list of Managers if it's not already in the list.
            if (!Managers.Values.Contains(manager))
            {
                // Generate a new GUID, so that app developers can reference this particular manager.
                var guid = Guid.NewGuid();
                bool isAdded = Managers.TryAdd(guid, manager);

                if (isAdded)
                {
                    // Notify that ManagerFound handlers so the app developers are aware of a new Manager.
                    ManagerFound(this, new ManagerFoundEventArgs { Id = guid, Message = managerMessage });
                }
            }
        }
    }
}
