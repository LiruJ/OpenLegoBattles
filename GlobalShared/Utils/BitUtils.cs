namespace GlobalShared.Utils
{
    public static class BitUtils
    {
        public static byte calculateBitCount(int value)
        {
            // Get the index of the highest set bit, which is also how many bits the value needs.
            byte bitCount = 0;
            while (value > 0)
            {
                bitCount++;
                value >>= 1;
            }
            return bitCount;
        }
    }
}
