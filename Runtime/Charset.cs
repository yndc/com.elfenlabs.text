using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;

namespace Elfenlabs.Text
{
    [Serializable]
    public struct UnicodeRange
    {
        [HexInt(digits = 4)]
        public int Start;
        [HexInt(digits = 4)]
        public int End;

        public UnicodeRange(int start, int end)
        {
            Start = start;
            End = end;
        }
    }

    public class CharacterSetBuilder
    {
        HashSet<int> Codes;

        public CharacterSetBuilder()
        {
            Codes = new HashSet<int>(1024);
        }

        public void Add(UnicodeRange range)
        {
            for (int i = range.Start; i <= range.End; i++)
            {
                Codes.Add(i);
            }
        }

        public void Add(string sample)
        {
            foreach (var c in sample)
            {
                Codes.Add(c);
            }
        }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            AddLigatures(builder);

            foreach (var character in Codes)
            {
                builder.Append((char)character);
            }

            return builder.ToString();
        }

        void AddLigatures(StringBuilder stringBuilder)
        {
            // Character ligatures
            stringBuilder.Append("fi");
            stringBuilder.Append("fl");
            stringBuilder.Append("ff");
            stringBuilder.Append("ffi");
            stringBuilder.Append("ffl");
            stringBuilder.Append("ft");
            stringBuilder.Append("st");
            stringBuilder.Append("ct");
            stringBuilder.Append("sp");
            stringBuilder.Append("Th");
            stringBuilder.Append("Qu");
            stringBuilder.Append("ch");
            stringBuilder.Append("ck");
            stringBuilder.Append("ll");
            stringBuilder.Append("ss");
            stringBuilder.Append("tt");
            stringBuilder.Append("mm");
            stringBuilder.Append("nn");
            stringBuilder.Append("pp");
            stringBuilder.Append("rr");
            stringBuilder.Append("gg");
            stringBuilder.Append("bb");
            stringBuilder.Append("dd");
            stringBuilder.Append("ww");
            stringBuilder.Append("vv");
            stringBuilder.Append("yy");
            stringBuilder.Append("oo");
            stringBuilder.Append("ee");
            stringBuilder.Append("aa");
            stringBuilder.Append("uu");
            stringBuilder.Append("ii");

            // Symbolic ligatures
            stringBuilder.Append("<=");
            stringBuilder.Append("==");
            stringBuilder.Append(">=");
            stringBuilder.Append("!=");
            stringBuilder.Append("->");
            stringBuilder.Append("<-");
            stringBuilder.Append("=>");
            stringBuilder.Append("->>");
            stringBuilder.Append("<<-");
        }
    }
}