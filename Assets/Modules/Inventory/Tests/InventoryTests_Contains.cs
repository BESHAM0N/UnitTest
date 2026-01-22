using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [TestCaseSource(nameof(ContainsCases))]
        public bool Contains(Inventory inventory, Item item) => inventory.Contains(item);

        private static IEnumerable<TestCaseData> ContainsCases()
        {
            yield return ContainsTrueCase();
            yield return ContainsFalseCase();
            yield return ContainsWhenInventoryIsEmptyCase();
            yield return ContainsWhenItemIsNullCase();
        }

        private static TestCaseData ContainsTrueCase()
        {
            Item item = new Item("X", width: 2, height: 2);
            Inventory inventory = new Inventory(5, 5,
                new KeyValuePair<Item, Vector2Int>(item, Vector2Int.zero)
            );
            return new TestCaseData(inventory, item).Returns(true).SetName("True");
        }

        private static TestCaseData ContainsFalseCase()
        {
            Item itemB = new Item("B", 2, 2);
            Item itemC = new Item("C", 2, 2);
            Inventory inventory = new Inventory(5, 5,
                new KeyValuePair<Item, Vector2Int>(itemB, Vector2Int.zero)
            );
            return new TestCaseData(inventory, itemC).Returns(false).SetName("False");
        }

        private static TestCaseData ContainsWhenInventoryIsEmptyCase()
        {
            Inventory inventory = new Inventory(5, 5);
            Item item = new Item("B", 2, 2);
            return new TestCaseData(inventory, item).Returns(false).SetName("Inventory is empty");
        }

        private static TestCaseData ContainsWhenItemIsNullCase()
        {
            Inventory inventory = new Inventory(5, 5);
            return new TestCaseData(inventory, null).Returns(false).SetName("Item is null");
        }
    }
}