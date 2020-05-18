namespace Cyanite.AI {
    public readonly struct IndexedNode {
        public readonly int index;
        public readonly Node node;

        public IndexedNode(int index, Node node) {
            this.index = index;
            this.node = node;
        }

        public static implicit operator Node(IndexedNode indexedNode) => indexedNode.node;
    }
}