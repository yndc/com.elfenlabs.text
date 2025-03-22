using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;

namespace Elfenlabs.Text
{
    [Flags]
    [Serializable]
    public enum CharacterPreset
    {
        Latin = 1 << 0,
        Cyrillic = 1 << 1,
    }

    public class CharacterSetBuilder
    {
        HashSet<int> Codes;
        StringBuilder LigaturesBuilder;
        CharacterPreset Presets;

        public CharacterSetBuilder()
        {
            Codes = new HashSet<int>(1024);
            LigaturesBuilder = new StringBuilder();
        }

        public CharacterSetBuilder WithPreset(CharacterPreset preset)
        {
            Presets |= preset;
            return this;
        }

        void AddRange(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                Codes.Add(i);
            }
        }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            // Apply presets
            if ((Presets & CharacterPreset.Latin) != 0)
            {
                AddRange(0x0020, 0x007F);
                builder.Append("fi");
                builder.Append("fl");
                builder.Append("ff");
                builder.Append("ffi");
                builder.Append("ffl");
                builder.Append("ft");
                builder.Append("st");
                builder.Append("ct");
                builder.Append("sp");
                builder.Append("Th");
                builder.Append("Qu");
                builder.Append("ch");
                builder.Append("ck");
                builder.Append("ll");
                builder.Append("ss");
                builder.Append("tt");
                builder.Append("mm");
                builder.Append("nn");
                builder.Append("pp");
                builder.Append("rr");
                builder.Append("gg");
                builder.Append("bb");
                builder.Append("dd");
                builder.Append("ww");
                builder.Append("vv");
                builder.Append("yy");
                builder.Append("oo");
                builder.Append("ee");
                builder.Append("aa");
                builder.Append("uu");
                builder.Append("ii");

            }

            foreach (var glyph in Codes)
            {
                builder.Append((char)glyph);
            }

            return builder.ToString();
        }
    }
}