using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hikari.AI.Jobs {
    [BurstCompile]
    public struct TreeWriteJob : IJob {
        [ReadOnly] public NativeMultiHashMap<int, Node> map;
        [ReadOnly] public NativeArray<int4x4> pieceShapes;
        public NativeList<Node> tree;
        public NativeList<SimpleBoard> boards;
        
        public void Execute() {
            var keys = map.GetKeyArray(Allocator.Temp);
            for (var i = 0; i < keys.Length; i++) {
                var n = tree[keys[i]];
                if (n.children != default) continue;
                
                var start = tree.Length;
                var values = map.GetValuesForKey(keys[i]);
                var c = 0;
                while (values.MoveNext()) {
                    tree.Add(values.Current);
                    boards.Add(boards[values.Current.parent].AddPieceFast(values.Current.piece, pieceShapes));
                    c++;
                }

                n.children = new ChildrenRef(start,c);
                tree[keys[i]] = n;
            }
        }
    }
}