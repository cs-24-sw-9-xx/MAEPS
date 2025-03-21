// Copyright 2025 MAEPS
// 
// This file is part of MAEPS
// 
// MAEPS is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAEPS is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAEPS. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Mads Beyer Mogensen

// Uncomment this to enable debug log tracing of messages.
// #define VIRTUAL_STIGMERGY_TRACING

using System.Collections.Generic;
using System.Text;

using Maes.Robot;

#if VIRTUAL_STIGMERGY_TRACING
using UnityEngine;
#endif

namespace Maes.Algorithms.Patrolling.Components
{
    // TODO: Add ConflictLost functionality
    /// <summary>
    /// A key-value-pair based storage system, where changes are communicated throughout the swarm.
    /// Will eventually get consistent.
    /// </summary>
    /// <remarks>
    /// Implements the paper: A Tuple Space for Data Sharing in Robot Swarms
    /// DOI 10.4108/eai.3-12-2015.2262503
    /// </remarks>
    public sealed class VirtualStigmergyComponent<TValue> : IComponent
        where TValue : class
    {
        public delegate ValueInfo OnConflictDelegate(string key, ValueInfo localValueInfo, ValueInfo incomingValueInfo);

        private readonly Dictionary<string, ValueInfo> _localKnowledge = new();

        private readonly OnConflictDelegate _onConflictDelegate;
        private readonly CommunicationManager _communicationManager;
        private readonly MonaRobot _monaRobot;

        /// <inheritdoc />
        public int PreUpdateOrder { get; } = -1000;

        /// <inheritdoc />
        public int PostUpdateOrder { get; } = -1000;

        /// <summary>
        /// Gets a queue of messages that are not from virtual stigmergy.
        /// </summary>
        public Queue<object> NonVirtualStigmergyMessageQueue { get; } = new Queue<object>();

        /// <summary>
        /// Gets how many key-value pairs are in the local knowledge.
        /// </summary>
        public int Size => _localKnowledge.Count;

        /// <summary>
        /// Creates a new instance of <see cref="VirtualStigmergyComponent{TValue}"/>.
        /// </summary>
        /// <param name="onConflictDelegate">A function to call to resolve conflicts.</param>
        /// <param name="controller">The robot controller.</param>
        /// <param name="monaRobot">The mona robot.</param>
        public VirtualStigmergyComponent(OnConflictDelegate onConflictDelegate, Robot2DController controller, MonaRobot monaRobot)
        {
            _onConflictDelegate = onConflictDelegate;
            _communicationManager = controller.CommunicationManager;
            _monaRobot = monaRobot;
        }

        /// <inheritdoc />
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                foreach (var objectMessage in _communicationManager.ReadMessages(_monaRobot))
                {
                    if (objectMessage is VirtualStigmergyMessage message)
                    {
                        switch (message.Type)
                        {
                            case MessageType.Get:
                                HandleGetMessage(message);
                                break;
                            case MessageType.Put:
                                HandlePutMessage(message);
                                break;
                        }
                    }
                    else
                    {
                        NonVirtualStigmergyMessageQueue.Enqueue(objectMessage);
                    }

                }

                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        private void HandleGetMessage(VirtualStigmergyMessage message)
        {
#if VIRTUAL_STIGMERGY_TRACING
            Debug.LogFormat("STIGMERGY Robot {0} got GET message: key: {1}, value: {2}, timestamp: {3}, robotId: {4}", _monaRobot.id, message.Key, message.Value, message.Timestamp, message.RobotId);
#endif

            if (!_localKnowledge.TryGetValue(message.Key, out var valueInfo))
            {
                // We don't have the information locally.
                // Therefore, we won't send a put message.
                return;
            }

            if (message.Timestamp < valueInfo.Timestamp)
            {
                // The neighbor has old information, lets give it new information.
                BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(message.Key, valueInfo.Value, valueInfo.Timestamp, valueInfo.RobotId));
                return;
            }

            if (message.Timestamp == valueInfo.Timestamp)
            {
                if (message.RobotId == valueInfo.RobotId)
                {
                    // Do nothing
                    return;
                }

                // We need to do conflict resolution
                var newValueInfo = _onConflictDelegate(message.Key, valueInfo,
                    new ValueInfo(message.Timestamp, message.RobotId, message.Value!));

                // Update our local knowledge and create a put message.
                // I don't know if we should do this the paper is not clear on it.
                // But it makes no sense to do conflict resolution if we are not going to do something like this.
                _localKnowledge[message.Key] = newValueInfo;
                BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(message.Key, newValueInfo.Value, newValueInfo.Timestamp, newValueInfo.RobotId));

                return;
            }

