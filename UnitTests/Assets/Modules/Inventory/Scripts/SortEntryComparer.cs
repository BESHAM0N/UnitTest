using System.Collections.Generic;

namespace Modules.Inventories
{
    public sealed class SortEntryComparer : IComparer<SortEntry>
    {
        public int Compare(SortEntry a, SortEntry b)
        {
            var area = b.Item.CellSize.CompareTo(a.Item.CellSize);
            if (area != 0) 
                return area;

            var width = b.Item.Size.x.CompareTo(a.Item.Size.x);
            if (width != 0) 
                return width;

            var height = b.Item.Size.y.CompareTo(a.Item.Size.y);
            if (height != 0) 
                return height;

            return a.Order.CompareTo(b.Order);
        }
    }
}