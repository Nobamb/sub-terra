using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubTerra.App.Save
{
    public enum AutoSaveReason
    {
        SurfaceReturn = 0,
        Settlement = 1,
        UpgradePurchased = 2,
        OutpostInstalled = 3,
        NewZoneEntered = 4,
        StructuralFailure = 5,
        QuitRequested = 6,
        PeriodicDirty = 7,
        Manual = 8,
        RunFailure = 9
    }

    /// <summary>
    /// 자동 저장 요청을 하나의 직렬 큐로 합친다. 저장 중 요청은 dirty로 남아 최신 State를 한 번 더 캡처한다.
    /// </summary>
    public sealed class AutoSaveCoordinator : IDisposable
    {
        private readonly object sync = new object();
        private readonly ISaveWriter writer;
        private readonly Func<SaveCaptureContext> capture;
        private readonly int slotId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

        private bool dirty;
        private bool running;
        private Task<SaveResult> activeTask;
        private SaveResult lastResult;

        public AutoSaveReason LatestReason { get; private set; }
        public bool IsRunning
        {
            get
            {
                lock (sync)
                {
                    return running;
                }
            }
        }

        public AutoSaveCoordinator(
            ISaveWriter saveWriter,
            Func<SaveCaptureContext> captureContext,
            int saveSlotId)
        {
            writer = saveWriter ?? throw new ArgumentNullException(nameof(saveWriter));
            capture = captureContext ?? throw new ArgumentNullException(nameof(captureContext));
            slotId = saveSlotId;
        }

        public Task<SaveResult> RequestAsync(AutoSaveReason reason)
        {
            lock (sync)
            {
                LatestReason = reason;
                dirty = true;
                if (running)
                {
                    return activeTask;
                }

                running = true;
                activeTask = RunQueueAsync();
                return activeTask;
            }
        }

        public async Task<bool> FlushAsync(TimeSpan timeout)
        {
            Task current;
            lock (sync)
            {
                current = activeTask;
            }

            if (current == null || current.IsCompleted)
            {
                return true;
            }

            var bounded = Task.Delay(timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
            return await Task.WhenAny(current, bounded) == current;
        }

        public void Dispose()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private async Task<SaveResult> RunQueueAsync()
        {
            await Task.Yield();
            var releasedRunner = false;
            try
            {
                while (!lifetime.IsCancellationRequested)
                {
                    lock (sync)
                    {
                        if (!dirty)
                        {
                            running = false;
                            releasedRunner = true;
                            return lastResult
                                ?? new SaveResult(SaveStatus.Cancelled, slotId);
                        }

                        dirty = false;
                    }

                    SaveCaptureContext context;
                    try
                    {
                        context = capture();
                    }
                    catch
                    {
                        lastResult = new SaveResult(SaveStatus.CaptureFailed, slotId);
                        continue;
                    }

                    lastResult = await writer.SaveAsync(
                        slotId,
                        context,
                        lifetime.Token);
                }
            }
            finally
            {
                if (!releasedRunner)
                {
                    lock (sync)
                    {
                        running = false;
                    }
                }
            }

            return lastResult ?? new SaveResult(SaveStatus.Cancelled, slotId);
        }
    }
}
