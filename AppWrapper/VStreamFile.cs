/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using Iros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppWrapper {
    class VStreamFile {
        private byte[] _source;
        private long _position;

        public VStreamFile(byte[] source) {
            _source = source;
        }

        public long SetPosition(long pos, Win32.EMoveMethod type) {
            switch (type) {
                case Win32.EMoveMethod.Begin:
                    _position = pos;
                    break;
                case Win32.EMoveMethod.Current:
                    _position += pos;
                    break;
                case Win32.EMoveMethod.End:
                    _position = _source.Length + pos;
                    break;
            }
            //DebugLogger.DetailedWriteLine("Moving VStream position to {_position}");
            return _position;
        }

        public int Read(IntPtr buffer, uint toRead, ref uint read) {
            int willRead = Math.Min((int)toRead, _source.Length - (int)_position);
            if (willRead == 0) {
                read = 0; return 1;
            }
            Util.CopyToIntPtr(_source, buffer, willRead, (int)_position);
            _position += willRead;
            //DebugLogger.DetailedWriteLine("VStream reading {willRead} bytes, new position {_position}");
            read = (uint)willRead;
            return 1;
        }
    }
}
