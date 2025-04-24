using Maes.Utilities;

using NUnit.Framework;

using Tests.EditModeTests.Utilities.MapInterpreter;

namespace Tests.EditModeTests
{
    public class ExtensionUtilsTests
    {
        [Test]
        public void CellIndexToTriangleIndexes_TwoTileMap_ReturnsCorrectTriangleIndexes()
        {
            // Arrange
            var map = new SimulationMapBuilder(" X").BuildMap();
            // Act
            var triangleIndexes = ExtensionUtils.CellIndexToTriangleIndexes(map.map);

            // Assert
            Assert.IsNotNull(triangleIndexes);
            Assert.AreEqual(2, triangleIndexes.Count);
            Assert.AreEqual(8, triangleIndexes[0].Count);
            var cellOneTriangleIndexes = triangleIndexes[0];
            Assert.AreEqual(8, cellOneTriangleIndexes.Count);
            Assert.True(cellOneTriangleIndexes.Contains(0));
            Assert.True(cellOneTriangleIndexes.Contains(7));
            Assert.False(cellOneTriangleIndexes.Contains(8));

            var cellTwoTriangleIndexes = triangleIndexes[1];
            Assert.AreEqual(8, cellTwoTriangleIndexes.Count);
            Assert.True(cellTwoTriangleIndexes.Contains(8));
            Assert.True(cellTwoTriangleIndexes.Contains(15));
            Assert.False(cellTwoTriangleIndexes.Contains(7));
            Assert.False(cellTwoTriangleIndexes.Contains(16));
        }
    }
}