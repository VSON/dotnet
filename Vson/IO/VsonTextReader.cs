using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Vson.Model;

namespace Vson.IO
{
	public class VsonTextReader : VsonReader
	{
		private const int EOF = -1;
		private static readonly char[] ExponentCharacters = { 'e', 'E' };
		private static readonly char[] DateTimeCharacters = { ':', 'T', 'Z', '+', '-' };
		private const byte None = 255;

		private static readonly VsonToken LineFeedToken = new VsonToken(VsonTokenType.NewLine, new VsonString("\n"));
		private static readonly VsonToken CarriageReturnToken = new VsonToken(VsonTokenType.NewLine, new VsonString("\r"));
		private static readonly VsonToken CarriageReturnLineFeedToken = new VsonToken(VsonTokenType.NewLine, new VsonString("\r"));
		private static IDictionary<string, VsonToken> keywordTokens;

		private static IDictionary<string, VsonToken> KeywordTokens => keywordTokens ?? (keywordTokens = new SortedDictionary<string, VsonToken>()
		{
			{"true", new VsonToken(VsonTokenType.Bool, VsonBool.True)},
			{"false", new VsonToken(VsonTokenType.Bool, VsonBool.False)},
			{"NaN", new VsonToken(VsonTokenType.Number, VsonNumber.NaN)},
			{"Infinity", new VsonToken(VsonTokenType.Number, VsonNumber.PositiveInfinity)},
			{"-Infinity", new VsonToken(VsonTokenType.Number, VsonNumber.NegativeInfinity)},
			{"null", new VsonToken(VsonTokenType.Null, VsonNull.Value)},
		});

		private readonly TextReader reader;
		private readonly bool readWhiteSpace;
		private readonly Stack<VsonContainerType> containers = new Stack<VsonContainerType>();
		private readonly StringBuilder buffer = new StringBuilder();
		private TextPosition lastTokenPosition;
		private TextPosition currentPosition;

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

		public TextPosition LastTokenPosition => lastTokenPosition;
		public TextPosition CurrentPosition => currentPosition;

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
				lastTokenPosition = currentPosition;

