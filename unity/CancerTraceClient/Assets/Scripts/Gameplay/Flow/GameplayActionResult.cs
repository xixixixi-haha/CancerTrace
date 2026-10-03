namespace CancerTrace.Gameplay.Flow
{
    public enum GameplayActionStatus
    {
        Success,
        InsufficientRp,
        AlreadyUsed,
        AlreadySubmitted,
        InvalidDiagnosis,
        ShiftCompleted,
        InvalidState
    }

    public sealed class GameplayActionResult<T>
    {
        private GameplayActionResult(GameplayActionStatus status, T data, string message)
        {
            Status = status;
            Data = data;
            Message = message;
        }

        public bool Success
        {
            get { return Status == GameplayActionStatus.Success; }
        }

        public GameplayActionStatus Status { get; private set; }
        public T Data { get; private set; }
        public string Message { get; private set; }

        internal static GameplayActionResult<T> Succeeded(T data)
        {
            return new GameplayActionResult<T>(GameplayActionStatus.Success, data, null);
        }

        internal static GameplayActionResult<T> Failed(
            GameplayActionStatus status,
            string message)
        {
            return new GameplayActionResult<T>(status, default(T), message);
        }
    }
}
