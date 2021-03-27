using System;
using UnityEngine;
using System.Text.RegularExpressions;

namespace KSTS
{


    class GUIRichValueSelector
    {
        private string name = "";
        private double value;
        public double Value { get { return this.value; } set { this.textValue = value.ToString(); if (this.TryParseTextValue()) this.UpdateTextField(); } }
        private double minValue = 0;
        private double maxValue = 0;
        private string unit = "";
        private double lastValue = 0;
        private string textValue = "";
        private string valueFormat = "";
        private bool showMinMax = true;

        private GUIStyle validFieldStyle;
        private GUIStyle invalidFieldStyle;

        public GUIRichValueSelector(string name, double value, string unit, double minValue, double maxValue, bool showMinMax, string valueFormat)
        {
            this.name = name;
            this.value = value;
            this.lastValue = value;
            this.unit = unit;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.showMinMax = showMinMax;
            this.valueFormat = valueFormat;
            this.textValue = this.value.ToString(this.valueFormat) + this.unit;

            this.validFieldStyle = new GUIStyle(GUI.textFieldStyle) { alignment = TextAnchor.MiddleRight, stretchWidth = false, fixedWidth = 320 };
            this.invalidFieldStyle = new GUIStyle(this.validFieldStyle);
            this.invalidFieldStyle.normal.textColor = Color.red;
            this.invalidFieldStyle.focused.textColor = Color.red;
        }

        protected bool TryParseTextValue()
        {
            double parsedValue;
            var text = this.textValue.Replace(",", "");
            text = Regex.Replace(text, this.unit + "$", "").Trim();
            if (!Double.TryParse(text, out parsedValue)) return false;
            if (parsedValue > this.maxValue) return false;
            if (parsedValue < this.minValue) return false;
            this.value = parsedValue;
            return true;
        }

        private void UpdateTextField()
        {
            this.textValue = this.value.ToString(this.valueFormat) + this.unit;
        }

        public double Display()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(this.name + ":", new GUIStyle(GUI.labelStyle) { stretchWidth = true });
            this.textValue = GUILayout.TextField(this.textValue, this.TryParseTextValue() ? this.validFieldStyle : this.invalidFieldStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (this.showMinMax) GUILayout.Label(this.minValue.ToString(valueFormat) + this.unit, new GUIStyle(GUI.labelStyle) { stretchWidth = false, alignment = TextAnchor.MiddleLeft });
            this.value = GUILayout.HorizontalSlider((float)this.value, (float)this.minValue, (float)this.maxValue);
            if (this.showMinMax) GUILayout.Label(this.maxValue.ToString(valueFormat) + this.unit, new GUIStyle(GUI.labelStyle) { stretchWidth = false, alignment = TextAnchor.MiddleRight });
            GUILayout.EndHorizontal();

            if (this.value != this.lastValue)
            {
                this.lastValue = this.value;
                UpdateTextField();
            }

            return this.value;
        }
    }
}
