/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AppWrapper {
    public static class Lzs {
        private const int N = 4096;
        private const int F = 18;
        private const int THRESHOLD = 2;
        private const int NIL = N;

        public static void Encode(Stream input, Stream output) {
            new EncodeContext().Encode(input, output);
        }
        public static void Decode(Stream input, Stream output) {
            new EncodeContext().Decode(input, output);
        }

        private class EncodeContext {
            public byte[] buffer = new byte[N + F];
            public int MatchPos, MatchLen;
            public int[] Lson = new int[N + 1];
            public int[] Rson = new int[N + 257];
            public int[] Dad = new int[N + 1];

            public void InitTree() {
                for (int i = N + 1; i <= N + 256; i++) Rson[i] = NIL;
                for (int i = 0; i < N; i++) Dad[i] = NIL;
            }

            public void InsertNode(int r) {
                int i, p, cmp;
                int key = r;
                cmp = 1;
                p = N + 1 + buffer[key];
                Rson[r] = Lson[r] = NIL;
                MatchLen = 0;

                while (true) {
                    if (cmp >= 0) {
                        if (Rson[p] != NIL)
                            p = Rson[p];
                        else {
                            Rson[p] = r;
                            Dad[r] = p;
                            return;
                        }
                    } else {
                        if (Lson[p] != NIL)
                            p = Lson[p];
                        else {
                            Lson[p] = r;
                            Dad[r] = p;
                            return;
                        }
                    }
                    for (i = 1; i < F; i++)
                        if ((cmp = buffer[key + i] - buffer[p + i]) != 0) break;
                    if (i > MatchLen) {
                        MatchPos = p;
                        if ((MatchLen = i) >= F) break;
                    }
                }
                Dad[r] = Dad[p]; Lson[r] = Lson[p]; Rson[r] = Rson[p];
                Dad[Lson[p]] = r; Dad[Rson[p]] = r;
                if (Rson[Dad[p]] == p)
                    Rson[Dad[p]] = r;
                else
                    Lson[Dad[p]] = r;
                Dad[p] = NIL;
            }

            public void DeleteNode(int p) {
                int q;
                if (Dad[p] == NIL) return;
                if (Rson[p] == NIL)
                    q = Lson[p];
                else if (Lson[p] == NIL)
                    q = Rson[p];
                else {
                    q = Lson[p];
                    if (Rson[q] != NIL) {
                        do {
                            q = Rson[q];
                        } while (Rson[q] != NIL);
                        Rson[Dad[q]] = Lson[q]; Dad[Lson[q]] = Dad[q];
                        Lson[q] = Lson[p]; Dad[Lson[p]] = q;
                    }
                    Rson[q] = Rson[p]; Dad[Rson[p]] = q;
                }
                Dad[q] = Dad[p];
                if (Rson[Dad[p]] == p)
                    Rson[Dad[p]] = q;
                else
                    Lson[Dad[p]] = q;
                Dad[p] = NIL;
            }


            public void Encode(Stream input, Stream output) /* was Encode(void) */
            {
                int i, c, len, r, s, last_match_length, code_buf_ptr;
                byte[] code_buf = new byte[17];
                byte mask;
                InitTree();  /* initialize trees */
                code_buf[0] = 0;  /* code_buf[1..16] saves eight units of code, and
		code_buf[0] works as eight flags, "1" representing that the unit
		is an unencoded letter (1 byte), "0" a position-and-length pair
		(2 bytes).  Thus, eight units require at most 16 bytes of code. */
                code_buf_ptr = mask = 1;
                s = 0; r = N - F;
                for (i = s; i < r; i++) buffer[i] = 0;
                for (len = 0; len < F && (c = input.ReadByte()) != -1; len++)
                    buffer[r + len] = (byte)c;  /* Read F bytes into the last F bytes of
			the buffer */
                if (len == 0) return;  /* text of size zero */
                for (i = 1; i <= F; i++) InsertNode(r - i);  /* Insert the F strings,
		each of which begins with one or more 'space' characters.  Note
		the order in which these strings are inserted.  This way,
		degenerate trees will be less likely to occur. */
                InsertNode(r);  /* Finally, insert the whole string just read.  The
		global variables match_length and match_position are set. */
                do {
                    if (MatchLen > len) MatchLen = len;  /* match_length
			may be spuriously long near the end of text. */
                    if (MatchLen <= THRESHOLD) {
                        MatchLen = 1;  /* Not long enough match.  Send one byte. */
                        code_buf[0] |= mask;  /* 'send one byte' flag */
                        code_buf[code_buf_ptr++] = buffer[r];  /* Send uncoded. */
                    } else {
                        code_buf[code_buf_ptr++] = (byte)MatchPos;
                        code_buf[code_buf_ptr++] = (byte)(((MatchPos >> 4) & 0xf0) | (MatchLen - (THRESHOLD + 1)));  /* Send position and
					length pair. Note match_length > THRESHOLD. */
                    }
                    if ((mask <<= 1) == 0) {  /* Shift mask left one bit. */
                        for (i = 0; i < code_buf_ptr; i++)  /* Send at most 8 units of */
                            output.WriteByte(code_buf[i]);     /* code together */
                        code_buf[0] = 0; code_buf_ptr = mask = 1;
                    }
                    last_match_length = MatchLen;
                    for (i = 0; i < last_match_length &&
                            (c = input.ReadByte()) != -1; i++) {
                        DeleteNode(s);		/* Delete old strings and */
                        buffer[s] = (byte)c;	/* read new bytes */
                        if (s < F - 1) buffer[s + N] = (byte)c;  /* If the position is
				near the end of buffer, extend the buffer to make
				string comparison easier. */
                        s = (s + 1) & (N - 1); r = (r + 1) & (N - 1);
                        /* Since this is a ring buffer, increment the position
                           modulo N. */
                        InsertNode(r);	/* Register the string in text_buf[r..r+F-1] */
                    }
                    while (i++ < last_match_length) {	/* After the end of text, */
                        DeleteNode(s);					/* no need to read, but */
                        s = (s + 1) & (N - 1); r = (r + 1) & (N - 1);
                        if ((--len) != 0) InsertNode(r);		/* buffer may not be empty. */
                    }
                } while (len > 0);	/* until length of string to be processed is zero */
                if (code_buf_ptr > 1) {		/* Send remaining code. */
                    for (i = 0; i < code_buf_ptr; i++) output.WriteByte(code_buf[i]);
                }
                return;
            }


            public void Decode(Stream input, Stream output) /* was Decode(void)
   Just the reverse of Encode(). */
            {
                int i, j, k, r, c;
                int flags;

                for (i = 0; i < N - F; i++) buffer[i] = 0;
                r = N - F; flags = 0;
                for (; ; ) {
                    if (((flags >>= 1) & 256) == 0) {
                        if ((c = input.ReadByte()) == -1) break;
                        flags = c | 0xff00;		/* uses higher byte cleverly */
                    }							/* to count eight */
                    if ((flags & 1) != 0) {
                        if ((c = input.ReadByte()) == -1) break;
                        output.WriteByte((byte)c); buffer[r++] = (byte)c; r &= (N - 1);
                    } else {
                        if ((i = input.ReadByte()) == -1) break;
                        if ((j = input.ReadByte()) == -1) break;
                        i |= ((j & 0xf0) << 4); j = (j & 0x0f) + THRESHOLD;
                        for (k = 0; k <= j; k++) {
                            c = buffer[(i + k) & (N - 1)];
                            output.WriteByte((byte)c); buffer[r++] = (byte)c; r &= (N - 1);
                        }
                    }
                }
                return;
            }
        }
    }
}
