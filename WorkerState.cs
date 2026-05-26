namespace OLA
{
    /// <summary>
    /// 任务运行状态。
    /// </summary>
    public enum WorkerState
    {
        Idle = 0,
        Running = 1,
        Paused = 2,
        Resuming = 3,
        Stopped = 4
    }
}
