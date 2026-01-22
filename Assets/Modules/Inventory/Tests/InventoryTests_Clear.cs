using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [Test]
        public void Clear_InventoryIsEmpty_OnClearedNotRisen()
        {
            //Arrange:
            var inventory = new Inventory(5, 5);

            bool wasEvent = false;
            inventory.OnCleared += () => wasEvent = true;

            //Act:
            inventory.Clear();

            //Assert:
            Assert.IsFalse(wasEvent);
        }
        
        [Test]
        public void Clear_RemovesAllItems_CountBecomesZero()
        {
            // Arrange
            var (inventory, itemD, itemX) = CreateInventoryWithTwoItems();

            // Act
            inventory.Clear();

            // Assert
            Assert.AreEqual(0, inventory.Count);
            Assert.IsFalse(inventory.Contains(itemD));
            Assert.IsFalse(inventory.Contains(itemX));
        }

        [Test]
        public void Clear_RaisesOnClearedEvent()
        {
            // Arrange
            var (inventory, _, _) = CreateInventoryWithTwoItems();
            bool wasEventRaised = false;
            inventory.OnCleared += () => wasEventRaised = true;

            // Act
            inventory.Clear();

            // Assert
            Assert.IsTrue(wasEventRaised);
        }

        [Test]
        public void Clear_InventoryEnumerationBecomesEmpty()
        {
            // Arrange
            var (inventory, _, _) = CreateInventoryWithTwoItems();

            // Act
            inventory.Clear();

            // Assert
            CollectionAssert.IsEmpty(inventory);
        }

        [Test]
        public void Clear_MakesAllSlotsFree()
        {
            // Arrange
            var (inventory, _, _) = CreateInventoryWithTwoItems();

            // Act
            inventory.Clear();

            // Assert
            for (int x = 0; x < inventory.Width; x++)
            for (int y = 0; y < inventory.Height; y++)
                Assert.IsTrue(inventory.IsFree(x, y), $"Slot [{x},{y}] should be free after Clear()");
        }

        private static (Inventory inventory, Item itemD, Item itemX) CreateInventoryWithTwoItems()
        {
            var itemD = new Item("D", 1, 2);
            var itemX = new Item("X", 3, 2);

            var inventory = new Inventory(5, 5,
                new KeyValuePair<Item, Vector2Int>(itemD, new Vector2Int(4, 0)),
                new KeyValuePair<Item, Vector2Int>(itemX, new Vector2Int(1, 2))
            );

            return (inventory, itemD, itemX);
        }
    }
}