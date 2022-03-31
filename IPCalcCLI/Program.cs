// See https://aka.ms/new-console-template for more information

using IpCalcCli;
using IPLibrary;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;

internal class Program
{
    private const string ADRESS_DESCRIPTION = "An IPv4 address in dot-decimal notation, e.g. 192.168.1.100";
    private const string CIDR_DESCRIPTION = "A netmask in CIDR notation (only the digits), e.g. 24";
    private const string VERBOSE_DESCRIPTION = "Show binary representation";

    internal static int Main(string[] args)
    {
        var addressArgument = new Argument<string>(ADRESS_DESCRIPTION);

        addressArgument.AddValidator(Validation.ValidateIPv4Address);

        var cidrOption = new Option<int?>(new string[] { "-c", "--cidr" }, CIDR_DESCRIPTION);

        cidrOption.AddValidator(Validation.ValidateCIDR);

        var verboseOption = new Option<bool>(aliases: new string[] { "-b", "--binary", "-v", "--verbose" },
                                             description: VERBOSE_DESCRIPTION,
                                             getDefaultValue: () => false);

        var rootCommand = new RootCommand("Gives various information related to an IPv4 address")
        {
            addressArgument,
            cidrOption,
            verboseOption
        };

        rootCommand.SetHandler((string s, int? i, bool b) => IPCalc(s, i, b), addressArgument, cidrOption, verboseOption);

        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp(context =>
            {
                context.HelpBuilder.CustomizeLayout(AddHelpFiglet());
            })
            .Build();

        return parser.Invoke(args);

        static Func<HelpContext, IEnumerable<HelpSectionDelegate>> AddHelpFiglet()
        {
            return _ => HelpBuilder.Default.GetLayout().Skip(1).Prepend(_ => AnsiConsole.Render(new FigletText("IPcalc").Color(Color.Green)));
        }
    }

    private static void IPCalc(string addressString, int? cidr = null, bool verbose = false)
    {
        try
        {
            var ip = new IPv4Address(addressString);
            if (cidr.HasValue)
            {
                ip.CIDR = cidr;
            }

            var table = new IPTable(ip, verbose);
            table.Render();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Something bad happened.");
            Console.WriteLine(ex);
        }
    }
}