using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Vson.Model;

namespace Vson.IO
{
	public class VsonTextReader : VsonReader
	{
		private static readonly VsonToken LineFeedToken = new VsonToken(VsonTokenType.NewLine, new VsonString("\n"));
		private static readonly VsonToken CarriageReturnToken = new VsonToken(VsonTokenType.NewLine, new VsonString("\r"));
		private static readonly VsonToken CarriageReturnLineFeedToken = new VsonToken(VsonTokenType.NewLine, new VsonString("\r"));

		private readonly TextReader reader;
		private readonly bool readWhiteSpace;
		private readonly Stack<VsonContainerType> containers = new Stack<VsonContainerType>();
		private readonly StringBuilder buffer = new StringBuilder();

		public VsonTextReader(string value)
			: this(new StringReader(value), false)
		{
		}

		public VsonTextReader(string value, bool readWhiteSpace)
			: this(new StringReader(value), readWhiteSpace)
		{
		}

		public VsonTextReader(Stream stream)
			: this(new StreamReader(stream), false)
		{
		}

		public VsonTextReader(Stream stream, bool readWhiteSpace)
			: this(new StreamReader(stream), readWhiteSpace)
		{
		}

		public VsonTextReader(TextReader reader)
			: this(reader, false)
		{
		}

		public VsonTextReader(TextReader reader, bool readWhiteSpace)
		{
			this.reader = reader;
			this.readWhiteSpace = readWhiteSpace;
		}

		private string Debuffer()
		{
			var value = buffer.ToString();
			buffer.Clear();
			return value;
		}

		public override VsonToken? NextToken()
		{
			for(;;)
			{
				var next = reader.Read();
				if(next == -1)
					return LexEndOfFile();

				var nextChar = (char)next;

				switch(nextChar)
				{
					case ' ':
					case '\t':
						if(!readWhiteSpace) continue;
						buffer.Append(nextChar);
						return LexWhiteSpace();
					case '\n':
						if(!readWhiteSpace) continue;
						return LineFeedToken;
					case '\r':
						if(!readWhiteSpace) continue;
						if(reader.Peek() == '\n')
						{
							reader.Read();
							return CarriageReturnLineFeedToken;
						}
						return CarriageReturnToken;
					case '/':
						buffer.Append(nextChar);
						var comment = LexComment();
						if(readWhiteSpace)
							return comment;

						continue;
					case '{':
						containers.Push(VsonContainerType.Object);
						return new VsonToken(VsonTokenType.StartObject);
					case '}':
						containers.Pop(); // TODO validate
						return new VsonToken(VsonTokenType.EndObject);
					case '[':
						containers.Push(VsonContainerType.Array);
						return new VsonToken(VsonTokenType.StartArray);
					case ']':
						containers.Pop(); // TODO validate
						return new VsonToken(VsonTokenType.EndArray);
					case 't':
						return LexTrue();
					case 'f':
						return LexFalse();
					case 'n':
						return LexNull();
					case 'N':
						return LexNaN();
					case 'I':
						return LexInfinity();
					case '-':
						return reader.Peek() == 'I' ? LexNegativeInfinity() : LexNumeric(nextChar);
					case '+':
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						return LexNumeric(nextChar);
					case '"':
						return LexString(); // Need to account for properties etc.
					default:
						throw new NotImplementedException(); // Invalid char
				}
			}
		}

		private VsonToken LexString()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexNumeric(char nextChar)
		{
			buffer.Append(nextChar);
			if(nextChar == '-' || nextChar == '+')
				ReadDigit();

			ReadDigits();

			// Now we decide what to kind of token this really is
			var next = reader.Peek();
			switch(next)
			{
				// integer
				case -1:
				case ',':
				case ']':
				case ')':
				case '}':
				case ' ':
				case '\t':
				case '\r':
				case '\n':
				// decimal
				case '.':
				// exponent
				case 'e':
				case 'E':
					return LexNumber();
				case '-':
					return LexDate();
				default:
					throw VsonReaderException.UnexpectedCharacter((char)next);
			}
		}

		private void ReadDigit()
		{
			var next = reader.Read();
			if(next == -1) throw VsonReaderException.UnexpectedEndOfFile();
			if(next < '0' || next > '9') throw VsonReaderException.UnexpectedCharacter((char)next);
			buffer.Append((char)next);
		}

		private void ReadDigits()
		{
			int next;
			while((next = reader.Peek()) >= '0' && next <= '9')
				buffer.Append((char)reader.Read());
		}

		private VsonToken LexNumber()
		{
			int next;

			// Look for decimals
			if(reader.Peek() == '.')
			{
				buffer.Append((char)reader.Read());
				ReadDigit();
				ReadDigits();
			}

			// Now look for extensions
			if((next = reader.Peek()) == 'e' || next == 'E')
			{
				buffer.Append((char)reader.Read());
				next = reader.Peek();
				if(next == '-' || next == '+')
				{
					buffer.Append((char)reader.Read());
					next = reader.Peek();
				}
				ReadDigit();
				ReadDigits();
			}

			var stringValue = Debuffer();
			if(stringValue[0] == '+') throw VsonReaderException.InvalidNumber(stringValue);
			// Check for leading zero

			double value;
			if(double.TryParse(stringValue, NumberStyles.AllowLeadingSign, null, out value))
				return new VsonToken(VsonTokenType.Number, new VsonNumber(value));

			throw new VsonReaderException($"'{stringValue}' cannot be converted to a double");
		}

		private VsonToken LexNegativeInfinity()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexInfinity()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexNaN()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexDate()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexNull()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexFalse()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexTrue()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexComment()
		{
			throw new System.NotImplementedException();
		}

		private VsonToken LexWhiteSpace()
		{
			int nextChar;

			while((nextChar = reader.Peek()) == ' ' || nextChar == '\t')
				buffer.Append((char)reader.Read());

			return new VsonToken(VsonTokenType.WhiteSpace, new VsonString(Debuffer()));
		}

		private VsonToken? LexEndOfFile()
		{
			throw new System.NotImplementedException();
		}

		private bool LexValueWithSeparator(string value)
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			base.Close();
			reader.Close();
		}
	}
}
