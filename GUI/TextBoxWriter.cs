// adapted from https://social.msdn.microsoft.com/Forums/vstudio/en-US/8110f566-fe7b-41f6-a92e-5e45955bdec8/redirecting-console-using-setout-and-handling-write-from-multiple-threads?forum=wpf

using System;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace GUI
{
    public class TextBoxWriter : TextWriter
    {
        private readonly RichTextBox _textBox;

        public TextBoxWriter(RichTextBox output)
        {
            _textBox = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            _textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.AppendText(value.ToString());
            }));
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
