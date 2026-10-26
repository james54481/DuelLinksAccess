namespace DuelLinksAccess
{
    public enum TradeAvailability { Available, SoldOut, Unavailable }

    public static class TradeExecutionPolicy
    {
        public static TradeAvailability Availability(bool? soldOut, bool? unchangeable, bool? materialsAvailable)
        {
            if (soldOut == true) return TradeAvailability.SoldOut;
            return soldOut == false && unchangeable == false && materialsAvailable == true
                ? TradeAvailability.Available : TradeAvailability.Unavailable;
        }

        public static long? ReadOwnedItem(int itemId, System.Func<int, long> readCount)
        {
            if (itemId <= 0 || readCount == null) return null;
            try
            {
                long count = readCount(itemId);
                return count >= 0 ? count : (long?)null;
            }
            catch { return null; }
        }

        public static bool CanClick(
            int expectedItemId,
            int actualItemId,
            bool buttonActive,
            bool buttonInteractable)
        {
            return expectedItemId > 0
                && actualItemId == expectedItemId
                && buttonActive
                && buttonInteractable;
        }
    }
}
