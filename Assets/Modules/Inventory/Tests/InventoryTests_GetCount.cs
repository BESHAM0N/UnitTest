using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCaseSource(nameof(GetCountCases))]
        public int GetCount(Inventory inventory, string name) => inventory.GetItemCount(name);

        private static IEnumerable<TestCaseData> GetCountCases()
        {
            var item1 = new Item("X", 1, 1);
            var item2 = new Item("X", 1, 1);
            var item3 = new Item("X", 1, 1);
            var item4 = new Item("", 1, 1);
            var item5 = new Item(null, 1, 1);

            var inventory = new Inventory(5, 5,
                new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(0, 0)),
                new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(0, 1)),
                new KeyValuePair<Item, Vector2Int>(item3, new Vector2Int(1, 0)),
                new KeyValuePair<Item, Vector2Int>(item4, new Vector2Int(1, 1)),
                new KeyValuePair<Item, Vector2Int>(item5, new Vector2Int(2, 1))
            );

            yield return new TestCaseData(inventory, "X").Returns(3).SetName("X");
            yield return new TestCaseData(inventory, "").Returns(1).SetName("Empty Name");
            yield return new TestCaseData(inventory, null).Returns(1).SetName("Name is null");
            yield return new TestCaseData(inventory, "F").Returns(0).SetName("Absent items");
        }
    }
}