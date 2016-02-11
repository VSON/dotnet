using Vson.Model;

namespace Vson.IO
{
	public struct VsonToken
	{
		public readonly VsonTokenType Type;
		public readonly VsonValue Value;

		public VsonToken(VsonTokenType type)
		{
			Type = type;
			Value = null;
		}

		public VsonToken(VsonTokenType type, VsonValue value)
		{
			Type = type;
			Value = value;
		}

		public bool IsWhiteSpace => Type == VsonTokenType.BlockComment ||
									Type == VsonTokenType.LineComment ||
									Type == VsonTokenType.WhiteSpace ||
									Type == VsonTokenType.NewLine ||
									Type == VsonTokenType.Comma ||
									Type == VsonTokenType.Colon;

		public bool IsComment => Type == VsonTokenType.BlockComment || Type == VsonTokenType.LineComment;
	}
}
