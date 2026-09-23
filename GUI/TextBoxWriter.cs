// adapted from https://social.msdn.microsoft.com/Forums/vstudio/en-US/8110f566-fe7b-41f6-a92e-5e45955bdec8/redirecting-console-using-setout-and-handling-write-from-multiple-threads?forum=wpf

using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GUI
{
    /// <summary>
    /// A TextWriter that redirects Console output to a RichTextBox.
    ///
    /// Writes are appended to an in-memory buffer and flushed to the UI on a timer (a handful of times
    /// per second) rather than dispatched one-per-write. FlashLFQ can emit an enormous volume of console
    /// output while reading a large or malformed PSM file; dispatching every write to the UI thread (and
    /// scrolling on every append) saturated that thread and froze the whole application. Batching bounds
    /// the amount of UI work regardless of how much is written.
    /// </summary>
    public class TextBoxWriter : TextWriter
    {
        // Cap on how much text is kept in the box. Past this the box is cleared (with a marker) so that
        // layout/rendering of a pathologically large document can't slow the UI to a crawl.
        private const int MaxDisplayedChars = 200_000;

        private readonly RichTextBox _textBox;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly object _lock = new object();
        private readonly DispatcherTimer _flushTimer;
        private int _displayedChars;

        public TextBoxWriter(RichTextBox output)
        {
            _textBox = output;

            _flushTimer = new DispatcherTimer(DispatcherPriority.Background, _textBox.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _flushTimer.Tick += (s, e) => Flush();
            _flushTimer.Start();
        }

        public override void Write(char value)
        {
            lock (_lock)
            {
                _buffer.Append(value);
            }
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            lock (_lock)
            {
                _buffer.Append(value);
            }
        }

        public override void WriteLine(string value)
        {
            lock (_lock)
            {
                _buffer.Append(value).Append(Environment.NewLine);
            }
        }

        public override void Flush()
        {
            string text;
            lock (_lock)
            {
                if (_buffer.Length == 0)
                {
                    return;
                }

                text = _buffer.ToString();
                _buffer.Clear();
            }

            if (_textBox.Dispatcher.CheckAccess())
            {
                AppendAndScroll(text);
            }
            else
            {
                _textBox.Dispatcher.BeginInvoke(new Action(() => AppendAndScroll(text)));
            }
        }

        private void AppendAndScroll(string text)
        {
            // Never append more than the cap in a single operation. Appending a very large string to a
            // RichTextBox is extremely slow (every newline becomes a FlowDocument paragraph), so a single
            // huge flush would hang the UI even though the number of dispatches is bounded. Keep only the
            // most recent tail of an oversized chunk.
            if (text.Length > MaxDisplayedChars)
            {
                text = "[earlier output truncated]" + Environment.NewLine
                    + text.Substring(text.Length - MaxDisplayedChars);
            }

            // Keep the box bounded overall: if it would exceed the cap, start fresh rather than letting
            // the document grow without limit.
            if (_displayedChars + text.Length > MaxDisplayedChars)
            {
                _textBox.Document.Blocks.Clear();
                _displayedChars = 0;
            }

            _textBox.AppendText(text);
            _displayedChars += text.Length;
            _textBox.ScrollToEnd();
        }

        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }
    }
}
