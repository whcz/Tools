﻿using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Winkhaus.Whokna.OutputManager.RTFControl
{
    [ClassInterface(ClassInterfaceType.None), ComSourceInterfaces(typeof(_DPadEvents)), Guid("D3409E2C-F40B-11D1-AEA5-0000E8D88491")]
    public partial class Pad : UserControl, _DPad, _DPadEvents
	{
        private FontSelector _fontSelector;
        private ColorSelector _colorSelector;
        private SizeSelector _sizeSelector;
        private bool _rtfBoxHadFocus;

		public Pad()
		{
			InitializeComponent();

            tbBold.Tag = FontStyle.Bold;
            tbItalic.Tag = FontStyle.Italic;
            tbUnderline.Tag = FontStyle.Underline;
            tbStrikeout.Tag = FontStyle.Strikeout;

            _fontSelector = new FontSelector(cmbFont);
            _colorSelector = new ColorSelector(cmbColor);
            _sizeSelector = new SizeSelector(cmbSize);

            SetToolbarFontDefaults();
		}

        private void SetToolbarFontDefaults()
        {
            if (!TryRestoreSavedSettings())
            {
                _fontSelector.TrySelectFontFamily(_fontSelector.GetDefaultFontFamily());
                _colorSelector.TrySelectColor(_colorSelector.GetDefaultColor());
                _sizeSelector.TrySelectSize(_sizeSelector.GetDefaultSize());

                tbBold.Checked = false;
                tbItalic.Checked = false;
                tbUnderline.Checked = false;
                tbStrikeout.Checked = false;
                SetButtonAlignmentState(HorizontalAlignment.Left);
            }

            rtfBox.Font = new Font(_fontSelector.GetSelectedFontFamily(), _sizeSelector.GetSelectedSize(), GetCurrentFontStyle());
            rtfBox.ForeColor = _colorSelector.GetSelectedColor();
        }

        #region interface members _DPad + _DPadEvents

        [DispId(1)]
        long _DPad.Height
        {
            get 
            {
                //Log("getHeight");
                return this.Height;
            }
            set 
            {
                //Log("setHeight(" + value + ")");
                this.Height = (int)value; 
            }
        }
        [DispId(2)]
        long _DPad.MaxWidth
        {
            get
            {
                //Log("getMaxWidth");
                return 600L;
            }
            set
            {
                //Log("setMaxWidth(" + value + ")");
                //this.MaximumSize = this.GetPreferredSize(new Size(620, 300));
            }
        }
        [DispId(3)]
        long _DPad.MaxHeight
        {
            get
            {
                //Log("getMaxHeight");
                if (base.Height < 250)
                {
                    return 250L;
                }
                return (long)base.Height;
            }
            set
            {
                //Log("setMaxHeight(" + value + ")");
                //this.MaximumSize = this.GetPreferredSize(new Size(620, 300));
            }
        }
        [DispId(4)]
        bool _DPad.Modified
        {
            get;
            set;
        }
        [DispId(5)]
        void _DPad.AppendText(object text)
        {
            this.rtfBox.Rtf.Insert(this.rtfBox.Rtf.Length, (string)text);
        }
        [DispId(6)]
        void _DPad.SelectAll(bool bSel)
        {
            if (bSel)
            {
                this.rtfBox.SelectAll();
                return;
            }
            this.rtfBox.SelectedText = null;
        }
        [DispId(7)]
        void _DPad.SetData(object data)
        {
            this.SetText((byte[])data);
        }
        [DispId(8)]
        object _DPad.GetData()
        {
            return this.GetText();
        }
        [DispId(9)]
        long _DPad.DrawToMetaFile(long pDC, bool draw)
        {
            return 0L;
        }
        [DispId(1)]
        void _DPadEvents.Modified()
        {
            this.GetText();
        }

        private void SetModify(bool state = true)
        {
            ((_DPad)this).Modified = state;
        }

        #endregion

        #region kompatibilita s původní implementací?

        public string defaultFontFamily
        {
            get;
            set;
        }
        public float defaultFontSize
        {
            get;
            set;
        }
        public int defaultLineHeight
        {
            get;
            set;
        }
        public Color defaultFontColor
        {
            get;
            set;
        }

        #endregion

        public void SetText(byte[] binaryData)
        {
            if (binaryData == null || binaryData.Length <= 1)
            {
                this.rtfBox.Clear();
                SetToolbarFontDefaults();
                SetSelectionByToolbar();
                return;
            }
            
            try
            {
                using (MemoryStream stream = new MemoryStream(binaryData))
                using (StreamReader streamReader = new StreamReader(stream, Encoding.ASCII))
                {
                    this.rtfBox.Rtf = streamReader.ReadToEnd();
                    SetToolbarBySelection();
                }
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Nastala chyba při načítání textu poznámky.");
            }
        }

        public byte[] GetText()
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            string s = this.rtfBox.Rtf + "\0";
            return encoding.GetBytes(s);
        }

        public byte[] GetTextAndClearPad()
        {
            byte[] text = this.GetText();
            this.rtfBox.Clear();
            return text;
        }

        public byte[] GetRtfLength()
        {
            return BitConverter.GetBytes(this.rtfBox.Rtf.Length);
        }

        private void rtfBox_TextChanged(object sender, EventArgs e)
        {
            if (((_DPad)this).Modified)
            {
                try
                {
                    ((_DPadEvents)this).Modified();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void rtfBox_KeyDown(object sender, KeyEventArgs e)
        {
            SetModify();
            if (e.KeyCode == Keys.Return)
            {
                var font = rtfBox.SelectionFont;
                var color = rtfBox.SelectionColor;

                rtfBox.SelectedText = "\r\n";

                rtfBox.SelectionFont = font;
                rtfBox.SelectionColor = color;
                SetToolbarBySelection();

                e.Handled = true;
            }
            else if (e.KeyCode == Keys.A && e.Control)
            {
                rtfBox.SelectionStart = 0;
                rtfBox.SelectionLength = rtfBox.TextLength;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.B && e.Control)
            {
                tbBold.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.U && e.Control)
            {
                tbUnderline.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.I && e.Control)
            {
                tbItalic.PerformClick();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
            }
        }

        #region Methods for handling saving, etc...

        private void SaveRtfText()
        {
            using (SaveFileDialog dialog = DialogFactory.GetSaveRtfDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    rtfBox.SaveFile(dialog.FileName);
                }
            }
        }

        private void OpenRtfText()
        {
            using (OpenFileDialog dialog = DialogFactory.GetOpenRtfDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    rtfBox.Clear();
                    rtfBox.LoadFile(dialog.FileName);
                    SetModify();
                }
            }
        }

        private void SetAlignment(HorizontalAlignment alignment)
        {
            rtfBox.SelectionAlignment = alignment;
            SetButtonAlignmentState(rtfBox.SelectionAlignment);
            SetModify();
        }

        private bool TrySetCurrentColor()
        {
            if (_rtfBoxHadFocus)
            {
                SetCurrentColor();
            }

            return _rtfBoxHadFocus;
        }

        private void SetCurrentColor()
        {
            rtfBox.SelectionColor = _colorSelector.GetSelectedColor();
            SetModify();
        }

        private bool TrySetCurrentFont(Func<Font, Font> fontModifier)
        {
            if (_rtfBoxHadFocus)
            {
                SetCurrentFont(fontModifier);
            }

            return _rtfBoxHadFocus;
        }

        private void SetCurrentFont(Func<Font, Font> fontModifier)
        {
            if (rtfBox.SelectionLength != 0)
            {
                // úprava fontu multistyle výběru
                using (RichTextBox rtfTmp = new RichTextBox())
                {
                    rtfTmp.SelectedRtf = rtfBox.SelectedRtf;
                    int start = rtfBox.SelectionStart;
                    int len = rtfBox.SelectionLength;

                    for (int i = 0; i < len; i++)
                    {
                        rtfTmp.Select(i, 1);
                        rtfTmp.SelectionFont = fontModifier(rtfTmp.SelectionFont);
                    }

                    rtfTmp.SelectAll();
                    rtfBox.SelectedRtf = rtfTmp.SelectedRtf;
                    rtfBox.Select(start, len);
                }
            }
            else
            {
                var style = GetCurrentFontStyle();

                var font = new Font(_fontSelector.GetSelectedFontFamily(), _sizeSelector.GetSelectedSize(), style);
                rtfBox.SelectionFont = font;
            }
            SetModify();
        }

        private FontStyle GetCurrentFontStyle()
        {
            var style = FontStyle.Regular;

            if (tbBold.Checked) style |= FontStyle.Bold;
            if (tbItalic.Checked) style |= FontStyle.Italic;
            if (tbUnderline.Checked) style |= FontStyle.Underline;
            if (tbStrikeout.Checked) style |= FontStyle.Strikeout;

            return style;
        }

        private void SetCurrentAlignment()
        {
            if (tbAlignLeft.Checked)
            {
                rtfBox.SelectionAlignment = HorizontalAlignment.Left;
            }
            else if (tbAlignCenter.Checked)
            {
                rtfBox.SelectionAlignment = HorizontalAlignment.Center;
            }
            else if (tbAlignRight.Checked)
            {
                rtfBox.SelectionAlignment = HorizontalAlignment.Right;
            }
        }

        private void SetToolbarBySelection()
        {
            var font = rtfBox.SelectionFont;
            if (font != null)
            {
                tbBold.Checked = font.Bold;
                tbItalic.Checked = font.Italic;
                tbUnderline.Checked = font.Underline;
                tbStrikeout.Checked = font.Strikeout;

                _sizeSelector.TrySelectSize(Convert.ToInt32(font.Size));
                _fontSelector.TrySelectFontFamily(rtfBox.SelectionFont.FontFamily);
            }
            else
            {
                _sizeSelector.TrySelectSize(-1);
                _fontSelector.TrySelectFontFamily((string)null);
            }

            SetButtonAlignmentState(rtfBox.SelectionAlignment);

            _colorSelector.TrySelectColor(rtfBox.SelectionColor);
        }

        private void SetSelectionByToolbar()
        {
            SetCurrentColor();
            SetCurrentFont(f => f);
            SetCurrentAlignment();
        }

        private void SetButtonAlignmentState(HorizontalAlignment alignment)
        {
            tbAlignLeft.Checked = alignment == HorizontalAlignment.Left;
            tbAlignCenter.Checked = alignment == HorizontalAlignment.Center;
            tbAlignRight.Checked = alignment == HorizontalAlignment.Right;
        }

        private bool CanSerializeFontSettings()
        {
            if (cmbFont.SelectedIndex == -1) return false;
            if (cmbSize.SelectedIndex == -1) return false;
            if (cmbColor.SelectedIndex == -1) return false;

            return true;
        }

        private string SerializeFontSettings()
        {
            char sep = '|';
            StringBuilder sb = new StringBuilder();

            sb.Append(_fontSelector.GetSelectedFontFamily().Name);
            sb.Append(sep);
            sb.Append(_sizeSelector.GetSelectedSize());
            sb.Append(sep);
            sb.Append(_colorSelector.GetSelectedColor().Name);
            sb.Append(sep);

            if (tbBold.Checked) { sb.Append("B"); }
            if (tbItalic.Checked) { sb.Append("I"); }
            if (tbUnderline.Checked) { sb.Append("U"); }
            if (tbStrikeout.Checked) { sb.Append("S"); }

            sb.Append(sep);

            if (tbAlignLeft.Checked) { sb.Append("L"); }
            else if (tbAlignCenter.Checked) { sb.Append("C"); }
            else if (tbAlignRight.Checked) { sb.Append("R"); }

            return sb.ToString();
        }

        private bool TryRestoreSavedSettings()
        {
            //string settings = Nastaveni.Instance().DefaultFont;
            //if (string.IsNullOrEmpty(settings)) return false;

            //string[] parts = settings.Split('|');

            //try
            //{
            //    _fontSelector.TrySelectFontFamily(parts[0]);
            //    _sizeSelector.TrySelectSize(Convert.ToInt32(parts[1]));
            //    _colorSelector.TrySelectColor(Color.FromName(parts[2]));

            //    tbBold.Checked = parts[3].Contains("B");
            //    tbItalic.Checked = parts[3].Contains("I");
            //    tbUnderline.Checked = parts[3].Contains("U");
            //    tbStrikeout.Checked = parts[3].Contains("S");

            //    if (parts[4] == "L") { SetButtonAlignmentState(HorizontalAlignment.Left); }
            //    else if (parts[4] == "C") { SetButtonAlignmentState(HorizontalAlignment.Center); }
            //    else if (parts[4] == "R") { SetButtonAlignmentState(HorizontalAlignment.Right); }

            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
            return false;
        }

        #endregion

        #region Menu button handlers

        private void tbOpen_Click(object sender, EventArgs e)
        {
            OpenRtfText();
        }

        private void tbSave_Click(object sender, EventArgs e)
        {
            SaveRtfText();
        }

        private void tbCut_Click(object sender, EventArgs e)
        {
            rtfBox.Cut();
            SetModify();
        }

        private void tbCopy_Click(object sender, EventArgs e)
        {
            rtfBox.Copy();
        }

        private void tbPaste_Click(object sender, EventArgs e)
        {
            DataFormats.Format rtfFormat = DataFormats.GetFormat(DataFormats.Rtf);
            if (rtfBox.CanPaste(rtfFormat))
            {
                rtfBox.Paste(rtfFormat);
                SetModify();
            }
        }

        private void tbAlignLeft_Click(object sender, EventArgs e)
        {
            SetAlignment(HorizontalAlignment.Left);
        }

        private void tbAlignCenter_Click(object sender, EventArgs e)
        {
            SetAlignment(HorizontalAlignment.Center);
        }

        private void tbAlignRight_Click(object sender, EventArgs e)
        {
            SetAlignment(HorizontalAlignment.Right);
        }

        private void ChangeFontStyle_Click(object sender, EventArgs e)
        {
            ToolStripButton button = (ToolStripButton)sender;

            if (TrySetCurrentFont(GetChangeFunc(button)))
            {
                rtfBox.Focus();
            }
        }

        private Func<Font, Font> GetChangeFunc(ToolStripButton button)
        {
            FontStyle style = (FontStyle)button.Tag;

            if (button.Checked)
            {
                return f => new Font(f, f.Style | style);
            }
            else
            {
                return f => new Font(f, f.Style & ~style);
            }
        }

        private void cmbFont_DropDownClosed(object sender, EventArgs e)
        {
            var family = _fontSelector.GetSelectedFontFamily();

            if (TrySetCurrentFont(f => new Font(family, f.Size, f.Style)))
            {
                rtfBox.Focus();
            }
        }

        private void cmbSize_DropDownClosed(object sender, EventArgs e)
        {
            var size = _sizeSelector.GetSelectedSize();

            if (TrySetCurrentFont(f => new Font(f.FontFamily, size, f.Style)))
            {
                rtfBox.Focus();
            }
        }

        private void cmbColor_DropDownClosed(object sender, EventArgs e)
        {
            if (TrySetCurrentColor())
            {
                rtfBox.Focus();
            }
        }

        private void tbSaveDefaults_Click(object sender, EventArgs e)
        {
            if (CanSerializeFontSettings())
            {
                //var nastaveni = Nastaveni.Instance();
                //nastaveni.DefaultFont = SerializeFontSettings();
                //nastaveni.Save();
            }
        }

        private void tbRestoreDefaults_Click(object sender, EventArgs e)
        {
            if (TryRestoreSavedSettings())
            {
                SetSelectionByToolbar();
            }
        }

        #endregion

        private void rtfBox_SelectionChanged(object sender, EventArgs e)
        {
            SetToolbarBySelection();
            tbSaveDefaults.Enabled = CanSerializeFontSettings();
        }

        private void rtfBox_Click(object sender, EventArgs e)
        {
            _rtfBoxHadFocus = true;
        }
    }
}