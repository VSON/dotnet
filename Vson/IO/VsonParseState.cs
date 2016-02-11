using System;

namespace Vson.IO
{
	internal enum VsonParseState
	{
		RootValue,
		EOF,
		Finished,
		ArrayFirst,
		ArrayRest,
		ArrayValue,
		ObjectFirst,
		ObjectPropertyName,
		ObjectColon,
		ObjectValue,
		ObjectRest
	}

	internal static class VsonParseStateExtensions
	{
		public static bool AllowsEOF(this VsonParseState state)
		{
			return state == VsonParseState.RootValue
				|| state == VsonParseState.EOF
				|| state == VsonParseState.Finished;
		}

		public static bool AllowsValue(this VsonParseState state)
		{
			return state == VsonParseState.RootValue
				|| state == VsonParseState.ArrayFirst
				|| state == VsonParseState.ArrayValue
				|| state == VsonParseState.ObjectValue;
		}

		public static VsonParseState TransitionOnValue(this VsonParseState state)
		{
			switch(state)
			{
				case VsonParseState.RootValue:
					return VsonParseState.EOF;
				case VsonParseState.ArrayFirst:
				case VsonParseState.ArrayValue:
					return VsonParseState.ArrayRest;
				case VsonParseState.ObjectValue:
					return VsonParseState.ObjectRest;
				default:
					throw new NotSupportedException($"Can't TransitionOnValue from state '{state}'");
			}
		}

		public static bool AllowsString(this VsonParseState state)
		{
			return state == VsonParseState.RootValue
				|| state == VsonParseState.ArrayFirst
				|| state == VsonParseState.ArrayValue
				|| state == VsonParseState.ObjectFirst
				|| state == VsonParseState.ObjectPropertyName
				|| state == VsonParseState.ObjectValue;
		}

		public static bool PropertyNameExpected(this VsonParseState state)
		{
			return state == VsonParseState.ObjectFirst
				|| state == VsonParseState.ObjectPropertyName;
		}

		public static VsonParseState TransitionOnString(this VsonParseState state)
		{
			switch(state)
			{
				case VsonParseState.RootValue:
					return VsonParseState.EOF;
				case VsonParseState.ArrayFirst:
				case VsonParseState.ArrayValue:
					return VsonParseState.ArrayRest;
				case VsonParseState.ObjectFirst:
				case VsonParseState.ObjectPropertyName:
					return VsonParseState.ObjectColon;
				case VsonParseState.ObjectValue:
					return VsonParseState.ObjectRest;
				default:
					throw new NotSupportedException($"Can't TransitionOnString from state '{state}'");
			}
		}

		public static bool AllowsEndArray(this VsonParseState state)
		{
			return state == VsonParseState.ArrayFirst
				|| state == VsonParseState.ArrayRest;
		}

		public static bool AllowsEndObject(this VsonParseState state)
		{
			return state == VsonParseState.ObjectFirst
				|| state == VsonParseState.ObjectRest;
		}

		public static bool AllowsComma(this VsonParseState state)
		{
			return state == VsonParseState.ArrayRest
				|| state == VsonParseState.ObjectRest;
		}

		public static VsonParseState TransitionOnComma(this VsonParseState state)
		{
			switch(state)
			{
				case VsonParseState.ArrayRest:
					return VsonParseState.ArrayValue;
				case VsonParseState.ObjectRest:
					return VsonParseState.ObjectPropertyName;
				default:
					throw new NotSupportedException($"Can't TransitionOnComma from state '{state}'");
			}
		}
	}
}
