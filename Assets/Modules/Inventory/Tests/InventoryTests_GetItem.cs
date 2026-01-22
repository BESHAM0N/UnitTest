using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [Test]
        public void GetItem_ItemNotPresent_ReturnsNull()
        {
            //Arrange:
            var inventory = new Inventory(3, 3);

            // Act
            Item item = inventory.GetItem(0, 0);
            
            //Assert:
            Assert.IsNull(item);
        }
        
        [TestCase(-1, -1)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(3, 0)]
        [TestCase(0, 3)]
        [TestCase(3, 3)]
        public void GetItem_PositionOutOfRange_ThrowsIndexOutOfRangeException(int x, int y)
        {
            //Arrange:
            var inventory = new Inventory(3, 3);

            //Assert:
            Assert.Catch<IndexOutOfRangeException>(() => inventory.GetItem(x, y));
        }
        
        [TestCaseSource(nameof(GetItemCases))]
        public Item GetItem(Inventory inventory, Vector2Int position) => 
            inventory.GetItem(position);

        private static IEnumerable<TestCaseData> GetItemCases()
        {
            var item1 = new Item("D", 1, 2);
            var item2 = new Item("X", 3, 2);

            var inventory = new Inventory(width: 5, height: 5,
                new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(1, 2)),
                new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(4, 0))
            );

            yield return new TestCaseData(inventory, new Vector2Int(1, 2))
                .Returns(item2).SetName("(1, 2)");

            yield return new TestCaseData(inventory, new Vector2Int(1, 3))
                .Returns(item2).SetName("(1, 3)");

            yield return new TestCaseData(inventory, new Vector2Int(3, 3))
                .Returns(item2).SetName("(3, 3)");

            yield return new TestCaseData(inventory, new Vector2Int(4, 1))
                .Returns(item1).SetName("(4, 1)");
        }
    }
}