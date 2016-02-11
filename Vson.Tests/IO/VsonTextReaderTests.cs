using NUnit.Framework;
using Vson.IO;
using Vson.Model;

namespace Vson.Tests.IO
{
	[TestFixture]
	public class VsonTextReaderTests
	{
		#region Special Asserts
		private static void AssertTokenIs(VsonToken? token, VsonTokenType expectedType, VsonValue expectedValue = null)
		{
			Assert.IsNotNull(token, "Token");
			Assert.AreEqual(expectedType, token.Value.Type);
			Assert.AreEqual(expectedValue, token.Value.Value);
		}

		private static void AssertIsEOF(VsonToken? token)
		{
			Assert.IsNull(token, "Expected EOF");
		}

		private static void AssertNextTokenThrows(VsonTextReader reader, string message)
		{
			var ex = Assert.Throws<VsonReaderException>(() => reader.NextToken());
			Assert.AreEqual(message, ex.Message);
		}

		private static void AssertLastTokenPosition(VsonTextReader reader, long offset, int line, int col)
		{
			var pos = reader.LastTokenPosition;
			Assert.AreEqual(offset, pos.Offset, "Offset");
			Assert.AreEqual(line, pos.Line, "Line");
			Assert.AreEqual(col, pos.Column, "Column");
		}
		#endregion

		#region Root Values
		[Test]
		public void ParseEmpty()
		{
			var reader = new VsonTextReader("");
			AssertIsEOF(reader.NextToken());
			AssertIsEOF(reader.NextToken()); // Reading beyond end is allowed
		}

		[Test]
		public void MultipleRootValues()
		{
			var reader = new VsonTextReader("1\"hi\"");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertNextTokenThrows(reader, "Unexpected start of string '\"' at char 1, line 1, column 2");
		}
		#endregion

