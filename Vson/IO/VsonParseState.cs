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
		ObjectProperty,
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
				|| state == VsonParseState.ObjectProperty
				|| state == VsonParseState.ObjectValue;
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
				case VsonParseState.ObjectProperty:
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
	}
}
