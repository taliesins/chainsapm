﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 14.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace CommandCreator
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "14.0.0.0")]
    public partial class CommandGeneratorCs : CommandGeneratorCsBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("/*\r\n *\r\n * This code was generated from the Command Creator.\r\n *\r\n *\r\n */\r\n\r\nusin" +
                    "g System;\r\nusing System.Collections.Generic;\r\nusing System.Linq;\r\nusing System.T" +
                    "ext;\r\nusing System.Threading.Tasks;\r\n\r\nnamespace ChainsAPM.Commands.");
            
            #line 19 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.Namespace));
            
            #line default
            #line hidden
            this.Write("\r\n{\r\n    public class ");
            
            #line 21 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.ClassName));
            
            #line default
            #line hidden
            this.Write(" : Interfaces.ICommand<byte>\r\n    {\r\n\r\n         public DateTime TimeStamp { get; " +
                    "set; }\r\n");
            
            #line 25 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var enumn in this.EnumerationList)
{ 
            
            #line default
            #line hidden
            this.Write("        public enum ");
            
            #line 27 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(enumn.EnumerationName));
            
            #line default
            #line hidden
            this.Write("\r\n        {\r\n");
            
            #line 29 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var enumItem in enumn.Items)
{ 
            
            #line default
            #line hidden
            
            #line 31 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 if (enumn.Items.Last().Name != enumItem.Name)
{ 
            
            #line default
            #line hidden
            this.Write("            ");
            
            #line 33 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(enumItem.Name));
            
            #line default
            #line hidden
            this.Write(" = ");
            
            #line 33 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(enumItem.Value));
            
            #line default
            #line hidden
            this.Write(",\r\n");
            
            #line 34 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"

} else { 
            
            #line default
            #line hidden
            this.Write("            ");
            
            #line 36 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(enumItem.Name));
            
            #line default
            #line hidden
            this.Write(" = ");
            
            #line 36 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(enumItem.Value));
            
            #line default
            #line hidden
            this.Write("\r\n");
            
            #line 37 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            
            #line 38 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write("        }\r\n");
            
            #line 40 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n");
            
            #line 42 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties)
{ 
            
            #line default
            #line hidden
            this.Write("        public ");
            
            #line 44 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 if (TypeMapping.ContainsKey(cpItem.TypeName)) { 
            
            #line default
            #line hidden
            
            #line 44 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(TypeMapping[cpItem.TypeName].ToString()));
            
            #line default
            #line hidden
            
            #line 44 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else { 
            
            #line default
            #line hidden
            
            #line 44 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.TypeName.ToString()));
            
            #line default
            #line hidden
            
            #line 44 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
}
            
            #line default
            #line hidden
            this.Write(" ");
            
            #line 44 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" { get; set; }\r\n");
            
            #line 45 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n        public ");
            
            #line 47 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.ClassName));
            
            #line default
            #line hidden
            this.Write("()\r\n        {\r\n\r\n        }\r\n\r\n        public ");
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.ClassName));
            
            #line default
            #line hidden
            this.Write("(System.Int64 timeStamp, ");
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties) { if (TypeMapping.ContainsKey(cpItem.TypeName)) { 
            
            #line default
            #line hidden
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(TypeMapping[cpItem.TypeName].ToString()));
            
            #line default
            #line hidden
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else { 
            
            #line default
            #line hidden
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.TypeName.ToString()));
            
            #line default
            #line hidden
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
}
            
            #line default
            #line hidden
            this.Write("  ");
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name.ToLower()));
            
            #line default
            #line hidden
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
  if (this.ClassProperties.Last().Name != cpItem.Name) { 
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 52 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else { } } 
            
            #line default
            #line hidden
            this.Write(")\r\n        {\r\n            TimeStamp = DateTime.FromFileTimeUtc(timestamp);\r\n");
            
            #line 55 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties)
{ 
            
            #line default
            #line hidden
            this.Write("            ");
            
            #line 57 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = ");
            
            #line 57 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name.ToLower()));
            
            #line default
            #line hidden
            this.Write(";\r\n");
            
            #line 58 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write("        }\r\n\r\n        public string Name\r\n        {\r\n            get { return \"");
            
            #line 63 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.Name));
            
            #line default
            #line hidden
            this.Write("\"; }\r\n        }\r\n\r\n        public ushort Code\r\n        {\r\n            get { retur" +
                    "n 0x");
            
            #line 68 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.Code.ToString("x4")));
            
            #line default
            #line hidden
            this.Write("; }\r\n        }\r\n\r\n        public string Description\r\n        {\r\n            get {" +
                    " return \"");
            
            #line 73 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.Description));
            
            #line default
            #line hidden
            this.Write("\"; }\r\n        }\r\n\r\n        public Type CommandType\r\n        {\r\n            get { " +
                    "return typeof(");
            
            #line 78 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.CommandType.ToString()));
            
            #line default
            #line hidden
            this.Write(@"); }
        }

        public Interfaces.ICommand<byte> Decode(ArraySegment<byte> input)
        {

            if (input.Count != 0)
            {
                Helpers.ArraySegmentStream segstream = new Helpers.ArraySegmentStream(input);
                int size = segstream.GetInt32();
                if (input.Count == size)
                {
                    short code = segstream.GetInt16();
                    if (code == Code)
                    {
                    var timestamp = segstream.GetInt64();
");
            
            #line 94 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties) { 
string typeName = "";
if (TypeMapping.ContainsKey(cpItem.TypeName)) { typeName = TypeMapping[cpItem.TypeName].ToString(); } else { typeName = cpItem.TypeName.ToString(); }
if (typeName == typeof(string).FullName) { 
            
            #line default
            #line hidden
            this.Write("                        var stringlen");
            
            #line 98 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = segstream.GetInt32();\r\n                        var decode");
            
            #line 99 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write("Hash = segstream.GetInt32();\r\n                        var decode");
            
            #line 100 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = segstream.GetUnicode(stringlen");
            
            #line 100 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(");\r\n\r\n");
            
            #line 102 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(Int32).FullName) { 
            
            #line default
            #line hidden
            this.Write("                        var decode");
            
            #line 103 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = segstream.GetInt32();\r\n\r\n");
            
            #line 105 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(Int16).FullName) { 
            
            #line default
            #line hidden
            this.Write("                        var decode");
            
            #line 106 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = segstream.GetInt16();\r\n\r\n");
            
            #line 108 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(Int64).FullName) { 
            
            #line default
            #line hidden
            this.Write("                        var decode");
            
            #line 109 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = segstream.GetInt64();\r\n\r\n");
            
            #line 111 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(byte).FullName) { 
            
            #line default
            #line hidden
            this.Write("                        var decode");
            
            #line 112 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = segstream.GetByte();\r\n\r\n");
            
            #line 114 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("                        var decode");
            
            #line 115 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(" = (");
            
            #line 115 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeName));
            
            #line default
            #line hidden
            this.Write(")segstream.GetInt32();\r\n\r\n");
            
            #line 117 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            
            #line 118 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write(@"                       
                        var term = segstream.GetInt16();

                        if (term != 0)
                        {
                            throw new System.Runtime.Serialization.SerializationException(""Terminator is a non zero value. Please check the incoming byte stream for possible errors."");
                        }
                        return new ");
            
            #line 125 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(this.ClassName));
            
            #line default
            #line hidden
            this.Write("(timestamp, ");
            
            #line 125 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties) { 
            
            #line default
            #line hidden
            this.Write("decode");
            
            #line 125 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            
            #line 125 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 if (this.ClassProperties.Last().Name != cpItem.Name) { 
            
            #line default
            #line hidden
            this.Write(", ");
            
            #line 125 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else { } } 
            
            #line default
            #line hidden
            this.Write(@");
                    }
                    else
                    {
                        throw new System.Runtime.Serialization.SerializationException(""Invalid command code detected. Please check the incoming byte stream for possible errors."");
                    }
                }
                else
                {
                    throw new System.Runtime.Serialization.SerializationException(""Size of message does not match size of byte stream. Please check the incoming byte stream for possible errors."");
                }
            }
            else
            {
                throw new System.Runtime.Serialization.SerializationException(""Size of message is zero. Please check the incoming byte stream for possible errors. "");
            }
        }

        public byte[] Encode()
        {
            int byteSize 4;
");
            
            #line 146 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties) { 
string typeName = "";			 
if (TypeMapping.ContainsKey(cpItem.TypeName)) { typeName = TypeMapping[cpItem.TypeName].ToString(); } else { typeName = cpItem.TypeName.ToString(); }
if (typeName == typeof(string).FullName) { 
            
            #line default
            #line hidden
            this.Write("            byteSize += 4; // Length Bytes\r\n            byteSize += 4; // Hash By" +
                    "tes\r\n            byteSize += ");
            
            #line 152 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(".Length; // Hash Bytes\r\n");
            
            #line 153 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(Int32).FullName) { 
            
            #line default
            #line hidden
            this.Write("            byteSize += sizeof(");
            
            #line 154 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeName));
            
            #line default
            #line hidden
            this.Write(");\r\n");
            
            #line 155 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(Int16).FullName) { 
            
            #line default
            #line hidden
            this.Write("            byteSize += sizeof(");
            
            #line 156 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeName));
            
            #line default
            #line hidden
            this.Write(");\r\n");
            
            #line 157 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(Int64).FullName) { 
            
            #line default
            #line hidden
            this.Write("            byteSize += sizeof(");
            
            #line 158 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeName));
            
            #line default
            #line hidden
            this.Write(");\r\n");
            
            #line 159 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else if (typeName == typeof(byte).FullName) { 
            
            #line default
            #line hidden
            this.Write("            byteSize += sizeof(");
            
            #line 160 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(typeName));
            
            #line default
            #line hidden
            this.Write(");\r\n");
            
            #line 161 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("            byteSize += sizeof(Int32);\r\n");
            
            #line 163 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            
            #line 164 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write(@"            var buffer = new List<byte>(byteSize);
            buffer.AddRange(BitConverter.GetBytes(byteSize)); // 4 bytes for size, 2 byte for code, 8 bytes for data, 8 bytes for data, 8 bytes for TS, 2 bytes for term
            buffer.AddRange(BitConverter.GetBytes(Code));
            buffer.AddRange(BitConverter.GetBytes(TimeStamp.ToFileTimeUtc()));
");
            
            #line 169 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 foreach (var cpItem in this.ClassProperties) { 
            
            #line default
            #line hidden
            this.Write("            buffer.AddRange(BitConverter.GetBytes(");
            
            #line 170 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(cpItem.Name));
            
            #line default
            #line hidden
            this.Write(")); \r\n");
            
            #line 171 "D:\Data\APM\chainsapm\CommandCreator\CommandGeneratorCs.tt"
 } 
            
            #line default
            #line hidden
            this.Write("            buffer.AddRange(BitConverter.GetBytes((short)0));\r\n            return" +
                    " buffer.ToArray();\r\n            return null;\r\n        }\r\n    }\r\n}\r\n\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "14.0.0.0")]
    public class CommandGeneratorCsBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        protected System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
