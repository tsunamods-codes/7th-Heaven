/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using Iros;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AppWrapper {

    static class FFText {
        private static char[] _map = {
            ' ','!','"','#','$','%','&','\'','(',')','*','+',',','-','.','/',
            '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
            '@','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z','[','\\',']','^','_',
            '`','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
            'p','q','r','s','t','u','v','w','x','y','z','{','|','}','~', ' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',
            ' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ',' ', (char)0,
                                     };

        public static string Translate(byte[] input) {
            string s = new string(input.Select(b => _map[b]).ToArray());
            if (s.IndexOf((char)0) >= 0) s = s.Substring(0, s.IndexOf((char)0));
            return s;
        }

    }

    public enum VarType {
        Byte,
        Short,
        Int,
        FFString,
        Sys,
        Counter,
        CounterAdv,
        Random,
        CounterRnd,
        RandomVarOnce,
        RandomVar
    }

    class RandomVar
    {
        public RandomVar(int randomValue)
        {
            value = randomValue;
            lastGeneratedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            locked = true;
        }

        public int value { get; set; }
        public bool locked { get; set; }
        public long lastGeneratedTimestamp { get; set; }
    }

    static class RuntimeVar {

        private static Dictionary<string, Func<int>> _sys = new Dictionary<string, Func<int>>(StringComparer.InvariantCultureIgnoreCase);
        private static Dictionary<string, int> _counters = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        private static Dictionary<string, RandomVar> _vars = new Dictionary<string, RandomVar>(StringComparer.InvariantCultureIgnoreCase);
        private static Random _r = new Random();

        static RuntimeVar() {
            _sys["Day"] = () => DateTime.Now.Day;
            _sys["Month"] = () => DateTime.Now.Month;
            _sys["Year"] = () => DateTime.Now.Year;
            _sys["Hour"] = () => DateTime.Now.Hour;
            _sys["Minute"] = () => DateTime.Now.Minute;
            _sys["Second"] = () => DateTime.Now.Second;
        }


        public static long Parse(string input) {
            long L;
            if (input.StartsWith("0x"))
                L = long.Parse(input.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
            else
                L = long.Parse(input);
            return L;
        }

        private static Func<bool> GetSys(string which, int size, string value, string spec) {
            return CompareInt(_sys[which], value, spec);
        }

        private static Func<bool, bool> GetUnlockRandomVarWrapper(string runtimeVarSpec)
        {
            return isActive => {
                if (isActive) {
                    string[] parts = runtimeVarSpec.Split(':');
                    if (Enum.TryParse<VarType>(parts[0], true, out VarType type)) {
                        if (type == VarType.RandomVar) {
                            if (_vars.TryGetValue(parts[1], out RandomVar randomVar))
                                randomVar.locked = false;
                        }
                    }
                }
                return isActive;
            };
        }

        private static Func<bool> CompareInt(Func<int> reader, string value, string spec) {
            Func<int, bool> compare;
            int last = 0;
            if (value.Contains("..")) {
                int[] range = value.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries).Select(s => (int)Parse(s)).ToArray();
                compare = i => {
                    if (i != last) DebugLogger.WriteLine($"RuntimeVar {spec} became {i}");
                    last = i;
                    return i >= range[0] && i <= range[1];
                };
            } else {
                HashSet<int> values = new HashSet<int>(value.Split(',').Select(s => (int)Parse(s)));
                compare = i => {
                    if (i != last) DebugLogger.WriteLine($"RuntimeVar {spec} became {i}");
                    last = i;
                    return values.Contains(i);
                };
            }

            Func<bool, bool> unlockRandomVarWrapper = GetUnlockRandomVarWrapper(spec);
            return () => unlockRandomVarWrapper(compare(reader()));
        }

        public static Func<bool> MakeRuntimeVar(string spec, string value) {
            VarType type;
            int size = 0;

            string[] parts = spec.Split(':');
            Enum.TryParse<VarType>(parts[0], true, out type);
            if (parts.Length > 2) size = (int)Parse(parts[2]);

            switch (type) {
                case VarType.Sys:
                    return GetSys(parts[1], size, value, spec);
                case VarType.Counter:
                    return CompareInt(() => {
                        int c;
                        _counters.TryGetValue(parts[1], out c);
                        return c % size;
                    }, value, spec);
                case VarType.CounterAdv:
                    return CompareInt(() => {
                        int c;
                        _counters.TryGetValue(parts[1], out c);
                        c++;
                        _counters[parts[1]] = c;
                        return c % size;
                    }, value, spec);
                case VarType.CounterRnd:
                    return CompareInt(() => {
                        int c = _r.Next(size);
                        _counters[parts[1]] = c;
                        return c;
                    }, value, spec);
                case VarType.Random:
                    return CompareInt(() => _r.Next(size), value, spec);
                case VarType.RandomVarOnce:
                    return CompareInt(() => {
                        if (_vars.TryGetValue(parts[1], out RandomVar randomValue))
                            return randomValue.value;
                        
                        _vars[parts[1]] = new RandomVar(_r.Next(size));
                        return _vars[parts[1]].value;
                    }, value, spec);
                case VarType.RandomVar:
                    return CompareInt(() => {
                        int timeToLiveSeconds = parts.Length > 3 ? int.Parse(parts[3]) : 0;
                        if (_vars.TryGetValue(parts[1], out RandomVar randomVar)) {
                            if (!randomVar.locked && DateTimeOffset.Now.ToUnixTimeSeconds() > randomVar.lastGeneratedTimestamp + timeToLiveSeconds) {
                                randomVar.lastGeneratedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                                randomVar.value = _r.Next(size);
                            }

                            randomVar.locked = true;
                            return randomVar.value;
                        }

                        _vars[parts[1]] = new RandomVar(_r.Next(size));
                        return _vars[parts[1]].value;
                    }, value, spec);
            }

            if (type == VarType.FFString) {
                byte[] data = new byte[size];
                HashSet<string> values = new HashSet<string>(value.Split('|'), StringComparer.InvariantCultureIgnoreCase);
                string last = String.Empty;
                IntPtr address;
                address = new IntPtr(Parse(parts[1]));
                return () => {
                    Util.CopyFromIntPtr(address, data, size);
                    string s = FFText.Translate(data);
                    if (!s.Equals(last))
                        DebugLogger.WriteLine($"RuntimeVar {spec} became {s}");
                    last = s;
                    return values.Contains(s.Trim());
                };
            } else {
                IntPtr address;
                address = new IntPtr(Parse(parts[1]));
                if (size == 0) size = -1;
                switch (type) {
                    case VarType.Int:
                        return CompareInt(() => System.Runtime.InteropServices.Marshal.ReadInt32(address) & size, value, spec);
                    case VarType.Byte:
                        return CompareInt(() => System.Runtime.InteropServices.Marshal.ReadByte(address) & size, value, spec);
                    case VarType.Short:
                        return CompareInt(() => System.Runtime.InteropServices.Marshal.ReadInt16(address) & size, value, spec);
                }
            }
            throw new Exception("Bad RunTimeVar specification");
        }
    }
}
