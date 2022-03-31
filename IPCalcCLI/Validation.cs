using IPLibrary;
using System.CommandLine.Parsing;

namespace IpCalcCli;

internal static class Validation
{
    internal static void ValidateIPv4Address(ArgumentResult result)
    {
        foreach (var token in result.Tokens)
        {
            if (!IPv4Address.IPv4Regex.IsMatch(token.Value))
            {
                result.ErrorMessage = $"Sorry. '{token.Value}' is not a valid IPv4 address.";
            }
        }
    }

    internal static void ValidateCIDR(OptionResult result)
    {
        foreach (var token in result.Tokens)
        {
            var canParse = int.TryParse(token.Value, out int parsed);
            if (!canParse || parsed < 0 || parsed > 32)
            {
                result.ErrorMessage = $"Sorry. '{token.Value}' is not a valid CIDR.{Environment.NewLine}A valid CIDR is a number between 0 and 32 included.";
            }
        }
    }
}
