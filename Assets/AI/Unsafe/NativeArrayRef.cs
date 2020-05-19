using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Hikari.AI.Unsafe {
    public readonly unsafe struct NativeArrayRef<T> : IDisposable where T : struct {
        private readonly void* ptr;
        private readonly int length;
        private readonly Allocator allocator;

        public NativeArray<T> Dereference(bool transferControl = false) {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, transferControl ? allocator : Allocator.None);
            
            // これをやらないとNativeArrayのインデクサアクセス時に死ぬ...らしい
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, transferControl ? AtomicSafetyHandle.Create() 
                : AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
            
            return array;
        }

        public NativeArrayRef(NativeArray<T> array, Allocator alloc) {
            ptr = array.GetUnsafePtr();
            length = array.Length;
            allocator = alloc;
        }

        public void Dispose() {
            var array = Dereference(true);
            if (array.IsCreated) array.Dispose();
        }
    }

    public static class NativeArrayRefExtensions {
        public static NativeArrayRef<T> AsArrayRef<T>(this NativeArray<T> array, Allocator alloc) where T : struct {
            return new NativeArrayRef<T>(array, alloc);
        }
    }
}