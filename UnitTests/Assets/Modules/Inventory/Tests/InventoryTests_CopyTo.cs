using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Modules.Inventories.Tests
{
    public sealed partial class InventoryTests
    {
        [Test]
        public void CopyTo()
        {
            var x = new Item("X", 2, 2);
            var y = new Item("Y", 1, 3);
            var z = new Item("Z", 2, 1);

            var inventory = new Inventory(3, 3,
                new KeyValuePair<Item, Vector2Int>(x, new Vector2Int(0, 0)),
                new KeyValuePair<Item, Vector2Int>(y, new Vector2Int(2, 0)),
                new KeyValuePair<Item, Vector2Int>(z, new Vector2Int(0, 2))
            );

            var expected = new[,]
            {
                {x, x, z},
                {x, x, z},
                {y, y, y}
            };
            
            // Act
            var actual = new Item[3, 3];
            inventory.CopyTo(actual);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}