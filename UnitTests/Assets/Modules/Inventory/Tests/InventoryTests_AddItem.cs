using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [Test]
        public void AddItem_AtPosition_ContainsItem()
        {
            //Arrange
            var inventory = new Inventory(10, 10);

            var item = new Item(2, 2);
            var position = new Vector2Int(4, 4);
                  
            // Act
            inventory.AddItem(item, position);
            
            // Assert
            Assert.IsTrue(inventory.Contains(item));
        }
        
        [Test]
        public void AddItem_AtPosition_IncreasesCount()
        {
            //Arrange
            var inventory = new Inventory(10, 10);
            int count = inventory.Count;

            var item = new Item(2, 2);
            var position = new Vector2Int(4, 4);
                  
            // Act
            inventory.AddItem(item, position);
            
            // Assert
            Assert.AreEqual(count + 1, inventory.Count);
        }
        
        [Test]
        public void AddItem_AtPosition_OccupiesSlots()
        {
            //Arrange
            var inventory = new Inventory(10, 10);

            var item = new Item(2, 1);
            var position = new Vector2Int(4, 3);
                  
            // Act
            inventory.AddItem(item, position);
            
            // Assert
            for (int x = 4; x < 6; x++)
            for (int y = 3; y < 4; y++)
                Assert.IsTrue(inventory.IsOccupied(x, y));
        }
        
        [Test]
        public void AddItem_AtPosition_InvokesOnAdded()
        {
            // Arrange
            var inventory = new Inventory(10, 10);
            var item = new Item(2, 2);
            var position = new Vector2Int(4, 4);
            
            Item addedItem = null;
            Vector2Int addedPosition = Vector2Int.zero;

            inventory.OnAdded += (i, p) =>
            {
                addedItem = i;
                addedPosition = p;
            };
            
            // Act
            inventory.AddItem(item, position);
            
            // Assert
            Assert.AreEqual(item, addedItem);
            Assert.AreEqual(addedPosition, position);
        }
        
        [TestCaseSource(nameof(AddItem_AtPosition_ReturnsTrue_Cases))]
        public void AddItem_AtPosition(Item item, Vector2Int position)
        {
            var inventory = new Inventory(width: 5, height: 5);
            
            // Act
            bool success = inventory.AddItem(item, position);

            // Assert
            Assert.IsTrue(success);
        }

        private static IEnumerable<TestCaseData> AddItem_AtPosition_ReturnsTrue_Cases()
        {
            yield return new TestCaseData(
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(0, 0)
            ).SetName("(2, 2) at (0, 0)");

            yield return new TestCaseData(
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int(3, 3)
            ).SetName("(2, 2) at (3, 3)");

            yield return new TestCaseData(
                new Item("A", new Vector2Int(5, 5)),
                new Vector2Int(0, 0)
            ).SetName("Full Item");

            yield return new TestCaseData(
                new Item(new Vector2Int(5, 5)),
                new Vector2Int(0, 0)
            ).SetName("Without name");
        }
        
        [Test]
        public void AddItem_ItemIsNull_ReturnsFalse()
        {
            // Arrange
            var inventory = new Inventory(5, 5);

            // Act
            var success = inventory.AddItem(null, 0, 0);

            // Assert
            Assert.IsFalse(success);
        }

        [Test]
        public void AddItem_ItemIsNull_DoesNotChangeCount()
        {
            // Arrange
            var inventory = new Inventory(5, 5);

            // Act
            inventory.AddItem(null, 0, 0);

            // Assert
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void AddItem_ItemIsNull_TargetSlotRemainsFree()
        {
            // Arrange
            var inventory = new Inventory(5, 5);

            // Act
            inventory.AddItem(null, 0, 0);

            // Assert
            Assert.IsTrue(inventory.IsFree(0, 0));
        }

        [Test]
        public void AddItem_ItemIsNull_DoesNotRaiseOnAddedEvent()
        {
            // Arrange
            var inventory = new Inventory(5, 5);

            Item addedItem = null;
            Vector2Int? addedPosition = null;
            bool eventRaised = false;

            inventory.OnAdded += (i, p) =>
            {
                eventRaised = true;
                addedItem = i;
                addedPosition = p;
            };

            // Act
            inventory.AddItem(null, 0, 0);

            // Assert
            Assert.IsFalse(eventRaised);
            Assert.IsNull(addedItem);
            Assert.IsNull(addedPosition);
        }

        [Test]
        public void AddItem_AtPositionOutOfRange_DoesNotInvokeOnAdded()
        {
            //Arrange:
            Item addedItem = null;
            Vector2Int addedPosition = Vector2Int.zero;

            var inventory = new Inventory(5, 5);
            var item = new Item("A", 1, 1);
            var position = new Vector2Int(-1, 0);

            inventory.OnAdded += (i, p) =>
            {
                addedItem = i;
                addedPosition = p;
            };

            //Act:
            inventory.AddItem(item, position);

            //Assert:
            Assert.AreEqual(Vector2Int.zero, addedPosition);
            Assert.IsNull(addedItem);
        }

        [TestCaseSource(nameof(AddItem_OutOfRangePosition_ReturnsFalse_Cases))]
        public void AddItem_AtPositionOutOfRange_ReturnsFalse(Item item, Vector2Int position)
        {
            //Arrange
            var inventory = new Inventory(5, 5);

            //Act
            bool success = inventory.AddItem(item, position);

            //Assert
            Assert.IsFalse(success);
        }

        private static IEnumerable<TestCaseData> AddItem_OutOfRangePosition_ReturnsFalse_Cases()
        {
            yield return new TestCaseData(
                new Item("A", 1, 1),
                new Vector2Int(-1, 0)
            ).SetName("Inventory (5, 5); Item: (1, 1); Position: (-1, 0)");

            yield return new TestCaseData(
                new Item("A", 1, 1),
                new Vector2Int(-1, -1)
            ).SetName("Inventory (5, 5); Item: (1, 1); Position: (-1, -1)");

            yield return new TestCaseData(
                new Item("A", 1, 1),
                new Vector2Int(0, -1)
            ).SetName("Inventory (5, 5); Item: (1, 1); Position: (0, -1)");

            yield return new TestCaseData(
                new Item("A", 1, 1),
                new Vector2Int(5, 5)
            ).SetName("Inventory (5, 5); Item: (1, 1); Position: (5, 5)");

            yield return new TestCaseData(
                new Item("A", 1, 1),
                new Vector2Int(5, 0)
            ).SetName("Inventory (5, 5); Item: (1, 1); Position: (5, 0)");

            yield return new TestCaseData(
                new Item("A", 1, 1),
                new Vector2Int(0, 5)
            ).SetName("Inventory (5, 5); Item: (1, 1); Position: (0, 5)");

            yield return new TestCaseData(
                new Item("A", 1, 2),
                new Vector2Int(0, 4)
            ).SetName("Inventory (5, 5); Item: (1, 2); Position: (0, 4)");

            yield return new TestCaseData(
                new Item(2, 3),
                new Vector2Int(3, 3)
            ).SetName("Inventory (5, 5); Item: (2, 3); Position: (3, 3)");
        }
        
        [Test]
        public void AddItem_ItemAlreadyExistsInInventory_ReturnsFalse()
        {
            // Arrange
            var item = new Item(1, 1);
            var inventory = new Inventory(5, 5);
            inventory.AddItem(item, 3, 3);
            
            // Act
            var success = inventory.AddItem(item, 0, 0);

            // Assert
            Assert.IsFalse(success);
        }

        [Test]
        public void AddItem_ItemAlreadyExistsInInventory_DoesNotRaiseOnAddedEvent()
        {
            // Arrange
            var item = new Item(1, 1);
            var inventory = new Inventory(5, 5);
            inventory.AddItem(item, 3, 3);

            bool eventRaised = false;
            inventory.OnAdded += (_, _) => eventRaised = true;

            // Act
            inventory.AddItem(item, 0, 0);

            // Assert
            Assert.IsFalse(eventRaised);
        }

        [Test]
        public void AddItem_ItemAlreadyExistsInInventory_DoesNotChangeCount()
        {
            // Arrange
            var item = new Item(1, 1);
            var inventory = new Inventory(5, 5);
            inventory.AddItem(item, 3, 3);

            // Act
            inventory.AddItem(item, 0, 0);

            // Assert
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void AddItem_ItemAlreadyExistsInInventory_TargetSlotRemainsFree()
        {
            // Arrange
            var item = new Item(1, 1);
            var inventory = new Inventory(5, 5);
            inventory.AddItem(item, 3, 3);

            // Act
            inventory.AddItem(item, 0, 0);

            // Assert
            Assert.IsTrue(inventory.IsFree(0, 0));
        }
        
        [TestCaseSource(nameof(AddItem_AtPosition_ThatIntersectsWithAnotherItem_ReturnsFalse_Cases))]
        public void AddItem_AtPosition_ThatIntersectsWithAnotherItem_ReturnsFalse(
            Inventory inventory,
            Item item,
            Vector2Int position
        )
        {
            //Act:
            bool success = inventory.AddItem(new Item(2, 2), 2, 2);

            Assert.IsFalse(success);
        }

        private static IEnumerable<TestCaseData> AddItem_AtPosition_ThatIntersectsWithAnotherItem_ReturnsFalse_Cases()
        {
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 1, 1), new Vector2Int(3, 3))
                ),
                new Item("Y", 2, 2),
                new Vector2Int(2, 2)
            ).SetName("Side Intersect");

            yield return new TestCaseData(
                new Inventory(width: 3, height: 3,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(1, 1))
                ),
                new Item("Y", 2, 2),
                new Vector2Int(1, 1)
            ).SetName("Full Intersect");
        }
        
        
        [TestCaseSource(nameof(AddItem_AtFreePosition_ReturnsTrue_Cases))]
        public void AddItem_AtFreePosition_ReturnsTrue(KeyValuePair<Item, Vector2Int>[] initialItems, Item item, Vector2Int expectedPosition)
        {
            var inventory = new Inventory(width: 5, height: 5);
            foreach (var initialItem in initialItems) 
                inventory.AddItem(initialItem.Key, initialItem.Value);
            
            //Act:
            bool success = inventory.AddItem(item);

            //Assert:
            Assert.IsTrue(success);
        }

        private static IEnumerable<TestCaseData> AddItem_AtFreePosition_ReturnsTrue_Cases()
        {
            yield return new TestCaseData(
                Array.Empty<KeyValuePair<Item, Vector2Int>>(),
                new Item("A", new Vector2Int(2, 2)),
                new Vector2Int()
            ).SetName("Empty Inventory");

            yield return new TestCaseData(
                new KeyValuePair<Item,Vector2Int>[]
                {
                    new(new Item("X", 1, 1), new Vector2Int(1, 1))
                },
                new Item("A", new Vector2Int(3, 3)),
                new Vector2Int(2, 0)
            ).SetName("Free Slot");

            yield return new TestCaseData(
                Array.Empty<KeyValuePair<Item, Vector2Int>>(),
                new Item("A", new Vector2Int(5, 5)),
                new Vector2Int(0, 0)
            ).SetName("Full Item");


            yield return new TestCaseData(
                Array.Empty<KeyValuePair<Item, Vector2Int>>(),
                new Item(new Vector2Int(5, 5)),
                new Vector2Int(0, 0)
            ).SetName("Without name");
        }
        
        [TestCaseSource(nameof(AddItem_AtFreePosition_ReturnsFalse_Cases))]
        public void AddItem_AtFreePosition_ReturnsFalse(Inventory inventory, Item item)
        {
            // Act
            bool success = inventory.AddItem(item);

            // Assert
            Assert.IsFalse(success);
        }

        private static IEnumerable<TestCaseData> AddItem_AtFreePosition_ReturnsFalse_Cases()
        {
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(new Item("X", 2, 2), new Vector2Int(2, 2))
                ),
                new Item("A", new Vector2Int(3, 3))
            ).SetName("Intersects");

            var item = new Item("X", 1, 1);
            yield return new TestCaseData(
                new Inventory(width: 5, height: 5,
                    new KeyValuePair<Item, Vector2Int>(item, new Vector2Int(3, 3))
                ),
                item
            ).SetName("Already Exists");

            yield return new TestCaseData(
                new Inventory(width: 5, height: 5),
                null
            ).SetName("Item is null");
        }
        
        [TestCase(0, 0)]
        [TestCase(-1, 10)]
        [TestCase(10, -2)]
        [TestCase(-2, -2)]
        [TestCase(0, 10)]
        [TestCase(5, 0)]
        public void AddItem_AtPosition_And_ItemWithInvalidSize_ThrowsArgumentException(int width, int height)
        {
            //Arrange:
            var inventory = new Inventory(5, 5);
            var item = new Item(width, height);

            //Assert:
            Assert.Catch<ArgumentException>(() => inventory.AddItem(item, Vector2Int.zero));
        }
        
        [TestCase(0, 0)]
        [TestCase(-1, 10)]
        [TestCase(10, -2)]
        [TestCase(-2, -2)]
        [TestCase(0, 10)]
        [TestCase(5, 0)]
        public void AddItem_AtFreePosition_And_ItemWithInvalidSize_ThrowsArgumentException(int width, int height)
        {
            //Arrange:
            var inventory = new Inventory(5, 5);
            var item = new Item(width, height);

            //Assert:
            Assert.Catch<ArgumentException>(() => inventory.AddItem(item));
        }
    }
}