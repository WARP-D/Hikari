using UnityEngine;

namespace Cyanite.Puzzle {
    public class InputController : MonoBehaviour, IController {
        [SerializeField] private int DAS = 7;
        [SerializeField] private int ARR = 2;
        private int leftPressingTime;
        private int rightPressingTime;

        public Command RequestControlUpdate() {
            if (Input.GetKeyDown(KeyCode.C)) return Command.Hold;
            
            var cmd = (Command) 0;
            if (Input.GetKey(KeyCode.LeftArrow)) {
                if (leftPressingTime == 0 ||
                    leftPressingTime > DAS && (leftPressingTime - DAS) % ARR == 0) {
                    cmd |= Command.Left;
                }

                leftPressingTime++;
            } else leftPressingTime = 0;

            if (Input.GetKey(KeyCode.RightArrow)) {
                if (rightPressingTime == 0 ||
                    rightPressingTime > DAS && (rightPressingTime - DAS) % ARR == 0) {
                    cmd |= Command.Right;
                }

                rightPressingTime++;
            } else rightPressingTime = 0;

            if (Input.GetKeyDown(KeyCode.Z)) cmd |= Command.RotateLeft;
            if (Input.GetKeyDown(KeyCode.X)) cmd |= Command.RotateRight;

            if (Input.GetKey(KeyCode.DownArrow)) cmd |= Command.SoftDrop;
            if (Input.GetKeyDown(KeyCode.UpArrow)) cmd |= Command.HardDrop;

            return cmd;
        }
    }
}