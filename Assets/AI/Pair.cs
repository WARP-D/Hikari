namespace Hikari.AI {
    public struct Pair<T1,T2> 
        where T1 : struct
        where T2 : struct {
        public T1 Item1;
        public T2 Item2;

        public Pair(T1 item1, T2 item2) {
            Item1 = item1;
            Item2 = item2;
        }

        public void Deconstruct(out T1 item1, out T2 item2) {
            item1 = Item1;
            item2 = Item2;
        }
    }
}