using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vson.Model;

namespace Vson.IO
{
	public class VsonTextReader : VsonReader
	{
		private const int EOF = -1;
		private static readonly char[] ExponentCharacters = { 'e', 'E' };
		private static readonly char[] DateTimeCharacters = { ':', 'T', 'Z', '+', '-' };

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
						throw new NotImplementedException();
					default:
						offset++;
						buffer.Append((char)reader.Read());
						break;
				}
			}
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
			throw new System.NotImplementedException();
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
