﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Nikse.SubtitleEdit.Controls.Adapters;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Logic;
using MessageBox = Nikse.SubtitleEdit.Forms.SeMsgBox.MessageBox;

namespace Nikse.SubtitleEdit.Forms.Options
{
    public sealed partial class DoNotBreakAfterListEdit : Form
    {
        public class DoNotBreakDictionary
        {
            public string Text { get; set; }
            public string FileName { get; set; }

            public DoNotBreakDictionary(string text, string fileName)
            {
                Text = text;
                FileName = fileName;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        private List<DoNotBreakDictionary> _list;
        private List<NoBreakAfterItem> _noBreakAfterList = new List<NoBreakAfterItem>();

        public DoNotBreakAfterListEdit()
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);

            Text = LanguageSettings.Current.Settings.UseDoNotBreakAfterList;
            labelLanguage.Text = LanguageSettings.Current.ChooseLanguage.Language;
            buttonNew.Text = LanguageSettings.Current.ExportCustomText.New;
            buttonRemoveNoBreakAfter.Text = LanguageSettings.Current.DvdSubRip.Remove;
            buttonAddNoBreakAfter.Text = LanguageSettings.Current.DvdSubRip.Add;
            radioButtonText.Text = LanguageSettings.Current.General.Text;
            radioButtonRegEx.Text = LanguageSettings.Current.MultipleReplace.RegularExpression;
            buttonOK.Text = LanguageSettings.Current.General.Ok;
            buttonCancel.Text = LanguageSettings.Current.General.Cancel;

            radioButtonRegEx.Left = radioButtonText.Left + radioButtonText.Width + 10;
            InitLanguages(string.Empty);
        }

        private void InitLanguages(string selectTwoLetterCode)
        {
            var selectedText = string.Empty;
            var files = Directory.GetFiles(Configuration.DictionariesDirectory, "*_NoBreakAfterList.xml");
            var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            _list = new List<DoNotBreakDictionary>();
            foreach (var fileName in files)
            {
                try
                {
                    var s = Path.GetFileName(fileName);
                    var languageId = s.Substring(0, s.IndexOf('_'));
                    var ci = cultures.FirstOrDefault(p => p.TwoLetterISOLanguageName == languageId);
                    if (ci == null)
                    {
                        ci = CultureInfo.GetCultureInfoByIetfLanguageTag(languageId);
                    }

                    _list.Add(new DoNotBreakDictionary(ci.EnglishName + " (" + ci.NativeName.CapitalizeFirstLetter() + ")", fileName));

                    if (!string.IsNullOrEmpty(selectTwoLetterCode))
                    {
                        if (ci.TwoLetterISOLanguageName == selectTwoLetterCode)
                        {
                            selectedText = _list[_list.Count - 1].Text;
                        }
                    }
                    else if ((Configuration.Settings.WordLists.LastLanguage ?? "en-US").StartsWith(languageId, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedText = _list[_list.Count - 1].Text;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            _list = _list.OrderBy(p => p.Text).ToList();

            comboBoxDictionaries.Items.Clear();
            comboBoxDictionaries.Items.AddItems(_list.Select(p=>p.Text));
            comboBoxDictionaries.Text = selectedText;
            if (comboBoxDictionaries.Items.Count > 0 && comboBoxDictionaries.SelectedIndex < 0)
            {
                comboBoxDictionaries.Text = _list.FirstOrDefault(p => p.Text.Contains("English"))?.Text;
                if (comboBoxDictionaries.Items.Count > 0 && comboBoxDictionaries.SelectedIndex < 0)
                {
                    comboBoxDictionaries.SelectedIndex = 0;
                }
            }
        }

        private void DoNotBreakAfterListEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void comboBoxDictionaries_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = comboBoxDictionaries.SelectedIndex;
            if (idx >= 0)
            {
                _noBreakAfterList = new List<NoBreakAfterItem>();
                var doc = new XmlDocument();
                doc.Load(_list[idx].FileName);
                foreach (XmlNode node in doc.DocumentElement.SelectNodes("Item"))
                {
                    if (!string.IsNullOrEmpty(node.InnerText))
                    {
                        if (node.Attributes["RegEx"] != null && node.Attributes["RegEx"].InnerText.Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            var r = new Regex(node.InnerText, RegexOptions.Compiled);
                            _noBreakAfterList.Add(new NoBreakAfterItem(r, node.InnerText));
                        }
                        else
                        {
                            _noBreakAfterList.Add(new NoBreakAfterItem(node.InnerText));
                        }
                    }
                }
                _noBreakAfterList.Sort();
                ShowBreakAfterList(_noBreakAfterList);
            }
        }

        private void ShowBreakAfterList(List<NoBreakAfterItem> noBreakAfterList)
        {
            listBoxNoBreakAfter.Items.Clear();
            foreach (var item in noBreakAfterList)
            {
                if (item.Text != null)
                {
                    listBoxNoBreakAfter.Items.Add(item);
                }
            }
        }

        private void buttonRemoveNameEtc_Click(object sender, EventArgs e)
        {
            int first = 0;
            var list = new List<int>();
            foreach (int i in listBoxNoBreakAfter.SelectedIndices)
            {
                list.Add(i);
            }

            if (list.Count > 0)
            {
                first = list[0];
            }

            list.Sort();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                _noBreakAfterList.RemoveAt(list[i]);
            }
            ShowBreakAfterList(_noBreakAfterList);
            if (first >= _noBreakAfterList.Count)
            {
                first = _noBreakAfterList.Count - 1;
            }

            if (first >= 0)
            {
                listBoxNoBreakAfter.SelectedIndex = first;
            }
            comboBoxDictionaries.Enabled = false;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Utilities.ResetNoBreakAfterList();
            int idx = comboBoxDictionaries.SelectedIndex;
            if (idx >= 0)
            {
                var doc = new XmlDocument();
                doc.LoadXml("<NoBreakAfterList />");
                foreach (NoBreakAfterItem item in _noBreakAfterList)
                {
                    XmlNode node = doc.CreateElement("Item");
                    node.InnerText = item.Text;
                    if (item.Regex != null)
                    {
                        XmlAttribute attribute = doc.CreateAttribute("RegEx");
                        attribute.InnerText = true.ToString();
                        node.Attributes.Append(attribute);
                    }
                    doc.DocumentElement.AppendChild(node);
                }
                doc.Save(_list[idx].FileName);
            }
            DialogResult = DialogResult.OK;
        }

        private void buttonAddNames_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxNoBreakAfter.Text))
            {
                return;
            }

