using System.Collections;
using System.Collections.Generic;
using Hikari.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class PieceTest {
        [Test]
        public void PieceShapeI() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[0][i]);
            }
        }

        [Test]
        public void PieceShapeO() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[1][i]);
            }
        }

        [Test]
        public void PieceShapeT() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[2][i]);
            }
        }

        [Test]
        public void PieceShapeJ() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[3][i]);
            }
        }

        [Test]
        public void PieceShapeL() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[4][i]);
            }
        }

        [Test]
        public void PieceShapeS() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[5][i]);
            }
        }

        [Test]
        public void PieceShapeZ() {
            for (var i = 0; i < 4; i++) {
                Debug.Log("Spin " + i);
                GeneratePieceShape(Piece.Shapes[6][i]);
            }
        }


        private void GeneratePieceShape(ushort[] shape) {
            for (var i = shape.Length - 1; i >= 0; i--) {
                var line = shape[i];
                Debug.Log(
                    string.Concat(HasBlock(line, 0), HasBlock(line, 1), HasBlock(line, 2), HasBlock(line, 3), "|"));
            }

            string HasBlock(ushort l, int i) => (l & (1 << i)) > 0 ? "*" : "_";
        }
    }
}