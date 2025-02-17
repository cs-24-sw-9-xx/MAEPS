// Copyright 2022 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using System;

using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Rosgraph;

using Unity.Robotics.ROSTCPConnector;

using UnityEngine;

namespace Maes.TransformToNav2
{
    internal class ROSClockPublisher : MonoBehaviour
    {
        [SerializeField]
        private Clock.ClockMode m_ClockMode;

        [SerializeField, HideInInspector]
        private Clock.ClockMode m_LastSetClockMode;

        [SerializeField]
        private readonly double m_PublishRateHz = 100f;
        private double m_LastPublishTimeSeconds;

        // Set in Start
        private ROSConnection m_ROS = null!;

        private readonly string m_ClockTopic = "/clock";

        private double PublishPeriodSeconds => 1.0f / m_PublishRateHz;

        private bool ShouldPublishMessage => Clock.FrameStartTimeInSeconds - PublishPeriodSeconds > m_LastPublishTimeSeconds;

        private void OnValidate()
        {
            var clocks = FindObjectsByType<ROSClockPublisher>(FindObjectsSortMode.None);
            if (clocks.Length > 1)
            {
                Debug.LogWarning("Found too many clock publishers in the scene, there should only be one!");
            }

            if (Application.isPlaying && m_LastSetClockMode != m_ClockMode)
            {
                Debug.LogWarning("Can't change ClockMode during simulation! Setting it back...");
                m_ClockMode = m_LastSetClockMode;
            }

            SetClockMode(m_ClockMode);
        }

        private void SetClockMode(Clock.ClockMode mode)
        {
            Clock.Mode = mode;
            m_LastSetClockMode = mode;
        }

        // Start is called before the first frame update
        private void Start()
        {
            SetClockMode(m_ClockMode);
            m_ROS = ROSConnection.GetOrCreateInstance();
            m_ROS.RegisterPublisher<ClockMsg>(m_ClockTopic);
        }

        private void PublishMessage()
        {
            var publishTime = Clock.time;
            var clockMsg = new TimeMsg
            {
                sec = (int)publishTime,
                nanosec = (uint)((publishTime - Math.Floor(publishTime)) * Clock.k_NanoSecondsInSeconds)
            };
            m_LastPublishTimeSeconds = publishTime;
            m_ROS.Publish(m_ClockTopic, clockMsg);
        }

        private void Update()
        {
            if (ShouldPublishMessage)
            {
                PublishMessage();
            }
        }
    }
}