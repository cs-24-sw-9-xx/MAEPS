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
// Contributors 2025: 
// Casper Nyvang Sørensen,
// Christian Ziegler Sejersen,
// Jakob Meyer Olsen
using System.Collections.Generic;

using Maes.Map.Generators;
using Maes.Robot;

using NUnit.Framework;

using Unity.Mathematics;


namespace Tests.EditModeTests
{
    [TestFixture]
    public class CommunicationManagerTests
    {
        private RobotConstraints _robotConstraints;

        [SetUp]
        public void SetUp()
        {
            _robotConstraints = new RobotConstraints(
                transmitPower: 100f,
                materialCommunication: true,
                attenuationDictionary: new Dictionary<uint, Dictionary<TileType, float>>
                {
                    { 2400U, new Dictionary<TileType, float>
                        {
                            { TileType.Wall, 10f },
                            { TileType.Room, 1f }
                        }
                    }
                },
                receiverSensitivity: 50f
            );


        }

        [TestCase("sXe;", 1)]
        [TestCase("" +
                  "s;" +
                  "X;" +
                  "e;", 1)]
        [TestCase("" +
                  "sX;" +
                  "Xe", 0)]
        [TestCase("" +
                  "sXX;" +
                  "XXX;" +
                  "XXe", math.SQRT2)]
        public void WallDistanceBetweenPointsTest(string stringMap, float distance)
        {
            var ((start, end), map) = Utilities.GenerateSimulationMapFromString(stringMap);
            var communicationManager = new CommunicationManager(map, _robotConstraints, null);

            var result = communicationManager.CommunicationBetweenPoints(start, end);

            Assert.AreEqual(distance, result.WallCellsDistance, 0.01f);
        }
    }
}