            // Message timestamp must be newer than ours here.
            _localKnowledge[message.Key] = new ValueInfo(message.Timestamp, message.RobotId, message.Value!);
            BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(message.Key, message.Value!, message.Timestamp, message.RobotId));
        }

        private void HandlePutMessage(VirtualStigmergyMessage message)
        {
#if VIRTUAL_STIGMERGY_TRACING
            Debug.LogFormat("STIGMERGY Robot {0} got PUT message: key: {1}, value: {2}, timestamp: {3}, robotId: {4}", _monaRobot.id, message.Key, message.Value, message.Timestamp, message.RobotId);
#endif

            var localTimestamp = 0;

            if (_localKnowledge.TryGetValue(message.Key, out var value))
            {
                localTimestamp = value.Timestamp;
            }

            if (localTimestamp > message.Timestamp)
            {
                return;
            }

            var newValueInfo = new ValueInfo(message.Timestamp, message.RobotId, message.Value!);

            // Do conflict resolution
            if (localTimestamp == message.Timestamp)
            {
                // Only do conflict resolution if we don't already have that one.
                if (value.RobotId == newValueInfo.RobotId)
                {
                    return;
                }

                var localValueInfo = _localKnowledge[message.Key];

                newValueInfo = _onConflictDelegate(message.Key, localValueInfo, newValueInfo);

                // Send the conflict resolute message
                BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(message.Key, newValueInfo.Value, newValueInfo.Timestamp, newValueInfo.RobotId));
            }
            else
            {
                // Resend message to our lovely neighbors
                BroadcastMessage(message);
            }

            _localKnowledge[message.Key] = newValueInfo;
        }

        public IEnumerable<ComponentWaitForCondition> PostUpdateLogic()
        {
            while (true)
            {
                yield return ComponentWaitForCondition.WaitForLogicTicks(1, shouldContinue: true);
            }
        }

        /// <summary>
        /// Add or update an entry in the stigmergy.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="value">The new value.</param>
        /// <remarks>This communicates this change with neighbors.</remarks>
        public void Put(string key, TValue value)
        {
            var timestamp = 1;
            if (_localKnowledge.TryGetValue(key, out var info))
            {
                timestamp = info.Timestamp + 1;
            }

            _localKnowledge[key] = new ValueInfo(timestamp, _monaRobot.id, value);
            BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(key, value, timestamp, _monaRobot.id));
        }

        /// <summary>
        /// The same as Get, but does not communicate with neighbors.
        /// </summary>
        /// <param name="key">The key to get.</param>
        /// <returns>The value or null if key is not present.</returns>
        /// <remarks>You probably don't want to use this method. See <see cref="Get"/>.</remarks>
        public TValue? GetNonSending(string key)
        {
            if (_localKnowledge.TryGetValue(key, out var valueInfo))
            {
                return valueInfo.Value;
            }

            return null;

        }

        /// <summary>
        /// Gets a value in the stigmergy.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value or null if key is not present.</returns>
        /// <remarks>This only returns what is in the local knowledge, but it sends a get message to receive the newest information from neighbors. This information is first available next tick, however.</remarks>
        public TValue? Get(string key)
        {
            if (!_localKnowledge.TryGetValue(key, out var valueInfo))
            {
                // Ask neighbors for this information.
                // It won't be immediately available, so we won't be able to return it here.
                // NOTE: It is unclear what the timestamp should be if we have nothing in our local knowledge.
                // So I will give it 0, it should always be smaller than any timestamp of any existing entries.
                BroadcastMessage(VirtualStigmergyMessage.CreateGetMessage(key, null, 0, _monaRobot.id));

                return null;
            }

            // Ask neighbors if our information is up to date.
            // It won't be immediately available, so we won't be able to return it here.
            BroadcastMessage(VirtualStigmergyMessage.CreateGetMessage(key, valueInfo.Value, valueInfo.Timestamp, valueInfo.RobotId));

            return valueInfo.Value;
        }

        /// <summary>
        /// Checks if a key exists in the local knowledge
        /// </summary>
        /// <param name="key">The key to check existance for.</param>
        /// <returns><see langword="true"/> if the key exists otherwise <see langword="false"/></returns>
        /// <remarks>This does not communicate with neighbors.</remarks>
        public bool Has(string key)
        {
            // The paper is a little vague what Has does.
            // Should it ask the swarm like Get? or should it just check the local knowledge?
            // I have implemented it using the local knowledge just as Size.
            return _localKnowledge.ContainsKey(key);
        }

        public void DebugInfo(StringBuilder stringBuilder)
        {
            if (_localKnowledge.Count > 0)
            {
                stringBuilder.Append("Local Knowledge:\n");
                foreach (var (key, valueInfo) in _localKnowledge)
                {
                    stringBuilder.AppendFormat("  \"{0}\": ({1}, {2}, \"{3}\"),\n", key, valueInfo.Timestamp, valueInfo.RobotId, valueInfo.Value);
                }
            }
        }

        private void BroadcastMessage(VirtualStigmergyMessage message)
        {
#if VIRTUAL_STIGMERGY_TRACING
            Debug.LogFormat("STIGMERGY Robot {0} sending {1} message: key: {2}, value: {3}, timestamp: {4}, robotId: {5}", _monaRobot.id, message.Type, message.Key, message.Value, message.Timestamp, message.RobotId);
#endif
            _communicationManager.BroadcastMessage(_monaRobot, message);
        }

        public readonly struct ValueInfo
        {
            public readonly int Timestamp;

            public readonly int RobotId;

            public readonly TValue Value;

            public ValueInfo(int timestamp, int robotId, TValue value)
            {
                Timestamp = timestamp;
                RobotId = robotId;
                Value = value;
            }
        }

        private sealed class VirtualStigmergyMessage
        {
            public MessageType Type { get; }

            public string Key { get; }

            public TValue? Value { get; }

            public int Timestamp { get; }

            public int RobotId { get; }

            private VirtualStigmergyMessage(MessageType type, string key, TValue? value, int timestamp, int robotId)
            {
                Type = type;
                Key = key;
                Value = value;
                Timestamp = timestamp;
                RobotId = robotId;
            }

            public static VirtualStigmergyMessage CreateGetMessage(string key, TValue? value, int timestamp,
                int robotId)
            {
                return new VirtualStigmergyMessage(MessageType.Get, key, value, timestamp, robotId);
            }

            public static VirtualStigmergyMessage CreatePutMessage(string key, TValue value, int timestamp, int robotId)
            {
                return new VirtualStigmergyMessage(MessageType.Put, key, value, timestamp, robotId);
            }
        }

        private enum MessageType
        {
            Get,
            Put,
        }
    }
}