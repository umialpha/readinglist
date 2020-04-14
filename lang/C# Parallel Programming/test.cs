using System;
using System.Drawing;
using System.Windows.Forms;

class Program {
    [STAThread]
    static void Main() {
        const string _title = "nguid";
        var counter = 1;
        var form = new Form{
            Text = _title,
            ClientSize = new Size(318, 188),
            AutoScaleDimensions = new SizeF(6F, 12F),
            AutoScaleMode = AutoScaleMode.Font,
        };
        form.Load += (ss,se) => ((Form)ss).Activate();
        var textBox = new TextBox {
            Font = new Font("Calibri",10),
            Multiline = true,
            Location = new Point(12,12),
            Size = new Size(294, 135),
            ReadOnly = true,
            TabIndex = 10,
            ScrollBars = ScrollBars.Vertical,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Text = string.Format("{0}.\r\n{1}\r\n", counter++, NewGuid()),
            BackColor = SystemColors.Window,
        };
        textBox.Click += (ss,se) => {
            var line = textBox.GetLineFromCharIndex(textBox.SelectionStart);
            if(line>=0 && line<textBox.Lines.Length) {
                var guidString = textBox.Lines[line];
                if (string.IsNullOrWhiteSpace(guidString)){
                    Clipboard.Clear();
                    form.Text = _title;
                } else {
                    Clipboard.SetText(guidString);
                    form.Text = string.Format("{0} - {1}", _title, guidString);
                }
            }
        };
        form.Controls.Add(textBox);
        var button = new Button{
            Location = new Point(12,153),
            Size = new Size(75, 23),
            TabIndex = 1,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Text = "&Generate",
        };
        button.Click += (ss,se) => { 
            textBox.Text = string.Format("{0}\r\n{1}.\r\n{2}\r\n", textBox.Text, counter++, NewGuid()); 
            textBox.SelectionStart = textBox.Text.Length;
            textBox.ScrollToCaret();
        };
        form.Controls.Add(button);
        button = new Button{
            Location = new Point(100,153),
            Size = new Size(75, 23),
            TabIndex = 2,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Text = "&Clear",
        };
        button.Click += (ss,se) => textBox.Text = "";
        form.Controls.Add(button);
        button = new Button{
            Location = new Point(190,153),
            Size = new Size(75, 23),
            TabIndex = 3,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Text = "C&lose",
        };
        button.Click += (ss,se) => form.Close();
        form.Controls.Add(button);
        Application.Run(form);
    }

    public static string NewGuid(){
        var guid = Guid.NewGuid();
        return string.Format("{0:N}\r\n{0:D}\r\n{0:B}\r\n{0:P}", guid);
    }
}