using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public partial class InventoryTests
    {
        [TestCase(0, 0)]
        [TestCase(-1, 10)]
        [TestCase(10, -2)]
        [TestCase(-2, -2)]
        [TestCase(0, 10)]
        [TestCase(5, 0)]
        public void CanAddItem_AtPosition_InvalidSize_ThrowsArgumentException(int width, int height)
        {
            //Arrange:
            var inventory = new Inventory(5, 5);
            var item = new Item(width, height);

            //Assert:
            Assert.Catch<ArgumentException>(() => inventory.CanAddItem(item, Vector2Int.zero));
        }
        
        [TestCaseSource(nameof(CanAddItem_AtPosition_Cases))]
        public bool CanAddItem_AtPosition(Inventory inventory, Item item, Vector2Int position) => 
            inventory.CanAddItem(item, position);

        private static IEnumerable<TestCaseData> CanAddItem_AtPosition_Cases()
        {
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(0, 0)
            ).Returns(true).SetName("Empty Inventory");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 1, 1), new Vector2Int(3, 3))
                ),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(0, 0)
            ).Returns(true).SetName("Free Slot");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(3, 3))
                ),
                new Item("A", new Vector2Int(3, 3)),
                new Vector2Int(1, 1)
            ).Returns(false).SetName("Intersects");

            var item = new Item("X", 1, 1);
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(item, new Vector2Int(3, 3))
                ),
                item,
                new Vector2Int(0, 0)
            ).Returns(false).SetName("Already Exists");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                null,
                new Vector2Int(0, 0)
            ).Returns(false).SetName("Item is null");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(3, 3) 
            ).Returns(true).SetName("Fits Exactly In Bottom-Right Corner");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(4, 0) 
            ).Returns(false).SetName("Out Of Bounds To The Right");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(0, 4) 
            ).Returns(false).SetName("Out Of Bounds To The Bottom");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(1, 1)),
                new Vector2Int(-1, 0)
            ).Returns(false).SetName("Negative X");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(1, 1)),
                new Vector2Int(0, -1)
            ).Returns(false).SetName("Negative Y");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 1, 1), new Vector2Int(2, 2))
                ),
                new Item("A", new Vector2Int(1, 1)),
                new Vector2Int(2, 2)
            ).Returns(false).SetName("Target Cell Already Occupied");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(1, 1)) // занимает (1..2, 1..2)
                ),
                new Item("A", new Vector2Int(2, 1)), // занимает (2..3, 1)
                new Vector2Int(2, 1) // пересекается в клетке (2,1)
            ).Returns(false).SetName("Partial Overlap By One Cell");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(0, 0)) // (0..1,0..1)
                ),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(2, 0) // (2..3,0..1) — вплотную справа, не пересекается
            ).Returns(true).SetName("Adjacent To Existing Item (No Overlap)");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(0, 0)) // (0..1,0..1)
                ),
                new Item("A", new Vector2Int(1, 1)),
                new Vector2Int(2, 2) // касается углом (2,2) vs занято до (1,1) -> нет пересечения
            ).Returns(true).SetName("Touches Only At Corner (No Overlap)");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(6, 1)),
                new Vector2Int(0, 0)
            ).Returns(false).SetName("Item Wider Than Inventory");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 1, 1), new Vector2Int(3, 3))
                ),
                new Item("X", new Vector2Int(1, 1)), // другой объект, но то же имя
                new Vector2Int(0, 0)
            ).Returns(true).SetName("Same Name But Different Instance");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(1, 5)),
                new Vector2Int(4, 0) // x=4 ок для ширины 1
            ).Returns(true).SetName("Fits Exactly On Right Edge");
        }
        
        
        [TestCaseSource(nameof(CanAddItem_AtFreePosition_Cases))]
        public bool CanAddItem_AtFreePosition(Inventory inventory, Item item) => inventory.CanAddItem(item);

        private static IEnumerable<TestCaseData> CanAddItem_AtFreePosition_Cases()
        {
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                new Item("A", new Vector2Int(2, 2))
            ).Returns(true).SetName("Empty Inventory");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 1, 1), new Vector2Int(3, 3))
                ),
                new Item("A", new Vector2Int(2, 2))
            ).Returns(true).SetName("Free Slot");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(2, 2))
                ),
                new Item("A", new Vector2Int(3, 3))
            ).Returns(false).SetName("Intersects");

            var item = new Item("X", 1, 1);
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(item, new Vector2Int(3, 3))
                ),
                item
            ).Returns(false).SetName("Already Exists");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                null
            ).Returns(false).SetName("Item is null");
        }
        
        [TestCase(0, 0)]
        [TestCase(-1, 10)]
        [TestCase(10, -2)]
        [TestCase(-2, -2)]
        [TestCase(0, 10)]
        [TestCase(5, 0)]
        public void CanAddItem_AtFreePosition_InvalidSize_ThrowsArgumentException(int width, int height)
        {
            //Arrange:
            var inventory = new Inventory(5, 5);
            var item = new Item(width, height);

            //Assert:
            Assert.Throws<ArgumentException>(() => inventory.CanAddItem(item));
        }
    }
}
