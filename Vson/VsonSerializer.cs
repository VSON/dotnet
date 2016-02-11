using System.IO;
using Vson.IO;
using Vson.Model;

namespace Vson
{
	public class VsonSerializer
	{
		public VsonValue Deserialize(string vson)
		{
			return Deserialize(new VsonTextReader(vson));
		}

		public VsonValue Deserialize(Stream stream)
		{
			return Deserialize(new VsonTextReader(stream));
		}

		public VsonValue Deserialize(TextReader reader)
		{
			return Deserialize(new VsonTextReader(reader));
		}

		public VsonValue Deserialize(VsonTextReader reader)
		{
			var value = DeserializeValue(reader);

			// Skip white space after the value
			VsonToken? token;
			do
			{
				token = reader.NextToken();
			} while(token != null && token.Value.IsWhiteSpace);

			if(token != null)
				throw new VsonSerializationException(reader.LastTokenPosition, $"Unexpected token {token.Value}");

			return value;
		}

		private static VsonValue DeserializeValue(VsonTextReader reader)
		{
			for(;;)
			{
				var token = reader.NextToken();
				if(token == null) throw new VsonSerializationException("No VSON value found");
				if(token.Value.IsWhiteSpace) continue;
				switch(token.Value.Type)
				{
					case VsonTokenType.Bool:
					case VsonTokenType.Date:
					case VsonTokenType.DateTime:
					case VsonTokenType.Null:
					case VsonTokenType.Number:
					case VsonTokenType.String:
						return token.Value.Value;
					case VsonTokenType.StartArray:
						return DeserializeArray(reader);
					case VsonTokenType.StartObject:
						return DeserializeObject(reader);
					default:
						throw new VsonSerializationException(reader.LastTokenPosition, $"Unexpected token {token.Value}");
				}
			}
		}

		private static VsonValue DeserializeArray(VsonTextReader reader)
		{
			var array = new VsonArray();
			for(;;)
			{
				var token = reader.NextToken();
				if(token.Value.IsWhiteSpace) continue;
				switch(token.Value.Type)
				{
					case VsonTokenType.Bool:
					case VsonTokenType.Date:
					case VsonTokenType.DateTime:
					case VsonTokenType.Null:
					case VsonTokenType.Number:
					case VsonTokenType.String:
						array.Add(token.Value.Value);
						break;
					case VsonTokenType.StartArray:
						array.Add(DeserializeArray(reader));
						break;
					case VsonTokenType.StartObject:
						array.Add(DeserializeObject(reader));
						break;
					case VsonTokenType.EndArray:
						return array;
					default:
						throw new VsonSerializationException($"Unexpected token {token.Value}");
				}
			}
		}

		private static VsonValue DeserializeObject(VsonTextReader reader)
		{
			var obj = new VsonObject();
			string propertyName = null;
			for(;;)
			{
				var token = reader.NextToken();
				if(token.Value.IsWhiteSpace) continue;
				switch(token.Value.Type)
				{
					case VsonTokenType.PropertyName:
						propertyName = token.Value.Value.ToString();
						break;
					case VsonTokenType.Bool:
					case VsonTokenType.Date:
					case VsonTokenType.DateTime:
					case VsonTokenType.Null:
					case VsonTokenType.Number:
					case VsonTokenType.String:
						obj.Add(propertyName, token.Value.Value);
						break;
					case VsonTokenType.StartArray:
						obj.Add(propertyName, DeserializeArray(reader));
						break;
					case VsonTokenType.StartObject:
						obj.Add(propertyName, DeserializeObject(reader));
						break;
					case VsonTokenType.EndObject:
						return obj;
					default:
						throw new VsonSerializationException(reader.LastTokenPosition, $"Unexpected token {token.Value}");
				}
			}
		}


	}
}