		#region Numbers
		[Test]
		[TestCase("1.1", "1.1")]
		[TestCase("-1.1", "-1.1")]
		[TestCase("0.0", "0.0")]
		[TestCase("-0.0", "-0.0")]
		[TestCase("1E-06", "1E-06")]
		[TestCase("1e-06", "1E-06")]
		[TestCase("NaN", "NaN")]
		[TestCase("Infinity", "Infinity")]
		[TestCase("-Infinity", "-Infinity")]
		public void ParseNumbers(string vson, string expected)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber(expected));
		}

		[Test]
		[TestCase("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd")]
		[TestCase("0123")]
		[TestCase(".23")]
		[TestCase("-.23")]
		public void ParseNumbersInvalid(string vson)
		{
			var reader = new VsonTextReader(vson);
			AssertNextTokenThrows(reader, $"Invalid token '{vson}' at char 0, line 1, column 1");
		}
		#endregion

		#region Strings
		[Test]
		[TestCase("\"Somebody's Stuff\"", "Somebody's Stuff")]
		[TestCase("\"A surrogate pair: \uD835\uDEE2\"", "A surrogate pair: \uD835\uDEE2")]
		[TestCase("\"\\\"\"", "\"")]
		[TestCase("\"\\\\\"", "\\")]
		[TestCase("\"\\/\"", "/")]
		[TestCase("\"\\b\"", "\b")]
		[TestCase("\"\\f\"", "\f")]
		[TestCase("\"\\n\"", "\n")]
		[TestCase("\"\\r\"", "\r")]
		[TestCase("\"\\t\"", "\t")]
		[TestCase("\"\\v\"", "\v")]
		[TestCase("\"\\u2f6A\"", "\u2f6A")]
		[TestCase("\"\\u{b}\"", "\u000b")]
		[TestCase("\"\\u{10FFFF}\"", "\U0010FFFF")]
		[TestCase("\"\u0085\"", "\u0085")] // Unicode next line
		[TestCase("\"\u2028\"", "\u2028")] // Unicode line separator
		[TestCase("\"\u2029\"", "\u2029")] // Unicode paragraph separator
		public void ParseStrings(string vson, string expected)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString(expected));
		}

		[Test]
		[TestCase("\"", "Unexpected end of file at char 1, line 1, column 2")]
		[TestCase("\"\\", "Unexpected end of file at char 2, line 1, column 3")]
		[TestCase("\"\\u", "Unexpected end of file at char 3, line 1, column 4")]
		[TestCase("\"\\u12", "Unexpected end of file at char 5, line 1, column 6")]
		[TestCase("\"\\u{12", "Unexpected end of file at char 6, line 1, column 7")]
		[TestCase("\"\\P\"", "Unexpected character 'P' encountered at char 2, line 1, column 3")]
		[TestCase("\"\\uW\"", "Unexpected character 'W' encountered at char 3, line 1, column 4")]
		[TestCase("\"\\u{W}\"", "Unexpected character 'W' encountered at char 4, line 1, column 5")]
		[TestCase("\"\x00\"", "Unexpected character \\u0000 encountered at char 1, line 1, column 2")]
		[TestCase("\"\x0F\"", "Unexpected character \\u000F encountered at char 1, line 1, column 2")]
		[TestCase("\"\x1F\"", "Unexpected character \\u001F encountered at char 1, line 1, column 2")]
		public void ParseStringsInvalid(string vson, string expectedMessage)
		{
			var reader = new VsonTextReader(vson);
			AssertNextTokenThrows(reader, expectedMessage);
		}
		#endregion

		#region Bools
		[Test]
		[TestCase("true", true)]
		[TestCase("false", false)]
		public void ParseBools(string vson, bool expected)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Bool, (VsonBool)expected);
		}
		#endregion

		[Test]
		public void ParseNull()
		{
			var reader = new VsonTextReader("null");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Null, VsonNull.Value);
		}

		#region Dates and DateTimes
		[Test]
		[TestCase("2016-02-10", 2016, 2, 10, null)]
		[TestCase("02016-02-10", 2016, 2, 10, null)]
		[TestCase("+2016-02-10", 2016, 2, 10, null)]
		[TestCase("987518-01-01", 987518, 1, 1, null)]
		[TestCase("0000-12-10", 0, 12, 10, null)]
		[TestCase("-0001-01-01", -1, 1, 1, null)]
		[TestCase("-4568791-01-01", -4568791, 1, 1, null)]
		[TestCase("2016-02-10Z", 2016, 2, 10, 0)]
		[TestCase("2016-02-10+00:00", 2016, 2, 10, 0)]
		[TestCase("2016-02-10+05:30", 2016, 2, 10, 5 * 60 + 30)]
		[TestCase("2016-02-10-05:30", 2016, 2, 10, -(5 * 60 + 30))]
		public void ParseDates(string vson, long year, byte month, byte day, int? offset)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Date, new VsonDate(year, month, day, (short?)offset));
		}

		[Test]
		[TestCase("100-01-01")]
		[TestCase("-100-01-01")]
		[TestCase("2016-00-01")]
		[TestCase("2016-13-01")]
		[TestCase("2016-01-32")]
		[TestCase("2016-02-30")]
		[TestCase("2016-03-32")]
		[TestCase("2016-04-31")]
		[TestCase("2016-05-32")]
		[TestCase("2016-06-31")]
		[TestCase("2016-07-32")]
		[TestCase("2016-08-32")]
		[TestCase("2016-09-31")]
		[TestCase("2016-10-32")]
		[TestCase("2016-11-31")]
		[TestCase("2016-12-32")]
		[TestCase("2015-02-29")]
		[TestCase("2016-01-01z")]
		[TestCase("2016-01-01-5")]
		[TestCase("2016-01-01-0530")]
		[TestCase("2016-01-01-25")]
		[TestCase("2016-01-01+01:60")]
		[TestCase("2016-01-01+24:01")]
		public void ParseDatesInvalid(string vson)
		{
			var reader = new VsonTextReader(vson);
			AssertNextTokenThrows(reader, $"Invalid token '{vson}' at char 0, line 1, column 1");
		}

		[Test]
		[TestCase("2016-01/01", "Invalid token '2016-01' at char 0, line 1, column 1")]
		[TestCase("01/01/2016", "Invalid token '01' at char 0, line 1, column 1")]
		public void ParseDatesInvalidWithMessage(string vson, string expectedMessage)
		{
			var reader = new VsonTextReader(vson);
			AssertNextTokenThrows(reader, expectedMessage);
		}

		[Test]
		public void ParseDateInvalidWithMultipleTokens()
		{
			var reader = new VsonTextReader("2016/01-01");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2016"));
			AssertNextTokenThrows(reader, "Unexpected character '0' encountered at char 5, line 1, column 6");
		}

		[Test]
		[TestCase("2016-02-10T00:00", 2016, 2, 10, 0, 0, 0, "", null)]
		[TestCase("987518-01-01T24:00", 987518, 1, 1, 24, 0, 0, "", null)]
		[TestCase("0000-12-10T01:02:03", 0, 12, 10, 1, 2, 3, "", null)]
		[TestCase("-0001-01-01T12:13:14.041245413", -1, 1, 1, 12, 13, 14, "041245413", null)]
		[TestCase("-4568791-01-01T12:13:14.041245413000", -4568791, 1, 1, 12, 13, 14, "041245413", null)]
		[TestCase("2016-02-10T12:13:14.041245413Z", 2016, 2, 10, 12, 13, 14, "041245413", 0)]
		[TestCase("2016-02-10T12:13:14.041245413000+00:00", 2016, 2, 10, 12, 13, 14, "041245413", 0)]
		[TestCase("2016-02-10T01:02:03+05:30", 2016, 2, 10, 1, 2, 3, "", 5 * 60 + 30)]
		[TestCase("2016-02-10T01:02-05:30", 2016, 2, 10, 1, 2, 0, "", -(5 * 60 + 30))]
		public void ParseDateTimes(string vson, long year, byte month, byte day, byte hours, byte min, byte sec, string frac, int? offset)
		{
			var reader = new VsonTextReader(vson);
			AssertTokenIs(reader.NextToken(), VsonTokenType.DateTime, new VsonDateTime(year, month, day, hours, min, sec, frac, (short?)offset));
		}

		[Test]
		[TestCase("2016-01-01t12:30")]
		[TestCase("2016-01-01T1230")]
		[TestCase("2016-01-01T12")]
		[TestCase("2016-01-01T12.30")]
		[TestCase("2016-01-01T12:30.10")]
		[TestCase("2016-01-01T24:01")]
		[TestCase("2016-01-01T24:00:01")]
		[TestCase("2016-01-01T24:00:00.000000000000001")]
		[TestCase("2016-01-01T25:00")]
		[TestCase("2016-01-01T01:60")]
		[TestCase("2016-01-01T01:01:60")]
		[TestCase("2016-01-01T01:01:01.545544c4541")]
		[TestCase("2016-01-01T01:01:01:41543215")]
		public void ParseDatesTimesInvalid(string vson)
		{
			var reader = new VsonTextReader(vson);
			AssertNextTokenThrows(reader, $"Invalid token '{vson}' at char 0, line 1, column 1");
		}
		#endregion

		#region Arrays
		[Test]
		public void ParseEmptyArray()
		{
			var reader = new VsonTextReader("[]");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseArray()
		{
			var reader = new VsonTextReader("[1,2,3]");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("3"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseArrayReadingCommas()
		{
			var reader = new VsonTextReader("[1,2,3]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("3"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseArrayWithLeadingComma()
		{
			var reader = new VsonTextReader("[,1]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertNextTokenThrows(reader, "Unexpected comma ',' at char 1, line 1, column 2");
		}

		[Test]
		public void ParseArrayWithTrailingComma()
		{
			var reader = new VsonTextReader("[1,]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertNextTokenThrows(reader, "Unexpected end of array ']' at char 3, line 1, column 4");
		}

		[Test]
		public void ParseArrayWithMissingComma()
		{
			var reader = new VsonTextReader("[1\"hi\"]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertNextTokenThrows(reader, "Unexpected start of string '\"' at char 2, line 1, column 3");
		}

		[Test]
		public void ParseArrayWithMissingValue()
		{
			var reader = new VsonTextReader("[1,,2]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertNextTokenThrows(reader, "Unexpected comma ',' at char 3, line 1, column 4");
		}

		[Test]
		public void ParseArrayWithUnclosed()
		{
			var reader = new VsonTextReader("[1,2", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertNextTokenThrows(reader, "Unexpected end of file at char 4, line 1, column 5");
		}

		[Test]
		public void ParseArrayWithUnstarted()
		{
			var reader = new VsonTextReader("1,2]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertNextTokenThrows(reader, "Unexpected comma ',' at char 1, line 1, column 2");
		}
		#endregion

		#region Objects
		[Test]
		public void ParseEmptyObject()
		{
			var reader = new VsonTextReader("{}");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseObject()
		{
			var reader = new VsonTextReader("{\"a\":1,\"b\":\"hi\",\"c\":true,\"d\":2015-06-23}");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("b"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString("hi"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("c"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Bool, VsonBool.True);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("d"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Date, new VsonDate(2015, 6, 23, null));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseObjectReadingCommasAndColons()
		{
			var reader = new VsonTextReader("{\"a\":1,\"b\":\"hi\",\"c\":true,\"d\":2015-06-23}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("b"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString("hi"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("c"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Bool, VsonBool.True);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("d"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Date, new VsonDate(2015, 6, 23, null));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseObjectWithLeadingComma()
		{
			var reader = new VsonTextReader("{,\"a\":1}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertNextTokenThrows(reader, "Unexpected comma ',' at char 1, line 1, column 2");
		}

		[Test]
		public void ParseObjectWithLeadingColon()
		{
			var reader = new VsonTextReader("{:\"a\":1}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertNextTokenThrows(reader, "Unexpected colon ':' at char 1, line 1, column 2");
		}

		[Test]
		public void ParseObjectWithTrailingComma()
		{
			var reader = new VsonTextReader("{\"a\":1,}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertNextTokenThrows(reader, "Unexpected end of object '}' at char 7, line 1, column 8");
		}

		[Test]
		public void ParseObjectWithTrailingColon()
		{
			var reader = new VsonTextReader("{\"a\":1:}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertNextTokenThrows(reader, "Invalid token '1:' at char 5, line 1, column 6");
		}

		[Test]
		public void ParseObjectWithMissingComma()
		{
			var reader = new VsonTextReader("{\"a\":1\"b\":2}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertNextTokenThrows(reader, "Unexpected start of string '\"' at char 6, line 1, column 7");
		}

		[Test]
		public void ParseObjectWithMissingColon()
		{
			var reader = new VsonTextReader("{\"a\"1}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertNextTokenThrows(reader, "Unexpected number '1' at char 4, line 1, column 5");
		}

		[Test]
		public void ParseObjectWithUnclosed()
		{
			var reader = new VsonTextReader("{\"a\":1,\"b\":2");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("b"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertNextTokenThrows(reader, "Unexpected end of file at char 12, line 1, column 13");
		}

		[Test]
		public void ParseObjectWithUnstarted()
		{
			var reader = new VsonTextReader("\"a\":1}");
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString("a"));
			AssertNextTokenThrows(reader, "Unexpected colon ':' at char 3, line 1, column 4");
		}
		#endregion

		#region Nesting
		[Test]
		public void ParseEmptyObjectInArray()
		{
			var reader = new VsonTextReader("[{}]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseImproperNesting()
		{
			var reader = new VsonTextReader("[{]}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertNextTokenThrows(reader, "Unexpected end of array ']' at char 2, line 1, column 3");
		}

		[Test]
		public void ParseContinueArray()
		{
			var reader = new VsonTextReader("[{},2]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());

			reader = new VsonTextReader("[{}2]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertNextTokenThrows(reader, "Unexpected number '2' at char 3, line 1, column 4");
		}

		[Test]
		public void ParseContinueObject()
		{
			var reader = new VsonTextReader("{\"a\":[],\"b\":1}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("b"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertIsEOF(reader.NextToken());

			reader = new VsonTextReader("{\"a\":[]\"b\":1}", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertNextTokenThrows(reader, "Unexpected start of string '\"' at char 7, line 1, column 8");
		}
		#endregion

		#region WhiteSpace
		[Test]
		public void SkipWhiteSpaceAroundRoot()
		{
			var reader = new VsonTextReader(" \r\t\r\n  \n5 \r\t\r\n  \n");
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("5"));
			AssertLastTokenPosition(reader, 8, 3, 0);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseWhiteSpaceAroundRoot()
		{
			var reader = new VsonTextReader(" \r\t\r\n  \n5 \r\t\r\n  \n", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertLastTokenPosition(reader, 0, 0, 0);
			AssertTokenIs(reader.NextToken(), VsonTokenType.NewLine, new VsonString("\r"));
			AssertLastTokenPosition(reader, 1, 0, 1);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertLastTokenPosition(reader, 2, 1, 0);
			AssertTokenIs(reader.NextToken(), VsonTokenType.NewLine, new VsonString("\r\n"));
			AssertLastTokenPosition(reader, 3, 1, 1);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("  "));
			AssertLastTokenPosition(reader, 5, 2, 0);
			AssertTokenIs(reader.NextToken(), VsonTokenType.NewLine, new VsonString("\n"));
			AssertLastTokenPosition(reader, 7, 2, 2);

			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("5"));
			AssertLastTokenPosition(reader, 8, 3, 0);

			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertLastTokenPosition(reader, 9, 3, 1);
			AssertTokenIs(reader.NextToken(), VsonTokenType.NewLine, new VsonString("\r"));
			AssertLastTokenPosition(reader, 10, 3, 2);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertLastTokenPosition(reader, 11, 4, 0);
			AssertTokenIs(reader.NextToken(), VsonTokenType.NewLine, new VsonString("\r\n"));
			AssertLastTokenPosition(reader, 12, 4, 1);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("  "));
			AssertLastTokenPosition(reader, 14, 5, 0);
			AssertTokenIs(reader.NextToken(), VsonTokenType.NewLine, new VsonString("\n"));
			AssertLastTokenPosition(reader, 16, 5, 2);

			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void SkipWhiteSpaceInArray()
		{
			var reader = new VsonTextReader("[ 1 , 2\t,\t3 ]");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("3"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseWhiteSpaceInArray()
		{
			var reader = new VsonTextReader("[ 1 , 2\t,\t3 ]", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartArray);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("2"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("3"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndArray);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void SkipWhiteSpaceInObject()
		{
			var reader = new VsonTextReader("{ \"a\" : 1 , \"b\"\t:\t\"hi\"\t,\t\"c\":true }");
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("b"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString("hi"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("c"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Bool, VsonBool.True);
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertIsEOF(reader.NextToken());
		}

		[Test]
		public void ParseWhiteSpaceInObject()
		{
			var reader = new VsonTextReader("{ \"a\" : 1 , \"b\"\t:\t\"hi\"\t,\t\"c\":true }", true);
			AssertTokenIs(reader.NextToken(), VsonTokenType.StartObject);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("a"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Number, new VsonNumber("1"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("b"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.String, new VsonString("hi"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Comma);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString("\t"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.PropertyName, new VsonString("c"));
			AssertTokenIs(reader.NextToken(), VsonTokenType.Colon);
			AssertTokenIs(reader.NextToken(), VsonTokenType.Bool, VsonBool.True);
			AssertTokenIs(reader.NextToken(), VsonTokenType.WhiteSpace, new VsonString(" "));
			AssertTokenIs(reader.NextToken(), VsonTokenType.EndObject);
			AssertIsEOF(reader.NextToken());
		}
		#endregion

		#region Comments
		// TODO test comment parsing
		#endregion
	}
}
