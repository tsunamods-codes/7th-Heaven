using Iros._7th.Workshop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace Iros._7th.Workshop.ConfigSettings
{
    public class FFNxConfigManager
    {
        private string _pathToFFNxToml = Sys.PathToFFNxToml;

        private string _pathToFFNxTomlBak = Sys.PathToFFNxToml + ".bak";

        private TomlTable _toml = null;

        public FFNxConfigManager()
        {
            Reload();
        }

        public void Reload()
        {
            if (File.Exists(_pathToFFNxToml)) _toml = Toml.Parse(File.ReadAllBytes(_pathToFFNxToml)).ToModel();
        }

        public string Get(string key)
        {
            return (string)_toml[key];
        }

        public void Set(string key, string value)
        {
            switch (_toml[key])
            {
                case string s:
                    {
                        _toml[key] = value;
                        break;
                    }
                case bool b:
                    {
                        _toml[key] = bool.Parse(value);
                        break;
                    }
                case double d:
                    {
                        CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                        ci.NumberFormat.NumberDecimalSeparator = ".";

                        _toml[key] = double.Parse(value, NumberStyles.Any, ci);
                        break;
                    }
                case Int64 i:
                    {
                        _toml[key] = Int64.Parse(value);
                        break;
                    }
            }
        }

        public bool IsSetWithValue(string key, string value)
        {
            switch (_toml[key])
            {
                case string s:
                    {
                        if (s != value) return false;
                        break;
                    }
                case bool b:
                    {
                        if (b != bool.Parse(value)) return false;
                        break;
                    }
                case double d:
                    {
                        CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                        ci.NumberFormat.CurrencyDecimalSeparator = ".";

                        if (d != double.Parse(value, NumberStyles.Any, ci)) return false;
                        break;
                    }
                case Int64 i:
                    {
                        if (i != Int64.Parse(value)) return false;
                        break;
                    }
            }

            return true;
        }

        public bool HasKey(string key)
        {
            return _toml.ContainsKey(key);
        }

        public void Save()
        {
            List<string> _read = File.ReadAllLines(_pathToFFNxToml).ToList();
            List<string> _write = new List<string>();

            foreach (string line in _read)
            {
                string[] parts = line.Split(new[] { "=" }, 2, StringSplitOptions.None);
                if (!line.StartsWith("#") && parts.Length == 2)
                {
                    string settingName = parts[0].Trim();

                    if (_toml.ContainsKey(settingName))
                    {
                        dynamic node = _toml[settingName];

                        switch (node)
                        {
                            case bool b:
                                {
                                    _write.Add($"{settingName} = { b.ToString().ToLower() }");
                                    break;
                                }
                            case string s:
                                {
                                    _write.Add($"{settingName} = \"{ s.ToLower() }\"");
                                    break;
                                }
                            case double d:
                                {
                                    CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                                    ci.NumberFormat.NumberDecimalSeparator = ".";

                                    _write.Add($"{settingName} = {((double)node).ToString("0.0", ci)}");
                                    break;
                                }
                            case Int64 i:
                                {
                                    if (settingName == "devtools_hotkey")
                                    {
                                        _write.Add($"{settingName} = 0x{i.ToString("X")}");
                                    }
                                    else
                                    {
                                        _write.Add($"{settingName} = {i.ToString()}");
                                    }
                                    break;
                                }
                            case TomlArray ta:
                                {
                                    if (ta.Count > 0)
                                    {
                                        if (node[0].GetType() == typeof(string))
                                        {
                                            _write.Add($"{settingName} = [\"{ string.Join("\", \"", ta.Select(w => w.ToString())) }\"]");
                                        }
                                        else
                                        {
                                            _write.Add($"{settingName} = [{ string.Join(", ", ta.Select(w => w.ToString())) }]");
                                        }
                                    }
                                    else
                                    {
                                        _write.Add($"{settingName} = []");
                                    }
                                    break;
                                }
                            default:
                                {
                                    _write.Add($"{settingName} = {node.ToString()}");
                                    break;
                                }
                        }
                    }
                    else
                    {
                        _write.Add(line);
                    }
                }
                else
                {
                    _write.Add(line);
                }
            }

            File.WriteAllLines(_pathToFFNxToml, _write);
        }

        public void OverrideInternalKeys()
        {
            // Override known internal keys on save to preserve mod behavior override logic
            _toml["external_sfx_path"] = "sfx";
            _toml["external_sfx_ext"] = "ogg";
            _toml["external_music_path"] = "music/vgmstream";
            _toml["external_music_ext"] = "ogg";
            _toml["external_voice_path"] = "voice";
            _toml["external_voice_ext"] = "ogg";
            _toml["external_ambient_path"] = "ambient";
            _toml["external_ambient_ext"] = "ogg";
            _toml["ffmpeg_video_ext"] = "avi";
            _toml["mod_path"] = "mods/Textures";
            _toml["direct_mode_path"] = "direct";
        }

        public void ResetTo7thHeavenDefaults()
        {
            ConfigSpec gameDriverUISpec = Util.Deserialize<Iros._7th.Workshop.ConfigSettings.ConfigSpec>(Sys.PathToGameDriverUiXml());

            foreach (Setting item in gameDriverUISpec.Settings)
            {
                string[] parts = item.DefaultValue.Split(',');
                foreach (string p in parts)
                {
                    string trimmedParts = p.Trim();

                    string[] set = trimmedParts.Split('=');
                    if (set.Length == 2)
                    {
                        string trimmedName = set[0].Trim();
                        string trimmedVal = set[1].Trim();

                        Set(trimmedName, trimmedVal);
                    }
                }
            }
        }

        public void Backup()
        {
            if (File.Exists(_pathToFFNxTomlBak)) File.Delete(_pathToFFNxTomlBak);

            File.Copy(_pathToFFNxToml, _pathToFFNxTomlBak);
        }

        public void RestoreBackup()
        {
            if (File.Exists(_pathToFFNxTomlBak))
            {
                File.Delete(_pathToFFNxToml);
                File.Move(_pathToFFNxTomlBak, _pathToFFNxToml);
                Reload();
            }
        }
    }
}
