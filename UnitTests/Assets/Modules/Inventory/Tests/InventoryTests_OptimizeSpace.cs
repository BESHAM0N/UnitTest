using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public partial class InventoryTests
    {
        [TestCaseSource(nameof(OptimizeSpaceCases))]
        public void OptimizeSpace(Inventory inventory, Item[,] expected)
        {
            //Act:
            inventory.OptimizeSpace();

            //Assert:
            for (int x = 0; x < inventory.Width; x++)
            for (int y = 0; y < inventory.Height; y++)
                Assert.AreEqual(expected[x, y], inventory.GetItem(x, y));
        }

        private static IEnumerable<TestCaseData> OptimizeSpaceCases()
        {
            yield return OptimizeSimpleCase();
            yield return OptimizeFullCase();
            yield return OptimizeMediumCase();
        }

        private static TestCaseData OptimizeSimpleCase()
        {
            var item1 = new Item("1", 1, 1);
            var item2 = new Item("2", 1, 1);
            var item3 = new Item("3", 1, 1);
            var item4 = new Item("4", 1, 1);
            var item5 = new Item("5", 2, 2);

            return new TestCaseData(new Inventory(4, 4,
                    new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(0, 0)),
                    new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(3, 3)),
                    new KeyValuePair<Item, Vector2Int>(item3, new Vector2Int(0, 3)),
                    new KeyValuePair<Item, Vector2Int>(item4, new Vector2Int(3, 0)),
                    new KeyValuePair<Item, Vector2Int>(item5, new Vector2Int(1, 1))
                ), new[,]
                {
                    {item5, item5, null, null},
                    {item5, item5, null, null},
                    {item1, item3, null, null},
                    {item2, item4, null, null}
                }
            ).SetName("Simple");
        }

        private static TestCaseData OptimizeFullCase()
        {
            var item1 = new Item("1", 1, 2);
            var item2 = new Item("2", 2, 2);
            var item3 = new Item("3", 1, 4);
            var item4 = new Item("4", 3, 2);

            return new TestCaseData(new Inventory(4, 4,
                    new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(0, 0)),
                    new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(1, 0)),
                    new KeyValuePair<Item, Vector2Int>(item3, new Vector2Int(3, 0)),
                    new KeyValuePair<Item, Vector2Int>(item4, new Vector2Int(0, 2))
                ), new[,]
                {
                    {item4, item4, item2, item2},
                    {item4, item4, item2, item2},
                    {item4, item4, item1, item1},
                    {item3, item3, item3, item3}
                }
            ).SetName("Full");
        }

        private static TestCaseData OptimizeMediumCase()
        {
            var item1 = new Item("1", 1, 1);
            var item2 = new Item("2", 2, 3);
            var item3 = new Item("3", 2, 1);
            var item4 = new Item("4", 2, 1);
            var item5 = new Item("5", 2, 2);
            var item6 = new Item("6", 2, 1);
            var item7 = new Item("7", 1, 4);

            return new TestCaseData(new Inventory(5, 5,
                    new KeyValuePair<Item, Vector2Int>(item1, new Vector2Int(0, 0)),
                    new KeyValuePair<Item, Vector2Int>(item2, new Vector2Int(0, 2)),
                    new KeyValuePair<Item, Vector2Int>(item3, new Vector2Int(3, 4)),
                    new KeyValuePair<Item, Vector2Int>(item4, new Vector2Int(2, 3)),
                    new KeyValuePair<Item, Vector2Int>(item5, new Vector2Int(2, 1)),
                    new KeyValuePair<Item, Vector2Int>(item6, new Vector2Int(2, 0)),
                    new KeyValuePair<Item, Vector2Int>(item7, new Vector2Int(4, 0))
                ), new[,]
                {
                    {item2, item2, item2, item4, item1},
                    {item2, item2, item2, item4, null},
                    {item5, item5, item3, item6, null},
                    {item5, item5, item3, item6, null},
                    {item7, item7, item7, item7, null}
                }
            ).SetName("Medium");
        }
    }
}