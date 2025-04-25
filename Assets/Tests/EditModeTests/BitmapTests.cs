using System.Linq;

using Maes.Utilities;

using NUnit.Framework;

using UnityEngine;

namespace Tests.EditModeTests
{
    public class BitmapTests
    {
        [Test]
        public void VisibilityBitmapContainsTests()
        {
            using var bitmap = new Bitmap(3, 3);
            bitmap.Set(1, 1);

            Assert.AreEqual(1, bitmap.Count);

            Assert.IsTrue(bitmap.Contains(1, 1));
            Assert.IsFalse(bitmap.Contains(2, 1));
            Assert.IsFalse(bitmap.Contains(1, 2));
            Assert.IsFalse(bitmap.Contains(2, 2));

            using var bitmap2 = new Bitmap(3, 3);
            bitmap2.Set(2, 2);
            Assert.IsTrue(bitmap2.Contains(2, 2));
        }

        [Test]
        public void VisibilityBitmapIntersectionTests()
        {
            using var bitmap1 = new Bitmap(3, 3);
            bitmap1.Set(2, 2);

            using var bitmap2 = new Bitmap(4, 4);
            bitmap2.Set(2, 2);

            using var intersectedBitmap = Bitmap.Intersection(bitmap1, bitmap2);
            Assert.IsTrue(intersectedBitmap.Contains(2, 2));
        }

        [Test]
        public void VisibilityBitmapExceptWithTests()
        {
            using var bitmap1 = new Bitmap(3, 3);
            for (var x = 1; x < 3; x++)
            {
                for (var y = 1; y < 3; y++)
                {
                    bitmap1.Set(x, y);
                }
            }

            using var bitmap2 = new Bitmap(4, 4);
            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    bitmap2.Set(x, y);
                }
            }

            bitmap2.ExceptWith(bitmap1);

            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    if (x > 0 && x < 3 && y > 0 && y < 3)
                    {
                        Assert.IsFalse(bitmap2.Contains(x, y));
                    }
                    else
                    {
                        Assert.IsTrue(bitmap2.Contains(x, y));
                    }
                }
            }
        }

        [Test]
        public void VisibilityBitmapEnumeratorTests()
        {
            using (var bitmap = new Bitmap(3, 3))
            {
                bitmap.Set(1, 2);

                using var enumerator = bitmap.GetEnumerator();
                Assert.IsTrue(enumerator.MoveNext());

                Assert.AreEqual(new Vector2Int(1, 2), enumerator.Current);

                var tile = bitmap.Single();
                Assert.AreEqual(new Vector2Int(1, 2), tile);
            }
        }
    }
}