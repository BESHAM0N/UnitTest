using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCaseSource(nameof(GetPositionsCases))]
        public Vector2Int[] GetPositions(Inventory inventory, Item item) => inventory.GetPositions(item);

        private static IEnumerable<TestCaseData> GetPositionsCases()
        {
            var itemD = new Item("D", 1, 2);
            var itemX = new Item("X", 3, 2);

            var inventory = new Inventory(width: 5, height: 5,
                new KeyValuePair<Item, Vector2Int>(itemD, new Vector2Int(4, 0)),
                new KeyValuePair<Item, Vector2Int>(itemX, new Vector2Int(1, 2))
            );

            yield return new TestCaseData(inventory, itemD).Returns(new[]
            {
                new Vector2Int(4, 0),
                new Vector2Int(4, 1)
            }).SetName("Item D");

            yield return new TestCaseData(inventory, itemX).Returns(new[]
            {
                new Vector2Int(1, 2),
                new Vector2Int(1, 3),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 2),
                new Vector2Int(3, 3)
            }).SetName("Item X");
        }
        
        [Test]
        public void GetPositions_ItemIsNull_ThrowsNullReferenceException()
        {
            var inventory = new Inventory(3, 3);
            Assert.Throws<NullReferenceException>(() => inventory.GetPositions(null));
        }
        
        [Test]
        public void GetPositions_ItemNotPresent_ThrowsKeyNotFoundException()
        {
            var inventory = new Inventory(3, 3);
            var item = new Item("X", 1, 1);

            Assert.Throws<KeyNotFoundException>(() => inventory.GetPositions(item));
        }
    }
}