using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCaseSource(nameof(TryGetPositionsCases))]
        public bool TryGetPositions(Inventory inventory, Item item, Vector2Int[] expectedPositions)
        {
            bool success = inventory.TryGetPositions(item, out Vector2Int[] actualPositions);
            Assert.AreEqual(expectedPositions, actualPositions);
            return success;
        }

        private static IEnumerable<TestCaseData> TryGetPositionsCases()
        {
            var itemD = new Item("D", 1, 2);
            var itemX = new Item("X", 3, 2);

            var inventory = new Inventory(width: 5, height: 5,
                new KeyValuePair<Item, Vector2Int>(itemD, new Vector2Int(4, 0)),
                new KeyValuePair<Item, Vector2Int>(itemX, new Vector2Int(1, 2))
            );

            yield return new TestCaseData(inventory, itemD, new[]
            {
                new Vector2Int(4, 0),
                new Vector2Int(4, 1)
            }).Returns(true).SetName("Item D");

            yield return new TestCaseData(inventory, itemX, new[]
            {
                new Vector2Int(1, 2),
                new Vector2Int(1, 3),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(3, 2),
                new Vector2Int(3, 3)
            }).Returns(true).SetName("Item X");

            yield return new TestCaseData(inventory, null, null)
                .Returns(false).SetName("Null");

            yield return new TestCaseData(inventory, new Item("X", 1, 1), null)
                .Returns(false).SetName("Absent");
        }
    }
}