            NoBreakAfterItem item;
            if (radioButtonText.Checked)
            {
                item = new NoBreakAfterItem(textBoxNoBreakAfter.Text);
            }
            else
            {
                if (!RegexUtils.IsValidRegex(textBoxNoBreakAfter.Text))
                {
                    MessageBox.Show(LanguageSettings.Current.General.RegularExpressionIsNotValid);
                    return;
                }
                item = new NoBreakAfterItem(new Regex(textBoxNoBreakAfter.Text), textBoxNoBreakAfter.Text);
            }

            foreach (NoBreakAfterItem nbai in _noBreakAfterList)
            {
                if ((nbai.Regex == null && item.Regex == null || nbai.Regex != null && item.Regex != null) && nbai.Text == item.Text)
                {
                    MessageBox.Show("Text already defined");
                    textBoxNoBreakAfter.Focus();
                    textBoxNoBreakAfter.SelectAll();
                    return;
                }
            }
            _noBreakAfterList.Add(item);
            comboBoxDictionaries.Enabled = false;
            ShowBreakAfterList(_noBreakAfterList);
            for (int i = 0; i < listBoxNoBreakAfter.Items.Count; i++)
            {
                if (listBoxNoBreakAfter.Items[i].ToString() == item.Text)
                {
                    listBoxNoBreakAfter.SelectedIndex = i;
                    return;
                }
            }
            textBoxNoBreakAfter.Text = string.Empty;
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            textBoxNoBreakAfter.ContextMenuStrip = null;
            if (sender == radioButtonRegEx && radioButtonRegEx.Checked)
            {
                textBoxNoBreakAfter.ContextMenuStrip = FindReplaceDialogHelper.GetRegExContextMenu(new NativeTextBoxAdapter(textBoxNoBreakAfter));
            }
        }

        private void listBoxNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = listBoxNoBreakAfter.SelectedIndex;
            if (idx >= 0 && idx < _noBreakAfterList.Count)
            {
                NoBreakAfterItem item = _noBreakAfterList[idx];
                textBoxNoBreakAfter.Text = item.Text;
                bool isRegEx = item.Regex != null;
                radioButtonRegEx.Checked = isRegEx;
                radioButtonText.Checked = !isRegEx;
            }
        }

        private void textBoxNoBreakAfter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonAddNames_Click(sender, e);
            }
        }

        private void buttonNew_Click(object sender, EventArgs e)
        {
            using (var form = new DoNotBreakAfterListNew())
            {
                var dr = form.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                var fileName = Path.Combine(Configuration.DictionariesDirectory, form.ChosenLanguage.TwoLetterISOLanguageName + "_NoBreakAfterList.xml");
                if (!File.Exists(fileName))
                {
                    File.WriteAllText(fileName, "<NoBreakAfterList><Item>Dr</Item><Item>Dr.</Item><Item>Mr.</Item><Item>Mrs.</Item><Item>Ms.</Item></NoBreakAfterList>");
                }

                InitLanguages(form.ChosenLanguage.TwoLetterISOLanguageName);
            }
        }
    }
}
