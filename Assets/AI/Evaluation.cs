namespace Hikari.AI {
    public struct Evaluation {
        public int defensiveness;
        public int offensiveness;

        public Evaluation(int defensiveness, int offensiveness) {
            this.defensiveness = defensiveness;
            this.offensiveness = offensiveness;
        }

        public int Sum() => offensiveness + defensiveness; //TODO Sum() should be more appropriate method

        public override string ToString() {
            return $"{defensiveness} {offensiveness}";
        }
    }
}