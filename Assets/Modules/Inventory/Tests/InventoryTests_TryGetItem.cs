using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCase(-1, -1)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(3, 0)]
        [TestCase(0, 3)]
        [TestCase(3, 3)]
        public void TryGetItem_AtPositionOutOfRange_ReturnsFalse(int x, int y)
        {
            //Arrange:
            var inventory = new Inventory(3, 3);

            //Act:
            bool success = inventory.TryGetItem(x, y, out Item actualItem);

            //Assert:
            Assert.IsFalse(success);
            Assert.IsNull(actualItem);
        }
        
        [TestCaseSource(nameof(TryGetItemCases))]
        public bool TryGetItem(Inventory inventory, Vector2Int position, Item expectedItem)
        {
            bool success = inventory.TryGetItem(position, out Item actualItem);
            Assert.AreEqual(expectedItem, actualItem);
            return success;
        }

        private static IEnumerable<TestCaseData> TryGetItemCases()
        {
            var item1 = new Item("D", 1, 2);
            var item2 = new Item("X", 3, 2);

            var inventory = new Inventory(width: 5, height: 5,
                new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(1, 2)),
                new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(4, 0))
            );

            yield return new TestCaseData(inventory, new Vector2Int(1, 2), item2)
                .Returns(true).SetName("(1, 2)");

            yield return new TestCaseData(inventory, new Vector2Int(1, 3), item2)
                .Returns(true).SetName("(1, 3)");

            yield return new TestCaseData(inventory, new Vector2Int(3, 3), item2)
                .Returns(true).SetName("(3, 3)");

            yield return new TestCaseData(inventory, new Vector2Int(4, 1), item1)
                .Returns(true).SetName("(4, 1)");

            yield return new TestCaseData(inventory, new Vector2Int(0, 0), null)
                .Returns(false).SetName("Null");
        }
    }
}