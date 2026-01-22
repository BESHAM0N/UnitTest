using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCase(5, 10)]
        [TestCase(3, 2)]
        [TestCase(1, 7)]
        [TestCase(100, 1)]
        public void Constructor_ValidDimensions_DoesNotThrow(int width, int height)
        {
            Assert.DoesNotThrow(() => _ = new Inventory(width, height));
        }

        [Test]
        public void Constructor_ValidDimensions_InitializesDimensionsCorrectly()
        {
            // Arrange
            const int width = 3;
            const int height = 4;

            //Act:
            var inventory = new Inventory(width, height);

            //Assert:
            Assert.AreEqual(width, inventory.Width);
            Assert.AreEqual(height, inventory.Height);
        }

        [Test]
        public void Constructor_ValidDimensions_InitializesCountToZero()
        {
            // Arrange
            const int width = 3;
            const int height = 4;

            //Act:
            var inventory = new Inventory(width, height);

            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void Constructor_ValidDimensions_InitializesAllSlotsAsFree()
        {
            // Arrange
            const int width = 3;
            const int height = 4;

            //Act:
            var inventory = new Inventory(width, height);

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Assert.IsTrue(inventory.IsFree(x, y));
        }

        [TestCase(-1, 10)]
        [TestCase(2, -1)]
        [TestCase(-10, -100)]
        [TestCase(0, 0)]
        [TestCase(0, 10)]
        [TestCase(10, 0)]
        public void Constructor_InvalidDimensions_ThrowsArgumentException(int width, int height)
        {
            //Assert:
            Assert.Throws<ArgumentException>(() =>
                _ = new Inventory(width, height));
        }

        [Test]
        public void Constructor_NullEnumerableKVPairs_ThrowArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new Inventory(5, 3, (IEnumerable<KeyValuePair<Item, Vector2Int>>) null));
        }

        [Test]
        public void Constructor_NullKeyValuePairItems_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new Inventory(5, 3, (KeyValuePair<Item, Vector2Int>[]) null));
        }

        [Test]
        public void Constructor_NullEnumerableItems_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new Inventory(5, 3, (IEnumerable<Item>) null));
        }

        [Test]
        public void Constructor_NullItemArray_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new Inventory(5, 3, (Item[]) null));
        }
        
        [TestCaseSource(nameof(Constructor_WithItems_Cases))]
        public void Constructor_WithItems_InitializesItems(
            int width,
            int height,
            KeyValuePair<Item, Vector2Int>[] items)
        {
            // Act
            var inventory = new Inventory(width, height, items);
            
            // Assert
            CollectionAssert.AreEquivalent(items.Select(x => x.Key), inventory);
        }

        private static IEnumerable<TestCaseData> Constructor_WithItems_Cases()
        {
            yield return new TestCaseData(10, 10, new[]
            {
                new KeyValuePair<Item, Vector2Int>(new Item("A", 10, 10), new Vector2Int(0, 0)),
            }).SetName("SingleFullSizeItem");

            yield return new TestCaseData(4, 4, new[]
            {
                new KeyValuePair<Item, Vector2Int>(new Item("A", 1, 2), new Vector2Int(0, 0)),
                new KeyValuePair<Item, Vector2Int>(new Item("B", 1, 1), new Vector2Int(0, 3)),
                new KeyValuePair<Item, Vector2Int>(new Item("C", 3, 2), new Vector2Int(1, 2)),
                new KeyValuePair<Item, Vector2Int>(new Item("D", 2, 1), new Vector2Int(2, 0)),
            }).SetName("PartiallyFilled");

            yield return new TestCaseData(4, 4, new[]
            {
                new KeyValuePair<Item, Vector2Int>(new Item("A", 2, 2), new Vector2Int(0, 0)),
                new KeyValuePair<Item, Vector2Int>(new Item("B", 2, 2), new Vector2Int(2, 0)),
                new KeyValuePair<Item, Vector2Int>(new Item("C", 2, 2), new Vector2Int(0, 2)),
                new KeyValuePair<Item, Vector2Int>(new Item("D", 2, 2), new Vector2Int(2, 2)),
            }).SetName("FullyFilledDense");
        }
    }
}