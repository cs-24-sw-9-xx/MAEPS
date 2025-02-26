using System.Collections.Generic;

using UnityEngine;

namespace PlayModeTests {
    public static class ScenarioBuilderUtilities{
        public static List<Vector2Int> GenerateRandomSpawningPositions(System.Random random, int mapSize, int robotCount)
        {
            var spawningPosList = new List<Vector2Int>();
            for (var amountOfSpawns = 0; amountOfSpawns < robotCount; amountOfSpawns++)
            {
                spawningPosList.Add(new Vector2Int(random.Next(0, mapSize), random.Next(0, mapSize)));
            }
            return spawningPosList;
        }
    }
}