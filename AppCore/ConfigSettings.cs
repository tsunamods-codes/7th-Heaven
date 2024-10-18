/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Iros.Workshop.ConfigSettings {

    public class Settings {
        public string Get(string setting) {
            return Sys.FFNxConfig.Get(setting);
        }
        public bool IsMatched(string spec) {
            string[] parts = spec.Split(',');

            foreach (string p in parts) {
                string trimmedParts = p.Trim();

                string[] set = trimmedParts.Split('=');
                if (set.Length == 2) {
                    string trimmedName = set[0].Trim();
                    string trimmedVal = set[1].Trim();

                    if (!Sys.FFNxConfig.IsSetWithValue(trimmedName, trimmedVal))
                        return false;
                }
            }

            return true;
        }
        public void Apply(string spec) {
            if (String.IsNullOrWhiteSpace(spec)) return;
            string[] parts = spec.Split(',');
            foreach (string p in parts) {
                string trimmedParts = p.Trim();
                string[] set = trimmedParts.Split('=');

                if (set.Length == 2) {
                    string trimmedName = set[0].Trim();
                    string trimmedVal = set[1].Trim();

                    Sys.FFNxConfig.Set(trimmedName, trimmedVal);
                }
            }
        }

        public bool HasSetting(string spec)
        {
            List<bool> exists = new List<bool>();

            string[] parts = spec.Split(',');
            foreach (string p in parts)
            {
                string trimmedParts = p.Trim();

                string[] set = trimmedParts.Split('=');
                if (set.Length == 2)
                {
                    string trimmedName = set[0].Trim();
                    string trimmedVal = set[1].Trim();

                    exists.Add(Sys.FFNxConfig.HasKey(trimmedName));
                }
            }

            return exists.Count > 0 && exists.All(s => s);
        }

        /// <summary>
        /// Adds any missing default settings
        /// </summary>
        public void SetMissingDefaults(List<Setting> settings)
        {
            foreach (Setting item in settings)
            {
                if (!HasSetting(item.DefaultValue))
                {
                    Apply(item.DefaultValue);
                }
            }
        }

        public void Save()
        {
            Sys.FFNxConfig.OverrideInternalKeys();
            Sys.FFNxConfig.Save();
        }
    }

    public class ConfigSpec {
        [XmlElement("Setting")]
        public List<Setting> Settings { get; set; }
    }

    [XmlInclude(typeof(DropDown)), XmlInclude(typeof(Checkbox)), XmlInclude(typeof(TextEntry))]
    public abstract class Setting {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Group { get; set; }

        public string DefaultValue { get; set; }

        public virtual void Load(System.Windows.Forms.Control container, Settings settings) {
            var lbl = new System.Windows.Forms.Label() { Text = Name };
            container.Controls.Add(lbl);
            lbl.Location = new System.Drawing.Point(10, 15);
            var ldesc = new System.Windows.Forms.Label() { Text = Description, AutoSize = true, MaximumSize = new System.Drawing.Size(200, 0) };
            container.Controls.Add(ldesc);
            ldesc.Location = new System.Drawing.Point(container.Width - 210, 10);
            ldesc.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        }

        public virtual void Save(Settings settings) {

        }
    }

    public class TextEntry : Setting {
        private System.Windows.Forms.TextBox _tb;

        public string Option { get; set; }

        [XmlElement("Suggest")]
        public List<string> Suggestions { get; set; }

        public override void Load(System.Windows.Forms.Control container, Settings settings) {
            base.Load(container, settings);
            _tb = new System.Windows.Forms.TextBox() { Text = settings.Get(Option) };
            _tb.Location = new System.Drawing.Point(150, 10);
            _tb.Width = 150;
            if (Suggestions.Any()) {
                _tb.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
                _tb.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
                _tb.AutoCompleteCustomSource = new System.Windows.Forms.AutoCompleteStringCollection();
                _tb.AutoCompleteCustomSource.AddRange(Suggestions.ToArray());
            }
            container.Controls.Add(_tb);
        }

        public override void Save(Settings settings) {
            base.Save(settings);
            settings.Apply(Option + "=" + _tb.Text);
        }
    }

    public class DDOption {
        public string Text { get; set; }
        public string Settings { get; set; }

        public override string ToString() {
            return Text;
        }
    }
    public class DropDown : Setting {
        private System.Windows.Forms.ComboBox _cb;

        [XmlElement("Option")]
        public List<DDOption> Options { get; set; }

        public override void Load(System.Windows.Forms.Control container, Settings settings) {
            base.Load(container, settings);
            _cb = new System.Windows.Forms.ComboBox() { DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            _cb.Width = 150;
            _cb.Name = Name;
            foreach (var ddo in Options) {
                _cb.Items.Add(ddo);
                if (settings.IsMatched(ddo.Settings))
                    _cb.SelectedIndex = _cb.Items.Count - 1;
            }
            container.Controls.Add(_cb);
            _cb.Location = new System.Drawing.Point(150, 10);
        }

        public override void Save(Settings settings) {
            base.Save(settings);
            if (_cb.SelectedIndex >= 0) {
                DDOption ddo = _cb.SelectedItem as DDOption;
                settings.Apply(ddo.Settings);
            }
        }
    }

    public class Checkbox : Setting {
        private System.Windows.Forms.CheckBox _cb;

        public string TrueSetting { get; set; }
        public string FalseSetting { get; set; }

        public Checkbox() {
        }

        public override void Load(System.Windows.Forms.Control container, Settings settings) {
            base.Load(container, settings);
            container.Controls.RemoveAt(0); //don't want label
            _cb = new System.Windows.Forms.CheckBox() { Text = Name };
            _cb.Checked = settings.IsMatched(TrueSetting);
            _cb.Location = new System.Drawing.Point(150, 15);
            container.Controls.Add(_cb);
        }

        public override void Save(Settings settings) {
            base.Save(settings);
            settings.Apply(_cb.Checked ? TrueSetting : FalseSetting);
        }
    }
}
