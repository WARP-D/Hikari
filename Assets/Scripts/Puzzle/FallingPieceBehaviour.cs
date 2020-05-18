using UnityEngine;

namespace Cyanite.Puzzle {
    public class FallingPieceBehaviour : MonoBehaviour {
        public bool transparent;
        public Piece piece;
        public CellBlock[] cells = new CellBlock[4];

        public void MakeShapeAndColor() {
            var shape = piece.GetShape();
            var c = 0;
            for (var i = 0; i < 4; i++) {
                for (var j = 0; j < 4; j++) {
                    if ((shape[i] & (1 << j)) > 0) {
                        cells[c].transform.localPosition = new Vector3(j,i);
                        cells[c].materialIndex = (int) piece.kind + (transparent ? 8 : 0);
                        cells[c].UpdateMaterial();
                        c++;
                    }
                }
            }
        }

        public void UpdatePosition() {
            transform.localPosition = new Vector3(piece.x,piece.y);
        }
    }
}