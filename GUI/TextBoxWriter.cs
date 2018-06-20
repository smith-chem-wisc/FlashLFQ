// adapted from https://social.msdn.microsoft.com/Forums/vstudio/en-US/8110f566-fe7b-41f6-a92e-5e45955bdec8/redirecting-console-using-setout-and-handling-write-from-multiple-threads?forum=wpf

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GUI
{
    public class TextBoxWriter : TextWriter
    {
        RichTextBox textBox = null;

        public TextBoxWriter(RichTextBox output)
        {
            textBox = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.AppendText(value.ToString());
            }));
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
