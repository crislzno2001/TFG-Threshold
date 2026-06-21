namespace OpenAI
{
    /// <summary>Outcome categories for the startup AI connection test.</summary>
    public enum AIConnectionStatus
    {
        Ok,
        MissingKey,
        InvalidKey,    // 401 / 403
        RateLimited,   // 429
        NetworkError,  // no connection / DNS / TLS
        Timeout,
        ServerError,   // 5xx
        Unknown
    }

    public struct AIConnectionResult
    {
        public AIConnectionStatus Status;
        public string Message;       // user-facing, friendly
        public long HttpCode;

        public bool IsOk => Status == AIConnectionStatus.Ok;

        public static AIConnectionResult Make(AIConnectionStatus s, string msg, long code = 0)
            => new AIConnectionResult { Status = s, Message = msg, HttpCode = code };
    }
}
