using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ImGuiOverlay.DMA
{
    /// <summary>
    /// Global thread-safe logger with color-coded console output and optional file logging.
    /// Usage:
    ///     Logger.Info("Game started");
    ///     Logger.Warn("DMA not attached");
    ///     Logger.Debug($"Player count = {count}");
    /// </summary>
    public static class Logger
    {
        private static readonly object _sync = new();
        private static readonly ConcurrentQueue<(string Level, string Message, ConsoleColor Color)> _queue = new();
        private static readonly string _logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        private static readonly string _logFile = Path.Combine(_logDir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        private static Thread? _worker;
        private static bool _running;

        public static bool WriteToFile { get; set; } = true;
        public static bool EnableDebug { get; set; } = true;

        static Logger()
        {
            Directory.CreateDirectory(_logDir);
            StartBackgroundWriter();
        }

        // ?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่
        // Main API
        // ?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่
        public static void Info(string msg)  => Enqueue("INFO", msg, ConsoleColor.Cyan);
        public static void Warn(string msg)  => Enqueue("WARN", msg, ConsoleColor.Yellow);
        public static void Error(string msg) => Enqueue("ERROR", msg, ConsoleColor.Red);
        public static void Debug(string msg)
        {
            if (EnableDebug)
                Enqueue("DEBUG", msg, ConsoleColor.Gray);
        }

        // ?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่
        // Internal queue + worker
        // ?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่
        private static void Enqueue(string level, string msg, ConsoleColor color)
        {
            _queue.Enqueue((level, msg, color));
        }

        private static void StartBackgroundWriter()
        {
            if (_running) return;
            _running = true;
            _worker = new Thread(() =>
            {
                while (_running)
                {
                    if (_queue.TryDequeue(out var entry))
                    {
                        Write(entry.Level, entry.Message, entry.Color);
                    }
                    else Thread.Sleep(4);
                }
            })
            { IsBackground = true, Name = "LoggerWorker" };
            _worker.Start();
        }

        private static void Write(string level, string msg, ConsoleColor color)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            var text = $"[{ts}] [{level}] {msg}";

            lock (_sync)
            {
                var prevColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = prevColor;

                if (WriteToFile)
                {
                    try { File.AppendAllText(_logFile, text + Environment.NewLine); }
                    catch { /* ignore IO errors */ }
                }
            }
        }

        // ?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่
        // Graceful shutdown
        // ?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่?ก่
        public static void Shutdown()
        {
            _running = false;
            _worker?.Join(200);
        }
    }
}