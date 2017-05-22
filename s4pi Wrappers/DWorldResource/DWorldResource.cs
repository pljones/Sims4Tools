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

namespace DWorldResource
{
    public class DWorldResource : AResource
    {
        static bool checking = s4pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        #region Attributes
        private TLVListChunk objectManager;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        public DWorldResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }
        #endregion

        #region Data I/O
        void Parse(Stream s) { objectManager = (TLVListChunk)TagLengthValue.TagLengthValueFactory(requestedApiVersion, OnResourceChanged, s, new String[] { "OMGS" }); }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();

            objectManager.UnParse(ms);

            ms.Flush();
            return ms;
        }
        #endregion

        #region Sub-types
        public abstract class TagLengthValue : AHandlerElement, IEquatable<TagLengthValue>
        {
            #region Attributes
            protected UInt32 tag;
            #endregion

            #region Constructors
            public TagLengthValue(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler) { this.tag = tag; }
            #endregion

            #region Data I/O
            public static TagLengthValue TagLengthValueFactory(int apiVersion, EventHandler handler, Stream s, IEnumerable<String> _validTags = null)
            {
                //List<uint> validTags = _validTags == null ? null : new List<uint>( _validTags.Select<String,uint>( _s => (uint)FOURCC(_s) ) );
                BinaryReader r = new BinaryReader(s);
                UInt32 tag = r.ReadUInt32();
                if (checking) if (_validTags != null && !_validTags.Contains(FOURCC(tag)))
                        throw new InvalidDataException(String.Format("Invalid Tag read: '{0}'; expected one of: ('{1}'); at 0x{2:X8}", FOURCC(tag), String.Join("', '", _validTags), s.Position));

                UInt32 length = r.ReadUInt32();

                TagLengthValue chunk = null;

                long pos = s.Position;
                switch (FOURCC(tag))
                {
                    case "OMGS": chunk = new TLVListChunk(apiVersion, handler, tag, new [] {
                        "OMGR"
                    }, s, length); break;
                    case "OMGR": chunk = new TLVListChunk(apiVersion, handler, tag, new[] {
                        "ID  ", "LOT ", "OBJ ", "REFS"
                    }, s, length); break;
                    case "ID  ": chunk = new UInt64Chunk(apiVersion, handler, tag, s); break;
                    case "LOT ": chunk = new TLVListChunk(apiVersion, handler, tag, new[] {
                        "SIZX", "SIZZ", "POS ", "ROT "
                    }, s, length); break;
                    case "OBJ ": chunk = new TLVListChunk(apiVersion, handler, tag, new [] {
                        "ID  ", "POS ", "ROT ", "MODL", "LEVL", "SCAL", "SCRP", "TRES", "DEFG", "DGUD", "MLOD", "PTID", "SLOT"
                    }, s, length); break;
                    case "REFS": chunk = new UnusedChunk(apiVersion, handler, tag, s, length); break;
                    case "SIZX": chunk = new UInt32Chunk(apiVersion, handler, tag, s); break;
                    case "SIZZ": chunk = new UInt32Chunk(apiVersion, handler, tag, s); break;
                    case "POS ": chunk = new VertexChunk(apiVersion, handler, tag, s); break;
                    case "ROT ": 
                        if (length == sizeof(Single))
                            chunk = new SingleChunk(apiVersion, handler, tag, s);
                        else if (length == 4 * sizeof(Single))
                            chunk = new QuaternionChunk(apiVersion, handler, tag, s);
                        else
                            throw new InvalidDataException(String.Format("'ROT ' with unknown length: '0x{0:X8}'; at 0x{1:X8}", length, s.Position));
                        break;
                    case "MODL": chunk = new TGIBlockChunk(apiVersion, handler, tag, s); break;
                    case "LEVL": chunk = new Int32Chunk(apiVersion, handler, tag, s); break;
                    case "SCAL": chunk = new SingleChunk(apiVersion, handler, tag, s); break;
                    case "SCRP": chunk = new StringChunk(apiVersion, handler, tag, s, length); break;
                    case "TRES": chunk = new UnusedChunk(apiVersion, handler, tag, s, length); break;
                    case "DEFG": chunk = new UInt32Chunk(apiVersion, handler, tag, s); break;
                    case "DGUD": chunk = new UInt64Chunk(apiVersion, handler, tag, s); break;
                    case "MLOD": chunk = new ByteChunk(apiVersion, handler, tag, s); break;
                    case "PTID": chunk = new UInt64Chunk(apiVersion, handler, tag, s); break;
                    case "SLOT": chunk = new UInt32Chunk(apiVersion, handler, tag, s); break;
                    default:
                        if (checking)
                            throw new InvalidDataException(String.Format("Unknown Tag read: '{0}'; at 0x{1:X8}", FOURCC(tag), s.Position));
                        s.Position += length;
                        break;
                }
                if (checking) if (s.Position != pos + length)
                        throw new InvalidDataException(String.Format("Invalid chunk data length: 0x{0:X8} bytes read; 0x{1:X8} bytes expected; at 0x{2:X8}", s.Position - pos, length, s.Position));

                return chunk;
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                w.Write(FOURCC(tag));

                long pos = s.Position;
                w.Write((UInt32)0);

                this.UnParse(s);

                long newPos = s.Position;
                s.Seek(pos, SeekOrigin.Begin);
                w.Write((UInt32)(newPos - pos));
                s.Seek(newPos, SeekOrigin.Begin);
            }
            #endregion

            #region Sub-types
            public delegate TagLengthValue CreateElementMethod(Stream s);
            public delegate void WriteElementMethod(Stream s, TagLengthValue value);
            #endregion

            #region IEquatable<TagLengthValue> Members
            public abstract bool Equals(TagLengthValue other);
            public override bool Equals(object obj) { return obj as TagLengthValue != null ? this.Equals(obj as TagLengthValue) : false; }
            #endregion

            #region AApiVersionedFields
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class TLVList : DependentList<TagLengthValue>
        {
            #region Attributes
            UInt32 expectedLength;
            IEnumerable<String> validTags;
            #endregion

            #region Constructors
            public TLVList(EventHandler handler, UInt32 expectedLength = 0, IEnumerable<String> validTags = null)
                : base(handler)
            {
                this.expectedLength = expectedLength;
                this.validTags = validTags;
            }
            public TLVList(EventHandler handler, Stream s, UInt32 expectedLength, IEnumerable<String> validTags = null)
                : this(null, expectedLength, validTags)
            {
                elementHandler = handler;
                Parse(s);
                this.handler = handler;
            }
            public TLVList(EventHandler handler, IEnumerable<TagLengthValue> collection, IEnumerable<String> validTags = null)
                : this(null, 0, validTags)
            {
                elementHandler = handler;
                this.AddRange(collection);
                this.handler = handler;
            }
            #endregion

            #region Data I/O
            protected override void Parse(Stream s)
            {
                long maxPos = s.Position + expectedLength;
                this.Clear();
                while (s.Position < maxPos)
                    base.Add(TagLengthValue.TagLengthValueFactory(0, handler, s, validTags));
            }
            public override void UnParse(Stream s) { foreach (var element in this) element.UnParse(s); }

            protected override TagLengthValue CreateElement(Stream s) { throw new InvalidOperationException(); }
            protected override void WriteElement(Stream s, TagLengthValue element) { throw new InvalidOperationException(); }
            #endregion
        }

        public class TLVListChunk : TagLengthValue
        {
            #region Attributes
            IEnumerable<String> validTags;
            private TLVList tlvList;
            #endregion

            #region Constructors
            public TLVListChunk(int apiVersion, EventHandler handler, UInt32 tag, IEnumerable<String> validTags) : base(apiVersion, handler, tag) { this.validTags = validTags; this.tlvList = new TLVList(handler, 0, validTags); }
            public TLVListChunk(int apiVersion, EventHandler handler, UInt32 tag, IEnumerable<String> validTags, Stream s, UInt32 expectedLength) : this(apiVersion, handler, tag, validTags) { Parse(s, expectedLength); }
            public TLVListChunk(int apiVersion, EventHandler handler, UInt32 tag, IEnumerable<String> validTags, TLVListChunk basis) : base(apiVersion, handler, tag) { tlvList = new TLVList(handler, basis.tlvList, validTags); }
            #endregion

            #region Data I/O
            private void Parse(Stream s, UInt32 expectedLength) { tlvList = new TLVList(handler, s, expectedLength, validTags); }
            public void UnParse(Stream s) { tlvList.UnParse(s); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(TLVListChunk other) { return tag.Equals(other.tag) && tlvList.Equals(other.tlvList); }
            public override bool Equals(TagLengthValue obj) { return obj as TLVListChunk != null ? this.Equals(obj as TLVListChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public TLVList Chunks { get { return tlvList; } set { if (!tlvList.Equals(value)) { tlvList = new TLVList(handler, value, validTags); OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class UnusedChunk : TagLengthValue
        {
            #region Attributes
            private byte[] data;
            #endregion

            #region Constructors
            public UnusedChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public UnusedChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s, UInt32 expectedLength) : this(apiVersion, handler, tag) { Parse(s, expectedLength); }
            #endregion

            #region Data I/O
            private void Parse(Stream s, UInt32 expectedLength)
            {
                data = new byte[expectedLength];
                s.Read(data, 0, (int)expectedLength);
            }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(UnusedChunk other) { return tag.Equals(other.tag) && data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as UnusedChunk != null ? this.Equals(obj as UnusedChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public virtual BinaryReader Data
            {
                get { MemoryStream ms = new MemoryStream(); UnParse(ms); return new BinaryReader(ms); }
                set
                {
                    if (value.BaseStream.CanSeek) { value.BaseStream.Position = 0; Parse(value.BaseStream, (UInt32)value.BaseStream.Length); }
                    else
                    {
                        MemoryStream ms = new MemoryStream();
                        byte[] buffer = new byte[1024 * 1024];
                        for (int read = value.BaseStream.Read(buffer, 0, buffer.Length); read > 0; read = value.BaseStream.Read(buffer, 0, buffer.Length))
                            ms.Write(buffer, 0, read);
                        ms.Flush();
                        ms.Position = 0;
                        Parse(ms, (UInt32)ms.Length);
                    }
                    OnElementChanged();
                }
            }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class UInt64Chunk : TagLengthValue
        {
            #region Attributes
            UInt64 data = 0;
            #endregion

            #region Constructors
            public UInt64Chunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public UInt64Chunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new BinaryReader(s).ReadUInt64(); }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(UInt64Chunk other) { return tag.Equals(other.tag) && data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as UInt64Chunk != null ? this.Equals(obj as UInt64Chunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public UInt64 Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class UInt32Chunk : TagLengthValue
        {
            #region Attributes
            UInt32 data = 0;
            #endregion

            #region Constructors
            public UInt32Chunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public UInt32Chunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new BinaryReader(s).ReadUInt32(); }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(UInt32Chunk other) { return data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as UInt32Chunk != null ? this.Equals(obj as UInt32Chunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public UInt32 Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class ByteChunk : TagLengthValue
        {
            #region Attributes
            Byte data = 0;
            #endregion

            #region Constructors
            public ByteChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public ByteChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new BinaryReader(s).ReadByte(); }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(ByteChunk other) { return data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as ByteChunk != null ? this.Equals(obj as ByteChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public Byte Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class Int32Chunk : TagLengthValue
        {
            #region Attributes
            Int32 data = 0;
            #endregion

            #region Constructors
            public Int32Chunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public Int32Chunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new BinaryReader(s).ReadInt32(); }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(Int32Chunk other) { return data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as Int32Chunk != null ? this.Equals(obj as Int32Chunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public Int32 Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class SingleChunk : TagLengthValue
        {
            #region Attributes
            Single data = 0;
            #endregion

            #region Constructors
            public SingleChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public SingleChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new BinaryReader(s).ReadSingle(); }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(SingleChunk other) { return data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as SingleChunk != null ? this.Equals(obj as SingleChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public Single Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class VertexChunk : TagLengthValue
        {
            #region Attributes
            Vertex data;
            #endregion

            #region Constructors
            public VertexChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public VertexChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new Vertex(requestedApiVersion, handler, s); }
            public void UnParse(Stream s) { data.UnParse(s); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(VertexChunk other) { return tag.Equals(other.tag) && data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as VertexChunk != null ? this.Equals(obj as VertexChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public Vertex Data { get { return data; } set { if (!data.Equals(value)) { data = new Vertex(requestedApiVersion, handler, value); OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class QuaternionChunk : TagLengthValue
        {
            #region Attributes
            Quaternion data;
            #endregion

            #region Constructors
            public QuaternionChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public QuaternionChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new Quaternion(requestedApiVersion, handler, s); }
            public void UnParse(Stream s) { data.UnParse(s); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(QuaternionChunk other) { return tag.Equals(other.tag) && data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as QuaternionChunk != null ? this.Equals(obj as QuaternionChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public Quaternion Data { get { return data; } set { if (!data.Equals(value)) { data = new Quaternion(requestedApiVersion, handler, value); OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class TGIBlockChunk : TagLengthValue
        {
            #region Attributes
            TGIBlock data;
            #endregion

            #region Constructors
            public TGIBlockChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public TGIBlockChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s) : this(apiVersion, handler, tag) { Parse(s); }
            #endregion

            #region Data I/O
            private void Parse(Stream s) { data = new TGIBlock(requestedApiVersion, handler, s); }
            public void UnParse(Stream s) { data.UnParse(s); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(TGIBlockChunk other) { return tag.Equals(other.tag) && data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as TGIBlockChunk != null ? this.Equals(obj as TGIBlockChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public TGIBlock Data { get { return data; } set { if (!data.Equals(value)) { data = new TGIBlock(requestedApiVersion, handler, value); OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }

        public class StringChunk : TagLengthValue
        {
            #region Attributes
            private String data;
            #endregion

            #region Constructors
            public StringChunk(int apiVersion, EventHandler handler, UInt32 tag) : base(apiVersion, handler, tag) { }
            public StringChunk(int apiVersion, EventHandler handler, UInt32 tag, Stream s, UInt32 expectedLength) : this(apiVersion, handler, tag) { Parse(s, expectedLength); }
            #endregion

            #region Data I/O
            private void Parse(Stream s, UInt32 expectedLength) { data = new String(new BinaryReader(s).ReadBytes((int)expectedLength).Select(x => (char)x).ToArray()); }
            public void UnParse(Stream s) { new BinaryWriter(s).Write(data.ToCharArray().Select(x => (byte)x).ToArray()); }
            #endregion

            #region IEquatable<TagLengthValue> Members
            public bool Equals(StringChunk other) { return tag.Equals(other.tag) && data.Equals(other.data); }
            public override bool Equals(TagLengthValue obj) { return obj as StringChunk != null ? this.Equals(obj as StringChunk) : false; }
            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public String Data { get { return data; } set { if (data != value) { data = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return this.ValueBuilder; } }
        }
        
        #endregion

        #region AApiVersionedFields
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region Content Fields
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [ElementPriority(1)]
        public TLVListChunk ObjectManager { get { return objectManager; } set { if (!objectManager.Equals(value)) { objectManager = new TLVListChunk(requestedApiVersion, OnResourceChanged, (uint)FOURCC("OMGS"), new[] { "OMGR" }, value); OnResourceChanged(this, EventArgs.Empty); } } }
        #endregion

        public string Value { get { return this.ValueBuilder; } }
    }

    public class DWorldResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary.
        /// </summary>
        public DWorldResourceHandler()
        {
            this.Add(typeof(DWorldResource), new List<string>(new string[] { "0x810A102D", }));
        }
    }
}
