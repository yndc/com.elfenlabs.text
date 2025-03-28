// Create the "HexInt" Attribute that can be applied to a property.
// Optionally, you can specify the number of digits to "zero pad" it.
using System;
using UnityEditor;
using UnityEngine;

namespace Elfenlabs.Text
{
    public sealed class HexIntAttribute : PropertyAttribute
    {
        public int digits;
        public string FormatString
        {
            get
            {
                if (digits == 0)
                {
                    return "X";
                }
                else
                {
                    return string.Format("X{0}", digits);
                }
            }
        }

        public HexIntAttribute(int digits)
        {
            if (digits < 0)
                throw new ArgumentOutOfRangeException("Digits cannot be negative");
            this.digits = digits;
        }

        public HexIntAttribute() : this(0)
        {
        }
    }


    // The following class should be placed in a script file at the path "Assets/Editor"

    // Here is where you create the custom PropertyDrawer. The magic happens in the
    // OnGUI method where we create a TextField in the inspector and set it's value
    // to the SerializedProperty's value as a long, read it back as a string and
    // try to parse it to a number again. If the parsing fails at any point, the
    // number is just set to 0.
    [CustomPropertyDrawer(typeof(HexIntAttribute))]
    public class HexIntDrawer : PropertyDrawer
    {
        public HexIntAttribute hexIntAttribute
        {
            get { return ((HexIntAttribute)attribute); }
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            string hexValue = EditorGUI.TextField(position, label,
                 property.longValue.ToString(hexIntAttribute.FormatString));

            long value = 0;

            if (hexValue.StartsWith("0x"))
            {
                try
                {
                    value = Convert.ToInt64(hexValue, 16);
                }
                catch (FormatException)
                {
                    value = 0;
                }
            }
            else
            {
                bool parsed = long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber,
                                            null, out value);
                if (!parsed)
                {
                    value = 0;
                }
            }

            if (EditorGUI.EndChangeCheck())
                property.longValue = value;
        }
    }
}