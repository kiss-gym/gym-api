namespace GymApi.Domain.SessionTracking
{
    public class ExerciseSet
    {
        public Guid Id { get; private set; }
        public int SetNumber { get; private set; }
        public decimal? Weight { get; private set; }
        public int? Repetitions { get; private set; }
        public bool IsCompleted { get; private set; }

        private ExerciseSet(int setNumber, decimal? weight, int? repetitions)
        {
            Id = Guid.NewGuid();
            SetNumber = setNumber;
            Weight = weight;
            Repetitions = repetitions;
            IsCompleted = false;
        }

        public static ExerciseSet Create(int setNumber, decimal? weight, int? repetitions)
        {
            return new ExerciseSet(setNumber, weight, repetitions);
        }

        internal void Complete()
        {
            IsCompleted = true;
        }

        internal void UnComplete()
        {
            IsCompleted = false;
        }

        internal void Update(decimal? weight, int? repetitions)
        {
            Weight = weight;
            Repetitions = repetitions;
        }
    }
}
