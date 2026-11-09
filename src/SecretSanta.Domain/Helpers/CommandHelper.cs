using SecretSanta.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecretSanta.Domain.Helpers;

internal class CommandHelper
{
	public static bool TryMapCommand(string text, out CommandType cmd)
	{
		foreach (var c in from CommandType c in Enum.GetValues<CommandType>()
						  let fi = typeof(CommandType).GetField(c.ToString())!
						  let disp = (DisplayAttribute)Attribute.GetCustomAttribute(fi, typeof(DisplayAttribute))!
						  let name = disp?.Name ?? c.ToString()
						  where string.Equals(name, text, StringComparison.OrdinalIgnoreCase)
						  select c)
		{
			cmd = c;
			return true;
		}

		cmd = default;
		return false;
	}
}
