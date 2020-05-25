using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hikari.AI.Jobs;
using Hikari.Puzzle;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Hikari.AI {
    public class HikariAI : IDisposable {
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
        private BackupJob backupJob;
        private AdvanceTreeJob advanceTreeJob;

        private NativeArray<Random> rngs;
        private NativeArray<SelectResult> selectedArray;
        private NativeList<ExpandResult> expandedList;
        private NativeArray<PieceKind> nextPiecesArray;
        private NativeArray<int4> evaluations;
        private NativeMultiHashMap<int, Node> expandedMap;
        private NativeList<Node> advancedTree;
        private NativeList<SimpleBoard> advancedBoards;

        private NativeArray<int4x4> pieceShapes;

        private JobHandle jobHandle;
        private JobHandle advanceJobHandle;
        private bool scheduled;
        private int completionDelay;

        private const int MaxCompletionDelay = 3;

        private bool requestNextMove;
        private Move? lastMove;

        public int length;
        public int maxDepth;

        public bool useHold = false;
        private bool isAdvancingTree;

        public int ParallelCount { get; set; } = 7*50;
        public int MinDepth { get; set; } = 2;

        public void Start() {
            JobsUtility.JobWorkerCount = 7;
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
            if (isAdvancingTree) {
                isAdvancingTree = false;
                advanceJobHandle.Complete();
                tree.Dispose(default);
                boards.Dispose(default);
                tree = advancedTree;
                boards = advancedBoards;
                advancedTree = default;
                advancedBoards = default;

                nextPieces.Dequeue();
                maxDepth = 0;
                length = tree.Length;
            }
            // Complete previous job
            if (scheduled) {
                if (!jobHandle.IsCompleted) {
                    if (completionDelay < MaxCompletionDelay) {
                        completionDelay++;
                        Debug.Log($"Jobs are not completed, I'll delay completion for {MaxCompletionDelay - completionDelay} more frame{(MaxCompletionDelay - completionDelay == 1 ? "" : "s")}");
                        return;
                    }

                    Debug.LogWarning("Jobs are still not completed, but I force to complete");
                }
                completionDelay = 0;
                
                jobHandle.Complete();
                if (requestNextMove) {
                    if (CreateNextMove(out var picked)) {
                        advanceJobHandle = AdvanceTree(picked);
                        isAdvancingTree = true;
                    }
                }
                
                // Debug.Log(selectJob.retryCounts.Sum());
                // Debug.Log(tree.AsArray().Select(n => n.eval.Sum()).Min());
                // Debug.Log(selectJob.depths.Max());
                length = tree.Length;
                maxDepth = math.max(maxDepth, selectJob.depths.Max());
                DisposeJobs();
                scheduled = false;
                
            }

            // Add pieces to queue if provided
            while (nextPiecesToAdd.Any()) {
                nextPieces.Enqueue(nextPiecesToAdd.Dequeue());
            }
            
            // Ensure that I can expand tree
            if (!nextPieces.IsCreated || nextPieces.Count <= 1) return;

            if (!isAdvancingTree) {
                PrepareAndScheduleJobs();
            }
        }

        private bool CreateNextMove(out int picked) {
            var rootChildrenRef = tree[root].children;
            if (rootChildrenRef.length == 0) {
                picked = -1;
                return false;
            }

            var rootChildren = new NativeSlice<Node>(tree,rootChildrenRef.start, rootChildrenRef.length);
            foreach (var node in rootChildren.Select((n,i) => new IndexedNode(rootChildrenRef.start + i,n)).OrderByDescending(n => n.node.visits)) {
                var b = boards[node.node.parent];
                var move = PathFinder.FindPath(ref b, node.node.piece, pieceShapes);
                if (move != null) {
                    lastMove = move.Value;
                    requestNextMove = false;
                    var mv = move.Value;
                    Debug.Log($"Move picked: {move.Value.piece.ToString()} / {length} Nodes / {maxDepth} Depth");
                    var sb = new StringBuilder();
                    for (var i = 0; i < move.Value.length; i++) {
                        unsafe {
                            sb.Append((Instruction)mv.instructions[i]).Append(' ');
                        }
                    }
                    Debug.Log(sb.ToString());
                    picked = node.index;
                    return true;
                }
            }

            picked = -1;
            return false;
        }

        private void PrepareAndScheduleJobs() {
            var parallelCount = math.min(ParallelCount, tree.Length); //todo is this the right way?
            
            // Prepare queue & tree
            nextPiecesArray = nextPieces.ToArray(Allocator.TempJob);

            rngs = new NativeArray<Random>(ParallelCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            createRngsJob = new CreateRngsJob {
                rng = new NativeArray<Random>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory) {
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
                depths = new NativeArray<int>(parallelCount, Allocator.TempJob),
                retryCounts = new NativeArray<int>(parallelCount, Allocator.TempJob)
            };
            jobHandle = selectJob.Schedule(parallelCount, 1, jobHandle);

            expandedList = new NativeList<ExpandResult>(parallelCount * 300, Allocator.TempJob);
            expandJob = new ExpandJob {
                useHold = new NativeArray<bool>(1, Allocator.TempJob) {
                    [0] = useHold
                },
                pieceShapes = pieceShapes,
                selected = selectJob.selected,
                boards = boards.AsDeferredJobArray(),
                expandResultWriter = expandedList.AsParallelWriter()
            };
            jobHandle = expandJob.Schedule(parallelCount, 1, jobHandle);

            evaluations = new NativeArray<int4>(parallelCount * 300, Allocator.TempJob);
            evaluateJob = new EvaluateJob {
                inputs = expandedList.AsDeferredJobArray(),
                weight = new NativeArray<Weights>(1, Allocator.TempJob) {
                    [0] = Weights.Default
                },
                pieceShapes = pieceShapes,
                results = evaluations,
                boards = boards.AsDeferredJobArray()
            };
            jobHandle = evaluateJob.Schedule(expandedList, 1, jobHandle);

            expandedMap = new NativeMultiHashMap<int, Node>(parallelCount * 300, Allocator.TempJob);
            reorderChildrenJob = new ReorderChildrenJob {
                expandResults = expandedList,
                evaluations = evaluations,
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

            backupJob = new BackupJob {
                expandResults = expandedList,
                evaluations = evaluations,
                tree = tree.AsDeferredJobArray()
            };
            jobHandle = backupJob.Schedule(jobHandle);

            JobHandle.ScheduleBatchedJobs();
            scheduled = true;
        }

        private JobHandle AdvanceTree(int picked) {
            if (maxDepth < MinDepth) throw new Exception();
            
            advancedTree = new NativeList<Node>(1_000_000,Allocator.Persistent);
            advancedBoards = new NativeList<SimpleBoard>(1_000_000, Allocator.Persistent);
            
            advanceTreeJob = new AdvanceTreeJob {
                tree = tree,
                boards = boards,
                advancedTree = advancedTree,
                advancedBoards = advancedBoards,
                picked = picked,
                root = root
            };
            var jh = advanceTreeJob.Schedule();
            JobHandle.ScheduleBatchedJobs();
            return jh;
        }

        public void AddNextPiece(PieceKind pieceKind) {
            nextPiecesToAdd.Enqueue(pieceKind);
        }

        public void RequestNextMove() {
            if (requestNextMove) Debug.LogWarning("Next move is already requested but you requested it again.");
            requestNextMove = true;
        }

        public bool PollNextMove(out Move? move) {
            move = lastMove;
            return !requestNextMove;
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
            
            if (advancedTree.IsCreated) advancedTree.Dispose();
            if (advancedBoards.IsCreated) advancedBoards.Dispose();

            if (nextPieces.IsCreated) nextPieces.Dispose();
            if (pieceShapes.IsCreated) pieceShapes.Dispose();
        }

        private void DisposeJobs(JobHandle inputDeps = default, bool scheduleBatchedJobs = false) {
            if (rngs.IsCreated) rngs.Dispose(inputDeps);
            if (selectJob.selected.IsCreated) selectJob.selected.Dispose(inputDeps);
            if (selectJob.depths.IsCreated) selectJob.depths.Dispose(inputDeps);
            if (selectJob.retryCounts.IsCreated) selectJob.retryCounts.Dispose(inputDeps);
            if (expandedList.IsCreated) expandedList.Dispose(inputDeps);
            if (evaluations.IsCreated) evaluations.Dispose(inputDeps);
            if (expandedMap.IsCreated) expandedMap.Dispose(inputDeps);
            if (nextPiecesArray.IsCreated) nextPiecesArray.Dispose(inputDeps);
            
            if (scheduleBatchedJobs) JobHandle.ScheduleBatchedJobs();
        }
    }
}