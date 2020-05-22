using System;
using System.Threading;
using StringBuilder = System.Text.StringBuilder;

namespace nfctest
{
    public class TerminalUI : IUniCLI
    {

        readonly Action<string> SetConsole;
        readonly Action<string> setStatus;
        readonly Func<string, string> prompt;

        string[] Chunks;
        int[] ChunkSizes;

        int curChunkIndex;
        readonly StringBuilder Chunk = new StringBuilder();
        int curChunkSize;

        public StringBuilder Display = new StringBuilder();
        int DisplaySize = 0;
        string DisplayString = "";

        readonly int MaxChunks, ChunkSize, MaxConsoleSize;

        public TerminalUI(Action<string> SetConsoleText, Action<string> setStatusText, Func<string, string> promptFunc, int maxChunks = 64, int chunkSize = 256)
        {
            SetConsole = SetConsoleText;
            setStatus = setStatusText;
            prompt = promptFunc;

            MaxChunks = maxChunks;
            ChunkSize = chunkSize;
            MaxConsoleSize = MaxChunks * ChunkSize;

            Chunks = new string[maxChunks];
            ChunkSizes = new int[maxChunks];
            curChunkIndex = 0;
        }

        public void Log(string s)
        {
            LogAppend(s + System.Environment.NewLine);
        }


        public void LogAppend(string s)
        {

            int lineSize = s.Length;

            lock (Display)
            {
                Display.Append(s);
                DisplaySize += lineSize;

                Chunk.Append(s);
                curChunkSize += lineSize;
                if (!LazyUpdating) new Thread(LazyUpdate) { Name = "DroidCLI Lazy Update" }.Start();
            }
        }



        private void RectifyDisplay()
        {
            if (curChunkSize > ChunkSize)
            {
                Chunks[curChunkIndex] = Chunk.ToString();
                ChunkSizes[curChunkIndex] = curChunkSize;

                curChunkIndex++;

                if (curChunkIndex == MaxChunks)
                {
                    curChunkIndex = 0;
                }

                Chunk.Clear();
                curChunkSize = 0;

                DisplaySize -= ChunkSizes[curChunkIndex];
                ChunkSizes[curChunkIndex] = 0;
                Chunks[curChunkIndex] = "";
            }

            if (DisplaySize > MaxConsoleSize)
            {
                Display.Clear();
                GC.Collect();

                for (int i = curChunkIndex + 1; i < MaxChunks; i++)
                {
                    Display.Append(Chunks[i]);
                }
                for (int i = 0; i < curChunkIndex; i++)
                {
                    Display.Append(Chunks[i]);
                }
            }
        }

        public void LogSpecial(string s)
        {
            Log("-_-_-_-_-_-_-_-\n" +
                 s +
                 "\n_-_-_-_-_-_-_-_-");
        }

        public void LogError(Exception ex, string Msg = "")
        {
            string ThrName = System.Threading.Thread.CurrentThread.Name;


            Log("x-------------------x\n" +
 $"{TimeStamp} \t {Msg} {ex} - {ex?.Message} @@ {ex?.StackTrace} \t {(ThrName == null ? null : $"@ {ThrName}")}" +
                 "\nx------------------ - x");
        }

        public static string TimeStamp { get { return $"{DateTime.Now.Hour:00}:{DateTime.Now.Minute:00}:{DateTime.Now.Second:00)}.{DateTime.Now.Millisecond:00}"; } }


        bool LazyUpdating = false;
        void LazyUpdate()
        {
            if (LazyUpdating) return;
            LazyUpdating = true;

            RectifyDisplay();
            System.Threading.Thread.Sleep(500);

            while (IsOnHold)
            {
                System.Threading.Thread.Sleep(200);
            }

            try
            {
                DisplayString = Display.ToString();
                LazyUpdating = false;
                SetConsole(DisplayString);

            }
            catch (System.Exception)
            { }
        }

        public string Prompt(string s)
        {
            // Hold();
            string res = prompt(s);
            // Unhold();
            Log(s);
            return res;
        }

        public bool IsOnHold = false;
        public void Hold()
        {
            IsOnHold = true;
        }

        public void Unhold()
        {
            IsOnHold = false;
        }
        public bool ToggleHold()
        {
            IsOnHold = !IsOnHold;
            return IsOnHold;
        }
        public void Clear()
        {

            lock (Display)
            {

                Chunks = new string[MaxChunks];
                ChunkSizes = new int[MaxChunks];
                curChunkIndex = 0;

                Chunk.Clear();
                Display.Clear();

                DisplayString = "";
                new Thread(LazyUpdate) { Name = "DroidCLI Lazy Clear" }.Start();
            }
        }

        public void SetStatus(string s)
        {
            setStatus(s);
        }
    }

    public class SubProgram
    {
        public CLIView view;
        public Thread Thread;

        public void Terminate()
        {
            Thread.Abort();
        }
    }
}