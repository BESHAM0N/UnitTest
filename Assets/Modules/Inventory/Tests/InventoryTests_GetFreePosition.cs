using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCaseSource(nameof(GetFreePositionCases))]
        public bool GetFreePosition(Inventory inventory, Vector2Int size, Vector2Int expectedPosition)
        {
            //Act:
            bool result = inventory.FindFreePosition(size, out Vector2Int actualPosition);

            //Assert:
            Assert.AreEqual(expectedPosition, actualPosition);
            return result;
        }

        private static IEnumerable<TestCaseData> GetFreePositionCases()
        {
            yield return new TestCaseData(
                new Inventory(5, 5),
                new Vector2Int(2, 2),
                new Vector2Int(0, 0)
            ).Returns(true).SetName("Inventory is empty");

            yield return new TestCaseData(
                new Inventory(5, 5),
                new Vector2Int(5, 5),
                new Vector2Int(0, 0)
            ).Returns(true).SetName("Full item");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 1, 1), new Vector2Int(1, 1))
                ),
                new Vector2Int(3, 3),
                new Vector2Int(2, 0)
            ).Returns(true).SetName("Right Ordering");

            yield return new TestCaseData(
                new Inventory(5, 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 3, 3), new Vector2Int(1, 1))
                ),
                new Vector2Int(2, 2),
                new Vector2Int()
            ).Returns(false).SetName("Middle Item");

            yield return new TestCaseData(
                new Inventory(5, 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 3, 5), new Vector2Int(0, 0)),
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 3), new Vector2Int(3, 0))
                ),
                new Vector2Int(2, 2),
                new Vector2Int(3, 3)
            ).Returns(true).SetName("Top Right Corner Space");


            yield return new TestCaseData(
                new Inventory(5, 5),
                new Vector2Int(6, 6),
                new Vector2Int(0, 0)
            ).Returns(false).SetName("Item size is bigger than inventory");
        }
        
        [TestCase(0, 0)]
        [TestCase(-1, 10)]
        [TestCase(10, -2)]
        [TestCase(-2, -2)]
        [TestCase(0, 10)]
        [TestCase(5, 0)]
        public void GetFreePosition_InvalidSize_ThrowsArgumentException(int width, int height)
        {
            //Arrange
            var inventory = new Inventory(5, 5);

            //Assert
            Assert.Throws<ArgumentException>(() =>
                inventory.FindFreePosition(new Vector2Int(width, height), out _));
        }
    }
}