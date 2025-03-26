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

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;

using NUnit.Framework;

using UnityEngine;


namespace Tests.EditModeTests
{
    [TestFixture]
    public class CommunicationManagerTests
    {
        private CommunicationManager _communicationManager;
        private RobotConstraints _robotConstraints;
        private SimulationMap<Tile> _simulationMap;

        private const string TestMap = "" +
                                    "            ;" +
                                    " XXXXXXXXXX ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " X        X ;" +
                                    " XXXXXXXXXX ;" +
                                    "            ";

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

            _simulationMap = Utilities.GenerateSimulationMapFromString(TestMap);
            _communicationManager = new CommunicationManager(_simulationMap, _robotConstraints, null!);
        }

        [TestCase(0, 0, 0, 0, 100)]
        [TestCase(0, 0, 1, 0, 99)]
        [TestCase(1, 1, 2, 2, 85.85f)]
        public void CommunicationBetweenPointsTest(float startX, float startY, float endX, float endY, float expectedSignal)
        {
            var start = new Vector2(startX, startY);
            var end = new Vector2(endX, endY);

            var result = _communicationManager.CommunicationSignalStrength(start, end, _simulationMap);

            Assert.AreEqual(expectedSignal, result, 0.01f);
        }
    }
}