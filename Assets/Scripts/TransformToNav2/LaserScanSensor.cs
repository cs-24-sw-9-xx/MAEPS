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

using System.Collections.Generic;

using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

using Unity.Robotics.ROSTCPConnector;

using UnityEngine;
using UnityEngine.Serialization;

namespace Maes.TransformToNav2
{
    internal class LaserScanSensor : MonoBehaviour
    {
        public string ScanTopic = "/scan";
        [FormerlySerializedAs("TimeBetweenScansSeconds")]
        public double PublishPeriodSeconds = 0.1;
        public float RangeMetersMin;
        public float RangeMetersMax = 1000f;
        public float ScanAngleStartDegrees;
        public float ScanAngleEndDegrees = -359;
        // Change the scan start and end by this amount after every publish
        public float ScanOffsetAfterPublish;
        public int NumMeasurementsPerScan = 180;
        public float TimeBetweenMeasurementsSeconds;
        public string FrameId = "base_scan";

        private float m_CurrentScanAngleStart;
        private float m_CurrentScanAngleEnd;

        // Set by Start
        private ROSConnection m_Ros = null!;
        private double m_TimeNextScanSeconds = -1;
        private int m_NumMeasurementsTaken;
        private readonly List<float> m_ranges = new();

        private bool m_isScanning;
        private double m_TimeLastScanBeganSeconds = -1;

        [SerializeField]
        public GameObject m_WrapperObject = null!;

        private void Awake()
        {
            // This module should not be enabled (i.e. run start) until explicitly told so
            // This allows for setting parameters before start runs.
            enabled = false;
        }

        private void Start()
        {
            ScanTopic = "/" + m_WrapperObject.name + ScanTopic; // Prepend robot name as namespace
            m_Ros = ROSConnection.GetOrCreateInstance();
            m_Ros.RegisterPublisher<LaserScanMsg>(ScanTopic);

            m_CurrentScanAngleStart = ScanAngleStartDegrees;
            m_CurrentScanAngleEnd = ScanAngleEndDegrees;

            m_TimeNextScanSeconds = Clock.Now + PublishPeriodSeconds;
        }

        private void BeginScan()
        {
            m_isScanning = true;
            m_TimeLastScanBeganSeconds = Clock.Now;
            m_TimeNextScanSeconds = m_TimeLastScanBeganSeconds + PublishPeriodSeconds;
            m_NumMeasurementsTaken = 0;
        }

        public void EndScan()
        {
            if (m_ranges.Count == 0)
            {
                Debug.LogWarning($"Took {m_NumMeasurementsTaken} measurements but found no valid ranges");
            }
            else if (m_ranges.Count != m_NumMeasurementsTaken || m_ranges.Count != NumMeasurementsPerScan)
            {
                Debug.LogWarning($"Expected {NumMeasurementsPerScan} measurements. Actually took {m_NumMeasurementsTaken}" +
                                 $"and recorded {m_ranges.Count} ranges.");
            }

            var timestamp = new TimeStamp(Clock.time);
            // Invert the angle ranges when going from Unity to ROS
            var angleStartRos = -m_CurrentScanAngleStart * Mathf.Deg2Rad;
            var angleEndRos = -m_CurrentScanAngleEnd * Mathf.Deg2Rad;
            if (angleStartRos > angleEndRos)
            {
                Debug.LogWarning("LaserScan was performed in a clockwise direction but ROS expects a counter-clockwise scan, flipping the ranges...");
                (angleEndRos, angleStartRos) = (angleStartRos, angleEndRos);
                m_ranges.Reverse();
            }

            var msg = new LaserScanMsg
            {
                header = new HeaderMsg
                {
                    frame_id = FrameId,
                    stamp = new TimeMsg
                    {
                        sec = timestamp.Seconds,
                        nanosec = timestamp.NanoSeconds,
                    }
                },
                range_min = RangeMetersMin,
                range_max = RangeMetersMax,
                angle_min = angleStartRos,
                angle_max = angleEndRos,
                angle_increment = (angleEndRos - angleStartRos) / NumMeasurementsPerScan,
                time_increment = TimeBetweenMeasurementsSeconds,
                scan_time = (float)PublishPeriodSeconds,
                intensities = new float[m_ranges.Count],
                ranges = m_ranges.ToArray(),
            };

            m_Ros.Publish(ScanTopic, msg);

            m_NumMeasurementsTaken = 0;
            m_ranges.Clear();
            m_isScanning = false;
            var now = (float)Clock.time;
            if (now > m_TimeNextScanSeconds)
            {
                Debug.LogWarning($"Failed to complete scan started at {m_TimeLastScanBeganSeconds:F} before next scan was " +
                                 $"scheduled to start: {m_TimeNextScanSeconds:F}, rescheduling to now ({now:F})");
                m_TimeNextScanSeconds = now;
            }

            m_CurrentScanAngleStart += ScanOffsetAfterPublish;
            m_CurrentScanAngleEnd += ScanOffsetAfterPublish;
            if (m_CurrentScanAngleStart > 360f || m_CurrentScanAngleEnd > 360f)
            {
                m_CurrentScanAngleStart -= 360f;
                m_CurrentScanAngleEnd -= 360f;
            }
        }

        public void Update()
        {
            if (!m_isScanning)
            {
                if (Clock.NowTimeInSeconds < m_TimeNextScanSeconds)
                {
                    return;
                }

                BeginScan();
            }


            var measurementsSoFar = TimeBetweenMeasurementsSeconds == 0 ? NumMeasurementsPerScan :
                1 + Mathf.FloorToInt((float)(Clock.time - m_TimeLastScanBeganSeconds) / TimeBetweenMeasurementsSeconds);
            if (measurementsSoFar > NumMeasurementsPerScan)
            {
                measurementsSoFar = NumMeasurementsPerScan;
            }

            var yawBaseDegrees = -transform.eulerAngles.z; //transform.eulerAngles.z;
            //Debug.Log($"Trace base angle: {transform.eulerAngles.z + 90}");
            while (m_NumMeasurementsTaken < measurementsSoFar)
            {
                var t = m_NumMeasurementsTaken / (float)NumMeasurementsPerScan;
                var yawSensorDegrees = Mathf.Lerp(m_CurrentScanAngleStart, m_CurrentScanAngleEnd, t);
                var yawDegrees = yawBaseDegrees + yawSensorDegrees;
                var directionVector = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
                directionVector = new Vector3(directionVector.x, directionVector.z, directionVector.y);
                var measurementStart = RangeMetersMin * directionVector + transform.position + Vector3.back * 0.1f;
                var hit = Physics2D.Raycast(measurementStart, directionVector, RangeMetersMax, layerMask: 1 << 3);
                // Only record measurement if it's within the sensor's operating range
                if (hit)
                {
                    // Visualise raytracing by uncommenting this part
                    // Debug.DrawRay(measurementStart, directionVector * hit.distance, Color.red, 1, true);
                    m_ranges.Add(hit.distance);
                }
                else
                {
                    m_ranges.Add(float.MaxValue);
                }

                // Even if Raycast didn't find a valid hit, we still count it as a measurement
                ++m_NumMeasurementsTaken;
            }

            if (m_NumMeasurementsTaken >= NumMeasurementsPerScan)
            {
                if (m_NumMeasurementsTaken > NumMeasurementsPerScan)
                {
                    Debug.LogError($"LaserScan has {m_NumMeasurementsTaken} measurements but we expected {NumMeasurementsPerScan}");
                }
                EndScan();
            }

        }
    }
}