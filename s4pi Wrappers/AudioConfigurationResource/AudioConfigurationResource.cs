/***************************************************************************
 *  Copyright (C) 2017 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s4pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s4pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using s4pi.Interfaces;
using System.Diagnostics;
using System.Collections;

namespace AudioConfigurationResource
{
    public class AudioConfigurationResource : AResource
    {
        static bool checking = s4pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        const Int32 kNumPolyphonyLevels = 3;

        #region Attributes
        private UInt16 version;
        private UInt64 parentKey;
        private UInt64List samples;
        private GlobalConditionList globalConditions;
        private InteractiveMusicEntryList iMusic;
        private HashEntry64FloatDictionary moodPitchChange;
        private HashEntry64FloatDictionary moodFreqPeriods;
        private HashEntry64FloatDictionary moodFreqOffsets;
        private HashEntry64FloatDictionary moodAttenuations;
        private HashEntry64BoolDictionary moodPlays;
        private HashEntry32FloatDictionary vuMeterFacialOverlayModsOffset;
        private HashEntry32FloatDictionary vuMeterFacialOverlayModsScale;
        private Single gain;
        private Single attenuation;
        private Byte aggregateGain;
        private PolyphonyDiffersFromBase polyphonyDiffersFromBase;
        private ConfigurationEntryList properties;
        private HashEntry64UInt32Dictionary samplesWeights;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        public AudioConfigurationResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }
        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader br = new BinaryReader(s);

            version = br.ReadUInt16() ;
            if (checking) if (version != 1 && version != 2)
                    throw new InvalidDataException(String.Format("Unexpected version read; expected 1 or 2; read '{0}'", version));

            parentKey = br.ReadUInt64() ;
            samples = new UInt64List(OnResourceChanged, s);
            globalConditions = new GlobalConditionList(OnResourceChanged, s);
            iMusic = new InteractiveMusicEntryList(OnResourceChanged, s);
            moodPitchChange = new HashEntry64FloatDictionary(OnResourceChanged, s);
            moodFreqPeriods = new HashEntry64FloatDictionary(OnResourceChanged, s);
            moodFreqOffsets = new HashEntry64FloatDictionary(OnResourceChanged, s);
            moodAttenuations = new HashEntry64FloatDictionary(OnResourceChanged, s);
            moodPlays = new HashEntry64BoolDictionary(OnResourceChanged, s);
            vuMeterFacialOverlayModsOffset = new HashEntry32FloatDictionary(OnResourceChanged, s);
            vuMeterFacialOverlayModsScale = new HashEntry32FloatDictionary(OnResourceChanged, s);
            gain = new Single();
            attenuation = new Single();
            aggregateGain = new Byte();
            polyphonyDiffersFromBase = new PolyphonyDiffersFromBase(0, OnResourceChanged, s);
            properties = new ConfigurationEntryList(OnResourceChanged, s);
            samplesWeights = (version == 1) ? null : new HashEntry64UInt32Dictionary(OnResourceChanged, s);
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(version);
            bw.Write(parentKey);

            if (samples == null) samples = new UInt64List(OnResourceChanged);
            samples.UnParse(ms);
            if (globalConditions == null) globalConditions = new GlobalConditionList(OnResourceChanged);
            globalConditions.UnParse(ms);
            if (iMusic == null) iMusic = new InteractiveMusicEntryList(OnResourceChanged);
            iMusic.UnParse(ms);
            moodPitchChange.UnParse(ms);
            moodFreqPeriods.UnParse(ms);
            moodFreqOffsets.UnParse(ms);
            moodAttenuations.UnParse(ms);
            moodPlays.UnParse(ms);
            vuMeterFacialOverlayModsOffset.UnParse(ms);
            vuMeterFacialOverlayModsScale.UnParse(ms);
            bw.Write(gain);
            bw.Write(attenuation);
            bw.Write(aggregateGain);
            polyphonyDiffersFromBase.UnParse(ms);
            properties.UnParse(ms);
            if (version >= 2)
            {
                samplesWeights.UnParse(ms);
            }

            ms.Flush();
            return ms;
        }
        #endregion

        #region Sub-types
        public class HashEntry64FloatDictionary : SimpleDictionary<UInt64, Single>
        {
            public HashEntry64FloatDictionary(EventHandler handler) : base(handler, createValue, writeValue) { }
            public HashEntry64FloatDictionary(EventHandler handler, SimpleDictionary<UInt64, Single> dictionary) : base(handler, dictionary, createValue, writeValue) { }
            public HashEntry64FloatDictionary(EventHandler handler, Stream s) : base(handler, s, createValue, writeValue) { }

            protected override ulong CreateKey(Stream s) { return new BinaryReader(s).ReadUInt64(); }
            private static float createValue(Stream s) { return new BinaryReader(s).ReadSingle(); }

            protected override void WriteKey(Stream s, ulong key) { new BinaryWriter(s).Write(key); }
            private static void writeValue(Stream s, float value) { new BinaryWriter(s).Write(value); }
        }

        public class HashEntry32FloatDictionary : SimpleDictionary<UInt32, Single>
        {
            public HashEntry32FloatDictionary(EventHandler handler) : base(handler, createValue, writeValue) { }
            public HashEntry32FloatDictionary(EventHandler handler, SimpleDictionary<UInt32, Single> dictionary) : base(handler, dictionary, createValue, writeValue) { }
            public HashEntry32FloatDictionary(EventHandler handler, Stream s) : base(handler, s, createValue, writeValue) { }

            protected override uint CreateKey(Stream s) { return new BinaryReader(s).ReadUInt32(); }
            private static float createValue(Stream s) { return new BinaryReader(s).ReadSingle(); }

            protected override void WriteKey(Stream s, uint key) { new BinaryWriter(s).Write(key); }
            private static void writeValue(Stream s, float value) { new BinaryWriter(s).Write(value); }
        }

        public class HashEntry64BoolDictionary : SimpleDictionary<UInt64, Boolean>
        {
            public HashEntry64BoolDictionary(EventHandler handler) : base(handler, createValue, writeValue) { }
            public HashEntry64BoolDictionary(EventHandler handler, SimpleDictionary<UInt64, Boolean> dictionary) : base(handler, dictionary, createValue, writeValue) { }
            public HashEntry64BoolDictionary(EventHandler handler, Stream s) : base(handler, s, createValue, writeValue) { }

            protected override ulong CreateKey(Stream s) { return new BinaryReader(s).ReadUInt64(); }
            private static bool createValue(Stream s) { return new BinaryReader(s).ReadByte() != 0; }

            protected override void WriteKey(Stream s, ulong key) { new BinaryWriter(s).Write(key); }
            private static void writeValue(Stream s, bool value) { new BinaryWriter(s).Write(value ? (Byte)1 : (Byte)0); }
        }

        public class HashEntry64UInt32Dictionary : SimpleDictionary<UInt64, UInt32>
        {
            public HashEntry64UInt32Dictionary(EventHandler handler) : base(handler, createValue, writeValue) { }
            public HashEntry64UInt32Dictionary(EventHandler handler, SimpleDictionary<UInt64, UInt32> dictionary) : base(handler, dictionary, createValue, writeValue) { }
            public HashEntry64UInt32Dictionary(EventHandler handler, Stream s) : base(handler, s, createValue, writeValue) { }

            protected override ulong CreateKey(Stream s) { return new BinaryReader(s).ReadUInt64(); }
            private static uint createValue(Stream s) { return new BinaryReader(s).ReadUInt32(); }

            protected override void WriteKey(Stream s, ulong key) { new BinaryWriter(s).Write(key); }
            private static void writeValue(Stream s, uint value) { new BinaryWriter(s).Write(value); }
        }

        public enum PropertyValueType : uint
        {
            Float = 1,
            UInt32,
            UInt64,
            Bool,
            String,
            Curve,
            LongSet
        }

        public class ResponseCurveData : AHandlerElement, IEquatable<ResponseCurveData>
        {
            #region Attributes
            private Single inputValue;
            private Single outputValue;
            private Single previousInputValue;
            private Single previousOutputValue;
            private Single deltaRatio;
            #endregion

            #region Constructors
            public ResponseCurveData(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0f, 0f, 0f, 0f, 0f) { }
            public ResponseCurveData(int apiVersion, EventHandler handler, ResponseCurveData basis) : this(apiVersion, handler, basis.inputValue, basis.outputValue, basis.previousInputValue, basis.previousOutputValue, basis.deltaRatio) { }
            public ResponseCurveData(int apiVersion, EventHandler handler,
                Single inputValue, Single outputValue, Single previousInputValue, Single previousOutputValue, Single deltaRatio)
                : base(apiVersion, handler)
            {
                this.inputValue = inputValue;
                this.outputValue = outputValue;
                this.previousInputValue = previousInputValue;
                this.previousOutputValue = previousOutputValue;
                this.deltaRatio = deltaRatio;
            }
            public ResponseCurveData(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                this.inputValue = r.ReadSingle();
                this.outputValue = r.ReadSingle();
                this.previousInputValue = r.ReadSingle();
                this.previousOutputValue = r.ReadSingle();
                this.deltaRatio = r.ReadSingle();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                w.Write(inputValue);
                w.Write(outputValue);
                w.Write(previousInputValue);
                w.Write(previousOutputValue);
                w.Write(deltaRatio);
            }
            #endregion

            #region AHandlerElement
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<ResponseCurveData> Members

            public bool Equals(ResponseCurveData other)
            {
                return inputValue.Equals(other.inputValue)
                    && outputValue.Equals(other.outputValue)
                    && previousInputValue.Equals(other.previousInputValue)
                    && previousOutputValue.Equals(other.previousOutputValue)
                    && deltaRatio.Equals(other.deltaRatio)
                ;
            }

            public override bool Equals(object other)
            {
                return other as ResponseCurveData != null ? this.Equals(other as ResponseCurveData) : false;
            }

            public override int GetHashCode()
            {
                return inputValue.GetHashCode()
                    ^ outputValue.GetHashCode()
                    ^ previousInputValue.GetHashCode()
                    ^ previousOutputValue.GetHashCode()
                    ^ deltaRatio.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(1)]
            public Single InputValue { get { return inputValue; } set { if (inputValue != value) { inputValue = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public Single OutputValue { get { return outputValue; } set { if (outputValue != value) { outputValue = value; OnElementChanged(); } } }
            [ElementPriority(3)]
            public Single PreviousInputValue { get { return previousInputValue; } set { if (previousInputValue != value) { previousInputValue = value; OnElementChanged(); } } }
            [ElementPriority(4)]
            public Single PreviousOutputValue { get { return previousOutputValue; } set { if (previousOutputValue != value) { previousOutputValue = value; OnElementChanged(); } } }
            [ElementPriority(5)]
            public Single DeltaRatio { get { return deltaRatio; } set { if (deltaRatio != value) { deltaRatio = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }

        public abstract class ConfigurationEntry : AHandlerElement, IEquatable<ConfigurationEntry>
        {
            protected UInt32 key;
            protected PropertyValueType dataType;

            #region Constructors
            protected ConfigurationEntry(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, 0) { }
            protected ConfigurationEntry(int apiVersion, EventHandler handler, ConfigurationEntry basis) : this(apiVersion, handler, basis.key, basis.dataType) { }
            protected ConfigurationEntry(int apiVersion, EventHandler handler,
                UInt32 key, PropertyValueType dataType)
                : base(apiVersion, handler)
            {
                this.key = key;
                this.dataType = dataType;
            }
            #endregion

            #region Data I/O
            public static ConfigurationEntry ConfigurationEntryFactory(int APIversion, EventHandler handler, Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                UInt32 key = r.ReadUInt32();
                PropertyValueType dataType = (PropertyValueType)r.ReadUInt32();

                switch (dataType)
                {
                    case PropertyValueType.Float: return new CESingle(APIversion, handler, key, s);
                    case PropertyValueType.UInt32: return new CEUInt32(APIversion, handler, key, s);
                    case PropertyValueType.UInt64: return new CEUInt64(APIversion, handler, key, s);
                    case PropertyValueType.Bool: return new CEBoolean(APIversion, handler, key, s);
                    case PropertyValueType.String: return new CEString(APIversion, handler, key, s);
                    case PropertyValueType.Curve: return new CECurve(APIversion, handler, key, s);
                    case PropertyValueType.LongSet: return new CEUInt64List(APIversion, handler, key, s);
                    default:
                        throw new InvalidDataException();
                }
            }

            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                w.Write(key);
                w.Write((uint)dataType);
            }
            #endregion

            #region AHandlerElement
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<ConfigurationEntry> Members

            public bool Equals(ConfigurationEntry other)
            {
                return key.Equals(other.key)
                ;
            }

            public override bool Equals(object other)
            {
                return other as ConfigurationEntry != null ? this.Equals(other as ConfigurationEntry) : false;
            }

            public override int GetHashCode()
            {
                return key.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(1)]
            public UInt32 Key { get { return key; } set { if (!key.Equals(value)) { key = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }
        public class CESingle : ConfigurationEntry {
            private Single data;

            #region Constructors
            public CESingle(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, 0f) { }
            public CESingle(int apiVersion, EventHandler handler, CESingle basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CESingle(int apiVersion, EventHandler handler,
                UInt32 key, Single data)
                : base(apiVersion, handler, key, PropertyValueType.Float)
            {
                this.data = data;
            }
            internal CESingle(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.Float) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                this.data = r.ReadSingle();
            }

            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                base.UnParse(s);
                w.Write(data);
            }
            #endregion

            #region IEquatable<CESingle> Members

            public bool Equals(CESingle other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CESingle != null ? this.Equals(other as CESingle) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public Single Data { get { return data; } set { if (!data.Equals(value)) { data = value; OnElementChanged(); } } }
            #endregion
        }
        public class CEUInt32 : ConfigurationEntry {
            private UInt32 data;

            #region Constructors
            public CEUInt32(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, 0) { }
            public CEUInt32(int apiVersion, EventHandler handler, CEUInt32 basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CEUInt32(int apiVersion, EventHandler handler,
                UInt32 key, UInt32 data)
                : base(apiVersion, handler, key, PropertyValueType.UInt32)
            {
                this.data = data;
            }
            internal CEUInt32(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.UInt32) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                this.data = r.ReadUInt32();
            }

            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                base.UnParse(s);
                w.Write(data);
            }
            #endregion

            #region IEquatable<CEUInt32> Members

            public bool Equals(CEUInt32 other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CEUInt32 != null ? this.Equals(other as CEUInt32) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public UInt32 Data { get { return data; } set { if (!data.Equals(value)) { data = value; OnElementChanged(); } } }
            #endregion
        }
        public class CEUInt64 : ConfigurationEntry {
            private UInt64 data;

            #region Constructors
            public CEUInt64(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, 0) { }
            public CEUInt64(int apiVersion, EventHandler handler, CEUInt64 basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CEUInt64(int apiVersion, EventHandler handler,
                UInt32 key, UInt64 data)
                : base(apiVersion, handler, key, PropertyValueType.UInt64)
            {
                this.data = data;
            }
            internal CEUInt64(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.UInt64) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                this.data = r.ReadUInt32();
            }

            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                base.UnParse(s);
                w.Write(data);
            }
            #endregion

            #region IEquatable<CEUInt64> Members

            public bool Equals(CEUInt64 other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CEUInt64 != null ? this.Equals(other as CEUInt64) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public UInt64 Data { get { return data; } set { if (!data.Equals(value)) { data = value; OnElementChanged(); } } }
            #endregion
        }
        public class CEBoolean : ConfigurationEntry {
            private Boolean data;

            #region Constructors
            public CEBoolean(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, false) { }
            public CEBoolean(int apiVersion, EventHandler handler, CEBoolean basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CEBoolean(int apiVersion, EventHandler handler,
                UInt32 key, Boolean data)
                : base(apiVersion, handler, key, PropertyValueType.Bool)
            {
                this.data = data;
            }
            internal CEBoolean(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.Bool) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                this.data = r.ReadByte() != 0;
            }

            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                base.UnParse(s);
                w.Write((Byte)(data ? 1 : 0));
            }
            #endregion

            #region IEquatable<CEBoolean> Members

            public bool Equals(CEBoolean other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CEBoolean != null ? this.Equals(other as CEBoolean) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public Boolean Data { get { return data; } set { if (!data.Equals(value)) { data = value; OnElementChanged(); } } }
            #endregion
        }
        public class CEString : ConfigurationEntry {
            private String data;

            #region Constructors
            public CEString(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, "") { }
            public CEString(int apiVersion, EventHandler handler, CEString basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CEString(int apiVersion, EventHandler handler,
                UInt32 key, String data)
                : base(apiVersion, handler, key, PropertyValueType.String)
            {
                this.data = data;
            }
            internal CEString(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.String) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                int expectedLength = r.ReadInt32();
                this.data = new String(r.ReadBytes((int)expectedLength).Select(x => (char)x).ToArray());
            }

            internal virtual void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                base.UnParse(s);
                if (data == null) data = "";
                w.Write(data.Length);
                if (data.Length > 0) w.Write(data);
            }
            #endregion

            #region IEquatable<CEString> Members

            public bool Equals(CEString other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CEString != null ? this.Equals(other as CEString) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public String Data { get { return data; } set { if (!data.Equals(value)) { data = value; OnElementChanged(); } } }
            #endregion
        }
        public class CECurve : ConfigurationEntry {
            private ResponseCurveData data;

            #region Constructors
            public CECurve(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, new ResponseCurveData(apiVersion, handler)) { }
            public CECurve(int apiVersion, EventHandler handler, CECurve basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CECurve(int apiVersion, EventHandler handler,
                UInt32 key, ResponseCurveData data)
                : base(apiVersion, handler, key, PropertyValueType.Curve)
            {
                this.data = new ResponseCurveData(apiVersion, handler, data);
            }
            internal CECurve(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.Curve) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                this.data = new ResponseCurveData(requestedApiVersion, handler, s);
            }

            internal virtual void UnParse(Stream s)
            {
                base.UnParse(s);
                if (data == null) data = new ResponseCurveData(requestedApiVersion, handler);
                data.UnParse(s);
            }
            #endregion

            #region IEquatable<CECurve> Members

            public bool Equals(CECurve other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CECurve != null ? this.Equals(other as CECurve) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public ResponseCurveData Data { get { return data; } set { if (!data.Equals(value)) { data = new ResponseCurveData(requestedApiVersion, handler, value); OnElementChanged(); } } }
            #endregion
        }
        public class CEUInt64List : ConfigurationEntry {
            private UInt64List data;

            #region Constructors
            public CEUInt64List(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, new UInt64List(handler)) { }
            public CEUInt64List(int apiVersion, EventHandler handler, CEUInt64List basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public CEUInt64List(int apiVersion, EventHandler handler,
                UInt32 key, UInt64List data)
                : base(apiVersion, handler, key, PropertyValueType.LongSet)
            {
                this.data = new UInt64List(handler, data);
            }
            internal CEUInt64List(int APIversion, EventHandler handler, UInt32 key, Stream s) : base(APIversion, handler, key, PropertyValueType.LongSet) { Parse(s); }
            #endregion

            #region Data I/O
            protected virtual void Parse(Stream s)
            {
                this.data = new UInt64List(handler, s);
            }

            internal virtual void UnParse(Stream s)
            {
                base.UnParse(s);
                if (data == null) data = new UInt64List(handler);
                data.UnParse(s);
            }
            #endregion

            #region IEquatable<CEUInt64List> Members

            public bool Equals(CEUInt64List other)
            {
                return base.Equals(other) && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as CEUInt64List != null ? this.Equals(other as CEUInt64List) : false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(2)]
            public UInt64List Data { get { return data; } set { if (!data.Equals(value)) { data = new UInt64List(handler, value); OnElementChanged(); } } }
            #endregion
        }
        public class ConfigurationEntryList : DependentList<ConfigurationEntry> {
            #region Constructors
            public ConfigurationEntryList(EventHandler handler) : base(handler) { }
            public ConfigurationEntryList(EventHandler handler, Stream s) : base(handler, s) { }
            public ConfigurationEntryList(EventHandler handler, IEnumerable<ConfigurationEntry> llp) : base(handler, llp) { }
            #endregion

            #region Data I/O
            protected override ConfigurationEntry CreateElement(Stream s) { return ConfigurationEntry.ConfigurationEntryFactory(0, elementHandler, s); }
            protected override void WriteElement(Stream s, ConfigurationEntry element) { element.UnParse(s); }
            #endregion
        }

        public class GlobalCondition : AHandlerElement, IEquatable<GlobalCondition>
        {
            private ConfigurationEntryList configEntries;

            #region Constructors
            public GlobalCondition(int apiVersion, EventHandler handler) : base(apiVersion, handler) { }
            public GlobalCondition(int apiVersion, EventHandler handler, GlobalCondition basis) : this(apiVersion, handler, basis.configEntries) { }
            public GlobalCondition(int apiVersion, EventHandler handler,
                ConfigurationEntryList configEntries)
                : base(apiVersion, handler)
            {
                this.configEntries = configEntries;
            }
            public GlobalCondition(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                this.configEntries = new ConfigurationEntryList(handler, s);
            }

            internal void UnParse(Stream s)
            {
                configEntries.UnParse(s);
            }
            #endregion

            #region AHandlerElement
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<GlobalCondition> Members

            public bool Equals(GlobalCondition other)
            {
                return configEntries.Equals(other.configEntries)
                ;
            }

            public override bool Equals(object other)
            {
                return other as GlobalCondition != null ? this.Equals(other as GlobalCondition) : false;
            }

            public override int GetHashCode()
            {
                return configEntries.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(1)]
            public ConfigurationEntryList ConfigEntries { get { return configEntries; } set { if (!configEntries.Equals(value)) { configEntries = new ConfigurationEntryList(handler, value); OnElementChanged(); } } }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }
        public class GlobalConditionList : DependentList<GlobalCondition>
        {
            #region Constructors
            public GlobalConditionList(EventHandler handler) : base(handler) { }
            public GlobalConditionList(EventHandler handler, Stream s) : base(handler, s) { }
            public GlobalConditionList(EventHandler handler, IEnumerable<GlobalCondition> llp) : base(handler, llp) { }
            #endregion

            #region Data I/O
            protected override GlobalCondition CreateElement(Stream s) { return new GlobalCondition(0, elementHandler, s); }
            protected override void WriteElement(Stream s, GlobalCondition element) { element.UnParse(s); }
            #endregion
       }

        public class InteractiveMusicEntry : AHandlerElement, IEquatable<InteractiveMusicEntry>
        {
            #region Attributes
            private UInt32 key;
            private SingleList data;
            #endregion

            #region Constructors
            public InteractiveMusicEntry(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, new SingleList(handler)) { }
            public InteractiveMusicEntry(int apiVersion, EventHandler handler, InteractiveMusicEntry basis) : this(apiVersion, handler, basis.key, basis.data) { }
            public InteractiveMusicEntry(int apiVersion, EventHandler handler,
                UInt32 key, SingleList data)
                : base(apiVersion, handler)
            {
                this.key = key;
                this.data = new SingleList(handler, data);
            }
            public InteractiveMusicEntry(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                this.key = r.ReadUInt32();
                this.data = new SingleList(handler, s);
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                w.Write(key);
                data.UnParse(s);
            }
            #endregion

            #region AHandlerElement
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<InteractiveMusicEntry> Members

            public bool Equals(InteractiveMusicEntry other)
            {
                return key.Equals(other.key)
                    && data.Equals(other.data)
                ;
            }

            public override bool Equals(object other)
            {
                return other as ConfigurationEntry != null ? this.Equals(other as ConfigurationEntry) : false;
            }

            public override int GetHashCode()
            {
                return key.GetHashCode()
                    ^ data.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(1)]
            public UInt32 Key { get { return key; } set { if (!key.Equals(value)) { key = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public SingleList Data { get { return data; } set { if (!data.Equals(value)) { data = new SingleList(handler, value); OnElementChanged(); } } }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }
        public class InteractiveMusicEntryList : DependentList<InteractiveMusicEntry>
        {
            #region Constructors
            public InteractiveMusicEntryList(EventHandler handler) : base(handler) { }
            public InteractiveMusicEntryList(EventHandler handler, Stream s) : base(handler, s) { }
            public InteractiveMusicEntryList(EventHandler handler, IEnumerable<InteractiveMusicEntry> llp) : base(handler, llp) { }
            #endregion

            #region Data I/O
            protected override InteractiveMusicEntry CreateElement(Stream s) { return new InteractiveMusicEntry(0, elementHandler, s); }
            protected override void WriteElement(Stream s, InteractiveMusicEntry element) { element.UnParse(s); }
            #endregion
        }

        public class PolyphonyDiffersFromBase : AHandlerElement, IEquatable<PolyphonyDiffersFromBase>
        {
            private Byte[] polyphonyDiffersFromBase;

            #region Constructors
            public PolyphonyDiffersFromBase(int apiVersion, EventHandler handler) : base(apiVersion, handler) { }
            public PolyphonyDiffersFromBase(int apiVersion, EventHandler handler, PolyphonyDiffersFromBase basis) : this(apiVersion, handler, basis.polyphonyDiffersFromBase) { }
            public PolyphonyDiffersFromBase(int apiVersion, EventHandler handler,
                Byte[] polyphonyDiffersFromBase)
                : base(apiVersion, handler)
            {
                if (polyphonyDiffersFromBase.Length != kNumPolyphonyLevels)
                    throw new ArgumentException();
                this.polyphonyDiffersFromBase = polyphonyDiffersFromBase.ToArray();
            }
            public PolyphonyDiffersFromBase(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                polyphonyDiffersFromBase = new Byte[kNumPolyphonyLevels];
                for (int i = 0; i < polyphonyDiffersFromBase.Length; i++)
                    polyphonyDiffersFromBase[i] = r.ReadByte();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                foreach (Byte b in polyphonyDiffersFromBase)
                    w.Write(b);
            }
            #endregion

            #region AHandlerElement
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<GlobalCondition> Members

            public bool Equals(PolyphonyDiffersFromBase other)
            {
                return polyphonyDiffersFromBase.Equals(other.polyphonyDiffersFromBase)
                ;
            }

            public override bool Equals(object other)
            {
                return other as PolyphonyDiffersFromBase != null ? this.Equals(other as PolyphonyDiffersFromBase) : false;
            }

            public override int GetHashCode()
            {
                return polyphonyDiffersFromBase.GetHashCode()
                    ;
            }

            #endregion

            #region ContentFields
            [ElementPriority(1)]
            public Boolean this[int index]
            {
                get { return !polyphonyDiffersFromBase[index].Equals(0); }
                set { if (!polyphonyDiffersFromBase[index].Equals(value)) { polyphonyDiffersFromBase[index] = (Byte)(value ? 1 : 0) ; OnElementChanged(); } }
            }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }
        #endregion

        #region AApiVersionedFields
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields
        {
            get
            {
                List<string> contentFields = GetContentFields(requestedApiVersion, this.GetType());
                if (version < 2)
                {
                    contentFields.Remove("SamplesWeights");
                }
                return contentFields;
            }
        }
        #endregion

        #region Content Fields
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [ElementPriority(1)]
        public UInt16 Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(2)]
        public UInt64 ParentKey { get { return parentKey; } set { if (parentKey != value) { parentKey = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(3)]
        public UInt64List Samples { get { return samples; } set { if (!samples.Equals(value)) { samples = new UInt64List(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(4)]
        public GlobalConditionList GlobalConditions { get { return globalConditions; } set { if (!globalConditions.Equals(value)) { globalConditions = new GlobalConditionList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(5)]
        public InteractiveMusicEntryList InteractiveMusicEntries { get { return iMusic; } set { if (!iMusic.Equals(value)) { iMusic = new InteractiveMusicEntryList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(6)]
        public HashEntry64FloatDictionary MoodPitchChange { get { return moodPitchChange; } set { if (!moodPitchChange.Equals(value)) { moodPitchChange = new HashEntry64FloatDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(7)]
        public HashEntry64FloatDictionary MoodFreqPeriods { get { return moodFreqPeriods; } set { if (!moodFreqPeriods.Equals(value)) { moodFreqPeriods = new HashEntry64FloatDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(8)]
        public HashEntry64FloatDictionary MoodFreqOffsets { get { return moodFreqOffsets; } set { if (!moodFreqOffsets.Equals(value)) { moodFreqOffsets = new HashEntry64FloatDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(9)]
        public HashEntry64FloatDictionary MoodAttenuations { get { return moodAttenuations; } set { if (!moodAttenuations.Equals(value)) { moodAttenuations = new HashEntry64FloatDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(10)]
        public HashEntry64BoolDictionary MoodPlays { get { return moodPlays; } set { if (!moodPlays.Equals(value)) { moodPlays = new HashEntry64BoolDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(11)]
        public HashEntry32FloatDictionary VuMeterFacialOverlayModsOffset { get { return vuMeterFacialOverlayModsOffset; } set { if (!vuMeterFacialOverlayModsOffset.Equals(value)) { vuMeterFacialOverlayModsOffset = new HashEntry32FloatDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(12)]
        public HashEntry32FloatDictionary VuMeterFacialOverlayModsScale { get { return vuMeterFacialOverlayModsScale; } set { if (!vuMeterFacialOverlayModsScale.Equals(value)) { vuMeterFacialOverlayModsScale = new HashEntry32FloatDictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(13)]
        public Single Gain { get { return gain; } set { if (gain != value) { gain = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(14)]
        public Single Attenuation { get { return attenuation; } set { if (attenuation != value) { attenuation = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(15)]
        public Byte AggregateGain { get { return aggregateGain; } set { if (aggregateGain != value) { aggregateGain = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(16)]
        public PolyphonyDiffersFromBase PolyphonyDiffers { get { return polyphonyDiffersFromBase; } set { if (!polyphonyDiffersFromBase.Equals(value)) { polyphonyDiffersFromBase = new PolyphonyDiffersFromBase(requestedApiVersion, OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(19)]
        public ConfigurationEntryList Properties { get { return properties; } set { if (!properties.Equals(value)) { properties = new ConfigurationEntryList(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(20)]
        public HashEntry64UInt32Dictionary SamplesWeights { get { return samplesWeights; } set { if (!samplesWeights.Equals(value)) { samplesWeights = new HashEntry64UInt32Dictionary(OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        #endregion

        public string Value { get { return this.ValueBuilder; } }
    }

    public class PropertyResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary.
        /// </summary>
        public PropertyResourceHandler()
        {
            this.Add(typeof(AudioConfigurationResource), new List<string>(new string[] { "0xFD04E3BE", }));
        }
    }
}
