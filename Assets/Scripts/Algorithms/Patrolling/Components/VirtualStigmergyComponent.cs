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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using JetBrains.Annotations;

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
    // ReSharper disable once UnusedTypeParameter
    public sealed class VirtualStigmergyComponent<TKey, TValue, TMarker> : IComponent
        where TKey : notnull
    {
        public delegate void OnNewUpdateDelegate(TKey key, ValueInfo valueInfo);
        public delegate ValueInfo OnConflictDelegate(TKey key, ValueInfo localValueInfo, ValueInfo incomingValueInfo);

        private readonly Dictionary<TKey, ValueInfo> _localKnowledge = new();

        private readonly OnConflictDelegate _onConflictDelegate;
        private readonly OnNewUpdateDelegate _onNewUpdateDelegate;
        private readonly IRobotController _controller;

        private readonly bool _isStruct;

        /// <inheritdoc />
        public int PreUpdateOrder { get; } = -1000;

        /// <inheritdoc />
        public int PostUpdateOrder { get; } = -1000;

        /// <summary>
        /// Gets how many key-value pairs are in the local knowledge.
        /// </summary>
        public int Size => _localKnowledge.Count;

        /// <summary>
        /// Creates a new instance of <see cref="VirtualStigmergyComponent{TKey,TValue,TMarker}"/>.
        /// </summary>
        /// <param name="onConflictDelegate">A function to call to resolve conflicts.</param>
        /// <param name="controller">The robot controller.</param>
        public VirtualStigmergyComponent(OnConflictDelegate onConflictDelegate, IRobotController controller, OnNewUpdateDelegate? onNewUpdateDelegate = null)
        {
            var valueType = typeof(TValue);
            if (!valueType.IsValueType && valueType.GetInterface(nameof(ICloneable)) == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(TValue)} must be either a struct or implement the {nameof(ICloneable)} interface");
            }

            _isStruct = valueType.IsValueType;

            _onConflictDelegate = onConflictDelegate;
            _controller = controller;

            _onNewUpdateDelegate = onNewUpdateDelegate ?? ((key, value) => { });
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        public IEnumerable<ComponentWaitForCondition> PreUpdateLogic()
        {
            while (true)
            {
                foreach (var objectMessage in _controller.ReceiveBroadcast())
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
                    new ValueInfo(message.Timestamp, message.RobotId, CloneValue(message.Value)!)); // Clone the value from the message to ensure nothing is smuggled.

                // Update our local knowledge and create a put message.
                // I don't know if we should do this the paper is not clear on it.
                // But it makes no sense to do conflict resolution if we are not going to do something like this.
                _localKnowledge[message.Key] = newValueInfo;
                _onNewUpdateDelegate(message.Key, newValueInfo);
                BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(message.Key, newValueInfo.Value, newValueInfo.Timestamp, newValueInfo.RobotId));

                return;
            }

            // Message timestamp must be newer than ours here.
            _localKnowledge[message.Key] = new ValueInfo(message.Timestamp, message.RobotId, message.Value!);
            _onNewUpdateDelegate(message.Key, _localKnowledge[message.Key]);
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
            _onNewUpdateDelegate(message.Key, newValueInfo);
        }

        /// <summary>
        /// Add or update an entry in the stigmergy.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="value">The new value.</param>
        /// <remarks>This communicates this change with neighbors.</remarks>
        public void Put(TKey key, TValue value)
        {
            var timestamp = 1;
            if (_localKnowledge.TryGetValue(key, out var info))
            {
                timestamp = info.Timestamp + 1;
            }

            _localKnowledge[key] = new ValueInfo(timestamp, _controller.Id, value);
            BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(key, value, timestamp, _controller.Id));
        }

        /// <summary>
        /// The same as Get, but does not communicate with neighbors.
        /// </summary>
        /// <param name="key">The key to get.</param>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if the key is present.</returns>
        /// <remarks>You probably don't want to use this method. See <see cref="TryGet"/>.</remarks>
        public bool TryGetNonSending(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            var ret = _localKnowledge.TryGetValue(key, out var info);
            if (ret)
            {
                value = info.Value;
            }
            else
            {
                value = default;
            }

            return ret;
        }

        /// <summary>
        /// Gets a value in the stigmergy.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if the key is present.</returns>
        /// <remarks>This only returns what is in the local knowledge, but it sends a get message to receive the newest information from neighbors. This information is first available next tick, however.</remarks>
        public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            var ret = _localKnowledge.TryGetValue(key, out var valueInfo);

            if (ret)
            {
                // Ask neighbors if our information is up to date.
                // It won't be immediately available, so we won't be able to return it here.
                BroadcastMessage(VirtualStigmergyMessage.CreateGetMessage(key, valueInfo.Value, valueInfo.Timestamp,
                    valueInfo.RobotId));
                value = valueInfo.Value;
            }
            else
            {
                // Ask neighbors for this information.
                // It won't be immediately available, so we won't be able to return it here.
                // NOTE: It is unclear what the timestamp should be if we have nothing in our local knowledge.
                // So I will give it 0, it should always be smaller than any timestamp of any existing entries.
                BroadcastMessage(VirtualStigmergyMessage.CreateGetMessage(key, default, 0, _controller.Id));
                value = default;
            }

            return ret;
        }

        /// <summary>
        /// Checks if a key exists in the local knowledge
        /// </summary>
        /// <param name="key">The key to check existence for.</param>
        /// <returns><see langword="true"/> if the key exists otherwise <see langword="false"/></returns>
        /// <remarks>This does not communicate with neighbors.</remarks>
        public bool Has(TKey key)
        {
            // The paper is a little vague what Has does.
            // Should it ask the swarm like Get? or should it just check the local knowledge?
            // I have implemented it using the local knowledge just as Size.
            return _localKnowledge.ContainsKey(key);
        }

        /// <summary>
        /// This sends all information in the stigmergy to anybody in range.
        /// </summary>
        /// <remarks>This is pretty wasteful, only do this if you must.</remarks>
        public void SendAll()
        {
            foreach (var (key, valueInfo) in _localKnowledge)
            {
                BroadcastMessage(VirtualStigmergyMessage.CreatePutMessage(key, valueInfo.Value, valueInfo.Timestamp, valueInfo.RobotId));
            }
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
            _controller.Broadcast(message);
        }

        [ContractAnnotation("value:null => null; value:notnull=>notnull")]
        private TValue? CloneValue(TValue? value)
        {
            if (_isStruct)
            {
                return value;
            }

            return (TValue?)((ICloneable?)value)?.Clone();
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

            public TKey Key { get; }

            public TValue? Value { get; }

            public int Timestamp { get; }

            public int RobotId { get; }

            private VirtualStigmergyMessage(MessageType type, TKey key, TValue? value, int timestamp, int robotId)
            {
                Type = type;
                Key = key;
                Value = value;
                Timestamp = timestamp;
                RobotId = robotId;
            }

            public static VirtualStigmergyMessage CreateGetMessage(TKey key, TValue? value, int timestamp,
                int robotId)
            {
                return new VirtualStigmergyMessage(MessageType.Get, key, value, timestamp, robotId);
            }

            public static VirtualStigmergyMessage CreatePutMessage(TKey key, TValue value, int timestamp, int robotId)
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