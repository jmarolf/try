using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;

namespace WorkspaceServer.Servers.Roslyn
{
    public class ConsoleOutput : IDisposable
    {
        private TextWriter originalOutputWriter;
        private TextWriter originalErrorWriter;
        private readonly StringWriter outputWriter = new StringWriter();
        private readonly StringWriter errorWriter = new StringWriter();

        private const int NOT_DISPOSED = 0;
        private const int DISPOSED = 1;

        private int alreadyDisposed = NOT_DISPOSED;

        private static readonly SemaphoreSlim consoleLock = new SemaphoreSlim(1, 1);

        private ConsoleOutput()
        {
        }

        public static async Task<ConsoleOutput> Capture(Budget budget = null)
        {
            budget = budget ?? new TimeBudget(TimeSpan.FromSeconds(15));

            var redirector = new ConsoleOutput();

            budget.CancellationToken.Register(redirector.Dispose);

            await consoleLock.WaitAsync(budget.CancellationToken);

            try
            {
                redirector.originalOutputWriter = Console.Out;
                redirector.originalErrorWriter = Console.Error;

                Console.SetOut(redirector.outputWriter);
                Console.SetError(redirector.errorWriter);
            }
            catch
            {
                consoleLock.Release();
                throw;
            }

            return redirector;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref alreadyDisposed, DISPOSED, NOT_DISPOSED) == NOT_DISPOSED)
            {
                if (originalOutputWriter != null)
                {
                    Console.SetOut(originalOutputWriter);
                }
                if (originalErrorWriter != null)
                {
                    Console.SetError(originalErrorWriter);
                }

                consoleLock.Release();
            }
        }

        public string StandardOutput => outputWriter.ToString();

        public string StandardError => errorWriter.ToString();

        public void Clear()
        {
            outputWriter.GetStringBuilder().Clear();
            errorWriter.GetStringBuilder().Clear();
        }

        public bool IsEmpty() => outputWriter.ToString().Length == 0 && errorWriter.ToString().Length == 0;
    }
}
