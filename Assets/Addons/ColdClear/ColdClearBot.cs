using System;
using Hikari.Puzzle;

namespace Hikari.Addons.ColdClear {
    internal class ColdClearBot : IDisposable {
        private IntPtr bot;
        public ColdClearBot() {
            ColdClearInterface.cc_default_options(out var options);
            ColdClearInterface.cc_default_weights(out var weights);
            bot = ColdClearInterface.cc_launch_async(options,weights);
        }

        public void AddNextPiece(PieceKind piece) {
            switch (piece) {
                case PieceKind.I:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_I);
                    break;
                case PieceKind.O:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_O);
                    break;
                case PieceKind.T:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_T);
                    break;
                case PieceKind.J:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_J);
                    break;
                case PieceKind.L:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_L);
                    break;
                case PieceKind.S:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_S);
                    break;
                case PieceKind.Z:
                    ColdClearInterface.cc_add_next_piece_async(bot,CCPiece.CC_Z);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
            }
        }

        public void Reset(ushort[] field, bool b2b, uint combo) {
            var array = new bool[10 * field.Length];
            for (var i = 0; i < field.Length; i++) {
                for (var j = 0; j < 10; j++) {
                    array[i * 10 + j] = (field[i] & (1 << j)) != 0;
                }
            }
            ColdClearInterface.cc_reset_async(bot,array,b2b,combo);
        }

        public void RequestNextMove(uint incoming) {
            ColdClearInterface.cc_request_next_move(bot,incoming);
        }

        public CCMove? PollNextMove() {
            if (ColdClearInterface.cc_poll_next_move(bot, out var move)) return move;
            else return null;
        }

        private void ReleaseUnmanagedResources() {
            ColdClearInterface.cc_destroy_async(bot);
        }

        public void Dispose() {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ColdClearBot() {
            ReleaseUnmanagedResources();
        }
    }
}