				var next = reader.Peek();
				switch(next)
				{
					case EOF:
						reader.Read();
						return null;
					case ' ':
					case '\t':
						if(readWhiteSpace)
							return LexWhiteSpace();

						SkipWhiteSpaceAndNewLine();
						continue;
					case '\n':
						if(readWhiteSpace)
						{
							reader.Read();
							currentPosition = currentPosition.NewLine();
							return LineFeedToken;
						}

						SkipWhiteSpaceAndNewLine();
						continue;
					case '\r':
						if(readWhiteSpace)
						{
							reader.Read();
							next = reader.Peek();
							if(next == '\n')
							{
								reader.Read();
								currentPosition = currentPosition.NewLine(2);
								return CarriageReturnLineFeedToken;
							}
							currentPosition = currentPosition.NewLine();
							return CarriageReturnToken;
						}

						SkipWhiteSpaceAndNewLine();
						continue;
					case '/':
						var comment = LexComment();
						if(readWhiteSpace)
							return comment;

						continue;
					case '{':
						reader.Read();
						containers.Push(VsonContainerType.Object);
						return new VsonToken(VsonTokenType.StartObject);
					case '}':
						reader.Read();
						containers.Pop(); // TODO validate
						return new VsonToken(VsonTokenType.EndObject);
					case '[':
						reader.Read();
						containers.Push(VsonContainerType.Array);
						return new VsonToken(VsonTokenType.StartArray);
					case ']':
						reader.Read();
						containers.Pop(); // TODO validate
						return new VsonToken(VsonTokenType.EndArray);
					case ',':
						reader.Read();
						if(readWhiteSpace)
							return new VsonToken(VsonTokenType.Comma);

						continue;
					case '"':
						return LexString(); // Need to account for properties etc.
					default:
						BufferToken();
						return LexToken();
				}
			}
		}

		private VsonToken LexWhiteSpace()
		{
			int next;

			// LexWhiteSpace should only be called if the next char is ' ' or '\t'
			do
			{
				buffer.Append((char)reader.Read());
			} while((next = reader.Peek()) == ' ' || next == '\t');

			currentPosition = currentPosition.Advance(buffer.Length);
			return new VsonToken(VsonTokenType.WhiteSpace, new VsonString(Debuffer()));
		}

		private void SkipWhiteSpaceAndNewLine()
		{
			var offset = 0;
			for(;;)
			{
				var next = reader.Peek();
				switch(next)
				{
					case ' ':
					case '\t':
						reader.Read();
						offset++;
						break;
					case '\n':
						reader.Read();
						currentPosition = currentPosition.NewLine(offset + 1);
						offset = 0;
						break;
					case '\r':
						reader.Read();
						next = reader.Peek();
						if(next == '\n')
						{
							reader.Read();
							offset += 2;
						}
						else
							offset += 1;
						currentPosition = currentPosition.NewLine(offset);
						offset = 0;
						break;
					default:
						currentPosition = currentPosition.Advance(offset);
						return;
				}
			}
		}

		private VsonToken LexComment()
		{
			// LexComment should only be called when the next char is '/'
			buffer.Append((char)reader.Read());
			var next = reader.Peek();
			switch(next)
			{
				case '/':
					return LexLineComment();
				case '*':
					return LexBlockComment();
				case EOF:
					currentPosition = currentPosition.Advance();
					throw VsonReaderException.UnexpectedEndOfFile(currentPosition);
				default:
					currentPosition = currentPosition.Advance();
					throw VsonReaderException.UnexpectedCharacter(currentPosition, (char)next);
			}
		}

		private VsonToken LexLineComment()
		{
			int next;

			// LexLineComment should only be called when the next char is '/'
			do
			{
				buffer.Append((char)reader.Read());
			} while((next = reader.Peek()) != '\r' && next == '\n' && next != EOF);

			currentPosition = currentPosition.Advance(buffer.Length);
			return new VsonToken(VsonTokenType.LineComment, new VsonString(Debuffer()));
		}

		private VsonToken LexBlockComment()
		{
			// LexLineComment should only be called when the next char is '*'
			buffer.Append((char)reader.Read());

			var offset = 0;
			var lastCharStar = false;
			for(;;)
			{
				var next = reader.Peek();
				switch(next)
				{
					case EOF:
						currentPosition = currentPosition.Advance(offset);
						throw VsonReaderException.UnexpectedEndOfFile(currentPosition);
					case '\n':
						lastCharStar = false;
						buffer.Append((char)reader.Read());
						currentPosition = currentPosition.NewLine(offset + 1);
						offset = 0;
						break;
					case '\r':
						lastCharStar = false;
						buffer.Append((char)reader.Read());
						next = reader.Peek();
						if(next == '\n')
						{
							buffer.Append((char)reader.Read());
							offset += 2;
						}
						else
							offset += 1;
						currentPosition = currentPosition.NewLine(offset);
						offset = 0;
						break;
					case '*':
						lastCharStar = true;
						buffer.Append((char)reader.Read());
						offset++;
						break;
					case '/':
						if(lastCharStar)
						{
							buffer.Append((char)reader.Read());
							currentPosition = currentPosition.Advance(offset + 1);
							return new VsonToken(VsonTokenType.BlockComment, new VsonString(Debuffer()));
						}
						goto default;
					default:
						lastCharStar = false;
						buffer.Append((char)reader.Read());
						offset++;
						break;
				}
			}
		}

		private VsonToken LexString()
		{
			// LexString should only be called when the next char is '"'
			reader.Read();
			var offset = 1; // Start at one to count the " char
			for(;;)
			{
				var next = reader.Peek();
				switch(next)
				{
					case EOF:
						currentPosition = currentPosition.Advance(offset);
						throw VsonReaderException.UnexpectedEndOfFile(currentPosition);
					case '"':
						reader.Read();
						currentPosition = currentPosition.Advance(offset + 1);
						return new VsonToken(VsonTokenType.String, new VsonString(Debuffer()));
					case '\\':
						reader.Read();
						next = reader.Peek();
						switch(next)
						{
							case EOF:
								currentPosition = currentPosition.Advance(offset + 1);
								throw VsonReaderException.UnexpectedEndOfFile(currentPosition);
							case '"':
							case '\\':
							case '/':
								buffer.Append((char)reader.Read());
								offset += 2;
								break;
							case 'b':
								reader.Read();
								offset += 2;
								buffer.Append('\b');
								break;
							case 'f':
								reader.Read();
								offset += 2;
								buffer.Append('\f');
								break;
							case 'n':
								reader.Read();
								offset += 2;
								buffer.Append('\n');
								break;
							case 'r':
								reader.Read();
								offset += 2;
								buffer.Append('\r');
								break;
							case 't':
								reader.Read();
								offset += 2;
								buffer.Append('\t');
								break;
							case 'v':
								reader.Read();
								offset += 2;
								buffer.Append('\v');
								break;
							case 'u':
								reader.Read();
								currentPosition = currentPosition.Advance(offset); // Let LexUnicodeEscape account for \u
								offset = 0;
								LexUnicodeEscape();
								break;
							default:
								currentPosition = currentPosition.Advance(offset + 1);
								throw VsonReaderException.UnexpectedCharacter(currentPosition, (char)reader.Read());
						}
						break;
					default:
						offset++;
						buffer.Append((char)reader.Read());
						break;
				}
			}
		}

		private void LexUnicodeEscape()
		{
			var escapePosition = currentPosition;
			currentPosition = currentPosition.Advance(2); // for \u
			var codepoint = 0;
			var next = reader.Peek();
			if(next == '{')
			{
				reader.Read();
				currentPosition = currentPosition.Advance();
				for(var i = 0; i < 6; i++)
					if(!LexHexDigit(ref codepoint, false))
						break;
				next = reader.Read();
				if(next == EOF) throw VsonReaderException.UnexpectedEndOfFile(currentPosition);
				if(next != '}') throw VsonReaderException.UnexpectedCharacter(currentPosition, (char)next);
				currentPosition = currentPosition.Advance();
			}
			else
			{
				LexHexDigit(ref codepoint);
				LexHexDigit(ref codepoint);
				LexHexDigit(ref codepoint);
				LexHexDigit(ref codepoint);
			}

			if(codepoint > 0x10FFFF)
				throw new VsonReaderException(escapePosition, "Invalide Unicode escape sequence");

			if(codepoint <= 0xFFFF)
				buffer.Append((char)codepoint);
			else
				buffer.Append(char.ConvertFromUtf32(codepoint));
		}

		private bool LexHexDigit(ref int codepoint, bool fixedWidth = true)
		{
			var next = reader.Peek();
			switch(next)
			{
				case EOF:
					throw VsonReaderException.UnexpectedEndOfFile(currentPosition);
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
					codepoint = (codepoint << 4) + (next - '0');
					break;
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
					codepoint = (codepoint << 4) + (next - 'a') + 0xa;
					break;
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
					codepoint = (codepoint << 4) + (next - 'A') + 0xA;
					break;
				case '}':
					if(!fixedWidth) return false;
					goto default;
				default:
					throw VsonReaderException.UnexpectedCharacter(currentPosition, (char)reader.Read());
			}

			reader.Read();
			currentPosition = currentPosition.Advance();
			return true;
		}

		private void BufferToken()
		{
			for(;;)
			{
				var next = reader.Peek();
				switch(next)
				{
					case EOF:
					case '[':
					case ']':
					case ',':
					case '"':
					case '{':
					case '}':
					case '/':
					case ' ':
					case '\t':
					case '\n':
					case '\r':
						currentPosition = currentPosition.Advance(buffer.Length);
						return;
					case '+':
					case '-':
					case '.':
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
					case ':':
						buffer.Append((char)reader.Read());
						break;
					default:
						if((next >= 'a' && next <= 'z') || (next >= 'A' && next <= 'Z'))
						{
							buffer.Append((char)reader.Read());
							continue;
						}
						currentPosition = currentPosition.Advance(buffer.Length);
						throw VsonReaderException.UnexpectedCharacter(currentPosition, (char)reader.Read());
				}
			}
		}

		private VsonToken LexToken()
		{
			// The token should already be in the buffer, we just need to figure out what it is
			var token = Debuffer();
			var firstChar = token[0];
			switch(firstChar)
			{
				case 't':
				case 'f':
				case 'n':
				case 'N':
				case 'I':
					return LexKeyword(token);
				case '-':
					if(token.Length >= 2 && token[1] == 'I')
						return LexKeyword(token);
					goto default;
				default:
					if(token.LastIndexOfAny(DateTimeCharacters) > 1 && token.IndexOfAny(ExponentCharacters) == -1)
						return LexDateTime(token);

					return LexNumber(token);
			}
		}

		private VsonToken LexKeyword(string token)
		{
			VsonToken keywordToken;
			if(KeywordTokens.TryGetValue(token, out keywordToken))
				return keywordToken;

			throw VsonReaderException.InvalidToken(lastTokenPosition, token);
		}

		private VsonToken LexDateTime(string token)
		{
			var pos = 0;

			// year
			var negativeYear = false;
			if(token[pos] == '-')
			{
				pos++;
				negativeYear = true;
			}

			pos = token.IndexOf('-', pos);
			BigInteger year;
			if(pos < 0
				|| !BigInteger.TryParse(token.Substring(0, pos), out year)
				|| year == 0 && negativeYear
				|| pos < (negativeYear ? 5 : 4))
				throw VsonReaderException.InvalidToken(lastTokenPosition, token);

			// month
			byte month;
			if(token[pos] != '-'
				|| pos + 2 >= token.Length
				|| !byte.TryParse(token.Substring(pos + 1, 2), out month)
				|| month < 1
				|| month > 12)
				throw VsonReaderException.InvalidToken(lastTokenPosition, token);

			pos += 3;

			// day
			byte day;
			if(pos >= token.Length
				|| token[pos] != '-'
				|| pos + 2 >= token.Length
				|| !byte.TryParse(token.Substring(pos + 1, 2), out day)
				|| day < 1
				|| day > VsonDate.DaysInMonth(year, day))
				throw VsonReaderException.InvalidToken(lastTokenPosition, token);

			pos += 3;

			// time
			byte hours = None; // Watch out for strange flag values because 0 is valid
			byte min = None;
			byte sec = 0;
			var frac = "";
			if(pos < token.Length && token[pos] == 'T')
			{
				pos++;
				if(pos + 2 > token.Length
					|| !byte.TryParse(token.Substring(pos, 2), out hours)
					|| hours > 24)
					throw VsonReaderException.InvalidToken(lastTokenPosition, token);
				pos += 2;
				if(pos + 3 > token.Length
				   || !byte.TryParse(token.Substring(pos + 1, 2), out min)
				   || min > 59
				   || (hours == 24 && min != 0))
					throw VsonReaderException.InvalidToken(lastTokenPosition, token);
				pos += 3;
				if(pos < token.Length && token[pos] == ':')
				{
					pos++;
					if(pos + 2 > token.Length
						|| !byte.TryParse(token.Substring(pos, 2), out sec)
						|| sec > 59
						|| (hours == 24 && sec != 0))
						throw VsonReaderException.InvalidToken(lastTokenPosition, token);
					pos += 2;
					if(pos < token.Length && token[pos] == '.')
					{
						pos++;
						var fracEnd = token.IndexOfAny(DateTimeCharacters, pos);
						if(fracEnd == -1) fracEnd = token.Length;
						frac = token.Substring(pos, fracEnd - pos).TrimEnd('0');
						foreach(char t in frac)
							if(t < '0' || t > '9')
								throw VsonReaderException.InvalidToken(lastTokenPosition, token);
						if(hours == 24 && frac != "")
							throw VsonReaderException.InvalidToken(lastTokenPosition, token);
						pos = fracEnd;
					}
				}
			}

			// offset
			short? offset = null;
			if(pos < token.Length)
				switch(token[pos])
				{
					case 'Z':
						offset = 0;
						pos++;
						break;
					case '+':
					case '-':
						short offsetHours;
						if(pos + 3 > token.Length
							|| !short.TryParse(token.Substring(pos, 3), out offsetHours)
							|| offsetHours > 24
							|| offsetHours < -24)
							throw VsonReaderException.InvalidToken(lastTokenPosition, token);
						offset = (short)(offsetHours * 60);
						pos += 3;
						if(pos < token.Length)
						{
							short offsetMinutes;
							if(pos + 3 > token.Length
								|| token[pos] != ':'
								|| !short.TryParse(token.Substring(pos + 1, 2), out offsetMinutes)
								|| offsetMinutes > 59
								|| offsetMinutes < 0)
								throw VsonReaderException.InvalidToken(lastTokenPosition, token);
							offset += (short)(offsetMinutes * Math.Sign(offset.Value));
							pos += 3;
							if(offset > 24 * 60 || offset < -24 * 60)
								throw VsonReaderException.InvalidToken(lastTokenPosition, token);
						}
						break;
				}

			if(token.Length != pos) // non-consumed characters
				throw VsonReaderException.InvalidToken(lastTokenPosition, token);

			if(hours != None)
				return new VsonToken(VsonTokenType.DateTime, new VsonDateTime(year, month, day, hours, min, sec, frac, offset));

			return new VsonToken(VsonTokenType.Date, new VsonDate(year, month, day, offset));
		}

		private VsonToken LexNumber(string token)
		{
			var pos = 0;

			// Negative sign
			if(pos < token.Length && token[pos] == '-')
				pos++;

			// Integer part
			if(pos >= token.Length)
				throw VsonReaderException.InvalidToken(lastTokenPosition, token);
			if(token[pos] == '0')
				pos++;
			else
			{
				if(!(token[pos] >= '1' && token[pos] <= '9'))
					throw VsonReaderException.InvalidToken(lastTokenPosition, token);
				do
				{
					pos++;
				} while(pos < token.Length && token[pos] >= '0' && token[pos] <= '9');
			}

			// Fraction part
			if(pos < token.Length && token[pos] == '.')
			{
				pos++;
				if(pos >= token.Length || !(token[pos] >= '0' && token[pos] <= '9'))
					throw VsonReaderException.InvalidToken(lastTokenPosition, token);
				do
				{
					pos++;
				} while(pos < token.Length && token[pos] >= '0' && token[pos] <= '9');
			}

			// Exponent part
			if(pos < token.Length && (token[pos] == 'e' || token[pos] == 'E'))
			{
				pos++;
				if(pos >= token.Length)
					throw VsonReaderException.InvalidToken(lastTokenPosition, token);
				if(token[pos] == '+' || token[pos] == '-') pos++;
				if(pos >= token.Length || !(token[pos] >= '0' && token[pos] <= '9'))
					throw VsonReaderException.InvalidToken(lastTokenPosition, token);
				do
				{
					pos++;
				} while(pos < token.Length && token[pos] >= '0' && token[pos] <= '9');
			}

			if(token.Length != pos) // non-consumed characters
				throw VsonReaderException.InvalidToken(lastTokenPosition, token);

			return new VsonToken(VsonTokenType.Number, new VsonNumber(token));
		}

		public override void Close()
		{
			base.Close();
			reader.Close();
		}
	}
}
