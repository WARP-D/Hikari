using System;
using System.Collections.Generic;
using System.Linq;
using Cyanite.AI.Jobs;
using Cyanite.Puzzle;
using UniRx.Async;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Cyanite.AI {
    public class CyaniteAI : IDisposable {
        private NativeList<Node> tree;
        private NativeList<SimpleBoard> boards;
        private int root;
        private NativeQueue<PieceKind> nextPieces;
        private Queue<PieceKind> nextPiecesToAdd = new Queue<PieceKind>();

        private CreateRngsJob createRngsJob;
        private SelectJob selectJob;
        private ExpandJob expandJob;
        private EvaluateJob evaluateJob;
        private ReorderChildrenJob reorderChildrenJob;
        private TreeWriteJob treeWriteJob;

        private NativeArray<Random> rngs;
        private NativeArray<SelectResult> selectedArray;
        private NativeList<ExpandResult> expandedList;
        private NativeArray<PieceKind> nextPiecesArray;
        private NativeArray<Evaluation> evaluations;
        private NativeMultiHashMap<int, NodeWithPiece> expandedMap;

        private NativeArray<int4x4> pieceShapes;

        private JobHandle jobHandle;
        private bool scheduled;

        private bool requestNextMove;
        private Move? lastMove;

        private int length;

        public bool useHold = false;

        public int ParallelCount { get; set; } = 7*50;

        public void Start() {
            Debug.Log(UnsafeUtility.SizeOf<SimpleBoard>());
            Debug.Log(UnsafeUtility.SizeOf<Node>());
            tree = new NativeList<Node>(1_000_000, Allocator.Persistent) {
                new Node(-1)
            };
            boards = new NativeList<SimpleBoard>(1_000_000, Allocator.Persistent) {
                new SimpleBoard()
            };
            nextPieces = new NativeQueue<PieceKind>(Allocator.Persistent);
            
            pieceShapes = new NativeArray<int>(Piece.OneDShapes,Allocator.Persistent).Reinterpret<int4x4>(UnsafeUtility.SizeOf<int>());
        }

        public void Update() {
            // Complete previous job
            if (scheduled) {
                jobHandle.Complete();
                if (requestNextMove) {
                    //todo
                }
                
                // Debug.Log(selectJob.retryCounts.Sum());
                // Debug.Log(tree.AsArray().Select(n => n.eval.Sum()).Min());
                length = tree.Length;
                DisposeJobs();
                scheduled = false;
                
            }

            // Add pieces to queue if provided
            while (nextPiecesToAdd.Any()) {
                nextPieces.Enqueue(nextPiecesToAdd.Dequeue());
            }
            
            // Ensure that I can expand tree
            if (!nextPieces.IsCreated || nextPieces.Count <= 1) return;

            // Prepare queue & tree
            nextPiecesArray = nextPieces.ToArray(Allocator.TempJob);

            var parallelCount = math.min(ParallelCount, tree.Length); //todo is this the right way?
            
            rngs = new NativeArray<Random>(ParallelCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            createRngsJob = new CreateRngsJob {
                rng = new NativeArray<Random>(1,Allocator.TempJob,NativeArrayOptions.UninitializedMemory) {
                    [0] = new Random((uint) UnityEngine.Random.Range(0, int.MaxValue))
                },
                outputs = rngs
            };
            jobHandle = createRngsJob.Schedule();
            
            selectedArray = new NativeArray<SelectResult>(parallelCount, Allocator.TempJob);
            selectJob = new SelectJob {
                tree = tree.AsDeferredJobArray(),
                boards = boards.AsDeferredJobArray(),
                rootIndex = new NativeArray<int>(1, Allocator.TempJob) {
                    [0] = root
                },
                rands = rngs,
                pieceQueue = nextPiecesArray,
                selected = selectedArray,
                depths = new NativeArray<int>(parallelCount,Allocator.TempJob),
                retryCounts = new NativeArray<int>(parallelCount, Allocator.TempJob)
            };
            jobHandle = selectJob.Schedule(parallelCount,1,jobHandle);
            
            expandedList = new NativeList<ExpandResult>(parallelCount * 300,Allocator.TempJob);
            expandJob = new ExpandJob {
                useHold = new NativeArray<bool>(1,Allocator.TempJob) {
                    [0] = useHold
                },
                pieceShapes = pieceShapes,
                selected = selectJob.selected,
                boards = boards.AsDeferredJobArray(),
                expandResultWriter = expandedList.AsParallelWriter()
            };
            jobHandle = expandJob.Schedule(parallelCount, 1, jobHandle);
            
            evaluations = new NativeArray<Evaluation>(parallelCount * 300, Allocator.TempJob);
            evaluateJob = new EvaluateJob {
                inputs = expandedList.AsDeferredJobArray(),
                weight = new NativeArray<Weights>(1,Allocator.TempJob) {
                    [0] = Weights.Default
                },
                pieceShapes = pieceShapes,
                results = evaluations,
                boards = boards.AsDeferredJobArray()
            };
            jobHandle = evaluateJob.Schedule(expandedList, 1, jobHandle);

            expandedMap = new NativeMultiHashMap<int, NodeWithPiece>(parallelCount * 300,Allocator.TempJob);
            reorderChildrenJob = new ReorderChildrenJob {
                expandResults = expandedList,
                evaluations = evaluations,
                pieceShapes = pieceShapes,
                map = expandedMap.AsParallelWriter()
            };
            jobHandle = reorderChildrenJob.Schedule(expandedList, 4, jobHandle);
            
            treeWriteJob = new TreeWriteJob {
                map = expandedMap,
                tree = tree,
                boards = boards,
                pieceShapes = pieceShapes
            };
            jobHandle = treeWriteJob.Schedule(jobHandle);

            JobHandle.ScheduleBatchedJobs();
            scheduled = true;
            
            
            Debug.Log(length);
        }

        public void AddNextPiece(PieceKind pieceKind) {
            nextPiecesToAdd.Enqueue(pieceKind);
        }

        public async UniTask<Move?> GetNextMove(PlayerLoopTiming timing = PlayerLoopTiming.Update) {
            if (requestNextMove) Debug.LogWarning("Next move is already requested but you requested it again.");
            requestNextMove = true;
            await UniTask.WaitUntil(() => !requestNextMove, timing);
            return lastMove;
        }

        public void GarbageReceived(IEnumerable<ushort> garbageLines) {
            //todo
        }

        public void Reset(IEnumerable<ushort> lines, bool b2b, uint combo) {
            //todo
        }

        public void Dispose() {
            jobHandle.Complete();
            
            DisposeJobs();
            
            if (tree.IsCreated) tree.Dispose();
            if (boards.IsCreated) boards.Dispose();

            if (nextPieces.IsCreated) nextPieces.Dispose();
            if (pieceShapes.IsCreated) pieceShapes.Dispose();
        }

        private void DisposeJobs() {
            // if (treeCopy.IsCreated) treeCopy.Dispose();
            if (rngs.IsCreated) rngs.Dispose(default);
            if (selectJob.selected.IsCreated) selectJob.selected.Dispose(default);
            if (selectJob.depths.IsCreated) selectJob.depths.Dispose(default);
            if (selectJob.retryCounts.IsCreated) selectJob.retryCounts.Dispose(default);
            if (expandedList.IsCreated) expandedList.Dispose(default);
            if (evaluations.IsCreated) evaluations.Dispose(default);
            if (expandedMap.IsCreated) expandedMap.Dispose(default);
            if (nextPiecesArray.IsCreated) nextPiecesArray.Dispose(default);
            
            JobHandle.ScheduleBatchedJobs();
        }
    }
}