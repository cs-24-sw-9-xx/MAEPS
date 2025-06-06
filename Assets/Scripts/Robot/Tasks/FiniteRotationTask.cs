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

using UnityEngine;

namespace Maes.Robot.Tasks
{
    // Represents a task to rotate the robot by a given amount of degrees
    internal sealed class FiniteRotationTask : ITask
    {
        private readonly float _degreesToRotate;
        private readonly float _startingAngle;

        private readonly Transform _robotTransform;
        private float _previousRotation;

        public bool IsCompleted { get; private set; }

        public FiniteRotationTask(Transform robotTransform, float degreesToRotate)
        {
            if (degreesToRotate < -180f || degreesToRotate > 180f)
            {
                throw new ArgumentException(
                    $"Given rotation must be between -180° and 180° but target rotation was {degreesToRotate}");
            }

            _degreesToRotate = degreesToRotate;
            _robotTransform = robotTransform;
            _startingAngle = robotTransform.rotation.eulerAngles.z;
        }

        public MovementDirective GetNextDirective()
        {
            if (IsCompleted)
            {
                return MovementDirective.NoMovement();
            }

            // Find the current amount of rotation since starting the task
            var absRotation = GetAbsoluteDegreesRotated();

            // Find the speed of the rotation during the previous tick
            var currentRotationRate = absRotation - _previousRotation;
            _previousRotation = absRotation;

            // Calculate how much more we need to rotate before reaching the goal
            var remainingRotation = Math.Abs(_degreesToRotate) - absRotation;

            // Calculate how often we need 
            var stopTimeTicks = GetStopTime(currentRotationRate);
            var degreesRotatedBeforeStop = GetDegreesRotated(currentRotationRate, stopTimeTicks);

            // Calculate how far the robot is from reaching the target rotation if we stop applying force now
            var targetDelta = remainingRotation - degreesRotatedBeforeStop;

            var forceMultiplier = 1f;
            if (targetDelta < 2.28f)
            {
                // We need to apply some amount of force to rotate the last amount
                // It has been observed that if we apply maximum force (1.0) we will rotate an additional 2.28 degrees 
                // To find the appropriate amount of force, use linear interpolation on the target delta. 
                forceMultiplier = (1f / 2.28f) * targetDelta * 0.85f; // 0.85 is a magic number, sorry
            }

            if (targetDelta < 0.1f)
            {
                // The robot will be within acceptable range when rotation has stopped by itself.
                // Stop applying force and consider task completed
                forceMultiplier = 0;
                IsCompleted = true;
            }

            // Determine rotation direction
            if (_degreesToRotate > 0)
            {
                forceMultiplier *= -1;
            }

            return new MovementDirective(forceMultiplier, -forceMultiplier);
        }


        // Returns the time (in ticks from now) at which the velocity of the robot will be approximately 0 (<0.001) 
        private static int GetStopTime(float currentRotationRate)
        {
            return (int)(-3.81f * Mathf.Log(0.01f / currentRotationRate));
        }

        // Returns the degrees rotated over the given ticks when starting at the given rotation rate
        private static float GetDegreesRotated(float currentRotationRate, int ticks)
        {
            // Get offset by solving for C in:
            // 0 = -3.81*v0*e^(-t/3.81)+C
            //var offset = (float) (3.81 * currentRotationRate * Math.Pow(Math.E, -(1f / 3.81f) * 0));
            //return (float) (-3.81 * currentRotationRate * Math.Pow(Math.E, -(1f / 3.81f) * ticks) + offset) - currentRotationRate;

            var rotation = 0f;
            for (var i = 0; i < ticks; i++)
            {
                rotation += GetRotationRate(currentRotationRate, i + 1);
            }

            return rotation;
        }

        // Returns the speed of rotation (measured in degrees per tick) after waiting the given amount of ticks without
        // applying force
        private static float GetRotationRate(float startingRate, int ticks)
        {
            return startingRate * Mathf.Pow((float)Math.E, -ticks / 3.81f);
        }


        // Returns the amount of degrees that has been rotated since starting this task
        private float GetAbsoluteDegreesRotated()
        {
            return Math.Abs(Mathf.DeltaAngle(_robotTransform.rotation.eulerAngles.z, _startingAngle));
        }
    }
}