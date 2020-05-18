using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Cyanite.AI.Jobs {
    [BurstCompile]
    public struct TreeWriteJob : IJob {
        [ReadOnly] public NativeMultiHashMap<int, Node> map;
        public NativeList<Node> tree;
        
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
                    c++;
                }

                n.children = new ChildrenRef(start,c);
                tree[keys[i]] = n;
            }
        }
    }
}