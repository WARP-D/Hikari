namespace Hikari.AI {
    public struct Evaluation {
        public float defensiveness;
        public float offensiveness;

        public Evaluation(float defensiveness, float offensiveness) {
            this.defensiveness = defensiveness;
            this.offensiveness = offensiveness;
        }

        public float Sum() => offensiveness + defensiveness; //TODO Sum() should be more appropriate method

        public override string ToString() {
            return $"{defensiveness} {offensiveness}";
        }
    }
}