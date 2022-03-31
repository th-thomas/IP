using IPLibrary;
using Spectre.Console;

namespace IpCalcCli;

internal class IPTable
{
    private const string NOT_APPLICABLE = "N/A";

    private readonly IPv4Address _address;
    private readonly Table _table;
    private readonly bool _showBinary;

    internal IPTable(IPv4Address address, bool showBinary = false)
    {
        _address = address;
        _showBinary = showBinary;

        _table = new Table
        {
            Border = TableBorder.Rounded
        };
        _table.HideHeaders().Expand();
        Set();
    }

    internal void Render()
    {
        AnsiConsole.Render(_table);
    }

    private void Set()
    {
        var address = _address.ToString();
        var netmask = _address.Netmask is null ? NOT_APPLICABLE : string.Join('.', _address.Netmask);
        var netmaskAsCidr = _address.CIDR.HasValue ? _address.CIDR.Value.ToString() : NOT_APPLICABLE;
        var wildcard = _address.WildcardMask is null ? NOT_APPLICABLE : string.Join('.', _address.WildcardMask);
        var network = _address.NetworkAddress is null ? NOT_APPLICABLE : _address.NetworkAddress.ToString();
        var networkClass = _address.NetworkClass.HasValue ? (_showBinary ? $"[green]Class {_address.NetworkClass.Value}[/]" : $"Class {_address.NetworkClass.Value}") : NOT_APPLICABLE;
        var broadcast = _address.NetworkBroadcastAddress is null ? NOT_APPLICABLE : _address.NetworkBroadcastAddress.ToString();
        var firstHost = _address.FirstHostOnNetwork is null ? NOT_APPLICABLE : _address.FirstHostOnNetwork.ToString();
        var lastHost = _address.LastHostOnNetwork is null ? NOT_APPLICABLE : _address.LastHostOnNetwork.ToString();
        var hosts = _address.HostsInNetwork.HasValue ? _address.HostsInNetwork.Value.ToString() : NOT_APPLICABLE;

        var addressAsBin = _address.ToBinaryRepresentation();
        var netmaskAsBin = _address.Netmask is null ? NOT_APPLICABLE : IPv4Address.ToBinaryRepresentation(_address.Netmask);
        var wildcardAsBin = _address.WildcardMask is null ? NOT_APPLICABLE : IPv4Address.ToBinaryRepresentation(_address.WildcardMask);

        string networkAsBin = NOT_APPLICABLE;
        if (_address.NetworkAddress is not null)
        {
            var uncolorizedString = _address.NetworkAddress.ToBinaryRepresentation();
            var bitsToColorize = _address.NetworkClass switch
            {
                'A' => 1,
                'B' => 2,
                'C' => 3,
                'D' => 4,
                'E' => 4,
                _ => 0
            };
            networkAsBin = $"[green]{uncolorizedString[..bitsToColorize]}[/]{uncolorizedString[bitsToColorize..]}";
        }

        var broadcastAsBin = _address.NetworkBroadcastAddress is null ? NOT_APPLICABLE : _address.NetworkBroadcastAddress.ToBinaryRepresentation();
        var firstHosAsBin = _address.FirstHostOnNetwork is null ? NOT_APPLICABLE : _address.FirstHostOnNetwork.ToBinaryRepresentation();
        var lastHostAsBin = _address.LastHostOnNetwork is null ? NOT_APPLICABLE : _address.LastHostOnNetwork.ToBinaryRepresentation();

        var ipCells = new Dictionary<string, (string Info, string PointDecimalInfo)>
        {
            { "Address", (address, addressAsBin) },
            { "Netmask", ($"{netmask} = {netmaskAsCidr}", netmaskAsBin)},
            { "Wildcard", (wildcard, wildcardAsBin) },
            { "Network", ($"{network}/{netmaskAsCidr} {networkClass}", networkAsBin) },
            { "Broadcast", (broadcast, broadcastAsBin) },
            { "HostMin", (firstHost, firstHosAsBin) },
            { "HostMax", (lastHost, lastHostAsBin) },
            { "Hosts/Net", (hosts, string.Empty) }
        };

        _table.AddColumn(string.Empty);
        _table.AddColumn(string.Empty).LeftAligned();
        foreach (var col in _table.Columns)
        {
            col.PadRight(2);
        }

        if (_showBinary)
        {
            _table.AddColumn(string.Empty).LeftAligned();
        }

        foreach (var info in ipCells)
        {
            if (_showBinary)
            {
                _table.AddRow(info.Key, info.Value.Info, info.Value.PointDecimalInfo);
            }
            else
            {
                _table.AddRow(info.Key, info.Value.Info);
            }
        }
    }
}
