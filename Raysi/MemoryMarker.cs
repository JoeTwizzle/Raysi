namespace Raysi
{
    public struct MemoryMarker
    {
        public bool Free;
        public int Length;

        public MemoryMarker(bool free, int length)
        {
            Free = free;
            Length = length;
        }
    }
}
