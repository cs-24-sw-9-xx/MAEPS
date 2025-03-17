using System.Collections.Generic;

using Maes.Map;
using Maes.Map.Generators;
using Maes.Robot;

using NUnit.Framework;

using Unity.Mathematics;

using UnityEngine;


namespace Tests.EditModeTests
{
    [TestFixture]
    public class CommunicationManagerTests
    {
        
        private CommunicationManager _communicationManager;
        private RobotConstraints _robotConstraints;
        private SimulationMap<Tile> _simulationMap;

        private const string testMap = "" +
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

            _simulationMap = Utilities.GenerateSimulationMapFromString(testMap);
            _communicationManager = new CommunicationManager(_simulationMap, _robotConstraints, null);
        }
        
        [TestCase(0, 0, 0, 0, 100)]
        [TestCase(0, 0, 1, 0, 99)]
        [TestCase(1, 1, 2, 2, 85.85f)]
        public void CommunicationBetweenPointsTest(float startX, float startY, float endX, float endY, float expectedSignal)
        {
            var start = new Vector2(startX, startY);
            var end = new Vector2(endX, endY);

            var result = _communicationManager.CommunicationBetweenPoints(start, end, _simulationMap);

            Assert.AreEqual(expectedSignal, result, 0.01f);
        }
    }
}