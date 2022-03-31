using System.Numerics;
using System.Text.RegularExpressions;

namespace IPLibrary;

public class IPv4Address
{
    #region Regex
    public const string IPV4PATTERN = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

    public static readonly Regex IPv4Regex = new(IPV4PATTERN, RegexOptions.Compiled);
    #endregion Regex

    #region Constructors
    private const string INVALIDIPV4ADDRESS_MESSAGE = "Address provided is not a valid IPv4 address. Please provide either an array of 4 bytes, or a string representation of the address in dot-decimal notation";

    public IPv4Address(byte[] address)
    {
        if (address.Length != 4)
        {
            throw new ArgumentException(INVALIDIPV4ADDRESS_MESSAGE, nameof(address));
        }
        _bytes = address;
    }

    public IPv4Address(string address)
    {
        if (!IPv4Regex.IsMatch(address))
        {
            throw new ArgumentException(INVALIDIPV4ADDRESS_MESSAGE);
        }
        _bytes = address.Split('.').Select(x => byte.Parse(x)).ToArray();
    }
    #endregion Constructors

    #region Public Properties   
    private readonly byte[] _bytes;
    public byte[] Bytes => _bytes;

    private int? _cidr;
    public int? CIDR
    {
        get => _cidr ?? DefaultNetmask.cidr;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value < 0 || value > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "A CIDR must be comprised between 0 an 32");
            }
            _cidr = value;
            var bits = Enumerable.Range(0, 32).Select(i => i < value);
            _netmask = Enumerable.Range(0, 4)
                .Select(i => (byte)bits.Select(b => b ? 1 : 0)
                            .Skip(i * 8)
                            .Take(8)
                            .Aggregate((k, j) => 2 * k + j))
                .ToArray();
        }
    }

    private byte[]? _netmask = null;

    public byte[]? Netmask
    {
        get => _netmask ?? DefaultNetmask.mask;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "Provided mask is not valid");
            }
            if (value.Length != 4)
            {
                throw new ArgumentException("Provided mask is not valid. It must consist of 4 bytes", nameof(value));
            }
            _netmask = value;
            _cidr = BitOperations.PopCount(BitConverter.ToUInt32(value));
        }
    }

    public IPv4Address? NetworkAddress
    {
        get
        {
            // Edge case of a PTP "Network"
            if ((_cidr.HasValue && _cidr.Value == 31) || (DefaultNetmask.cidr.HasValue && DefaultNetmask.cidr.Value == 31))
            {
                return null;
            }

            if (_netmask is not null)
            {
                return NetworkAddress(_netmask);
            }

            if (DefaultNetmask.mask is not null)
            {
                return NetworkAddress(DefaultNetmask.mask);
            }

            return null;

            IPv4Address NetworkAddress(byte[] mask) => new(_bytes.Select((x, i) => (byte)(x & mask[i])).ToArray());
        }
    }

    public IPv4Address? FirstHostOnNetwork
    {
        get
        {
            // Edge case of a /32 CIDR
            if ((_cidr.HasValue && _cidr.Value == 32) || (DefaultNetmask.cidr.HasValue && DefaultNetmask.cidr.Value == 32))
            {
                return this;
            }

            if (_netmask is not null)
            {
                // Edge case of a PTP "Network"
                if ((_cidr.HasValue && _cidr.Value == 31) || (DefaultNetmask.cidr.HasValue && DefaultNetmask.cidr.Value == 31))
                {
                    return FirstHost(_netmask, true);
                }
                return FirstHost(_netmask);
            }

            if (DefaultNetmask.mask is not null)
            {
                return FirstHost(DefaultNetmask.mask);
            }

            return null;


            IPv4Address FirstHost(byte[] mask, bool isPtp = false)
            {
                var bytes = _bytes.Select((x, i) => (byte)(x & mask[i])).ToArray();
                if (!isPtp)
                {
                    bytes[3] += 1;
                }
                return new IPv4Address(bytes);
            }
        }
    }

    public IPv4Address? NetworkBroadcastAddress
    {
        get
        {
            // Edge case of a PTP "Network"
            if ((_cidr.HasValue && _cidr.Value == 31) || (DefaultNetmask.cidr.HasValue && DefaultNetmask.cidr.Value == 31))
            {
                return null;
            }

            if (WildcardMask is null)
            {
                return null;
            }

            var bytes = _bytes.Select((x, i) => (byte)(x | WildcardMask[i])).ToArray();
            return new IPv4Address(bytes);
        }
    }

    public IPv4Address? LastHostOnNetwork
    {
        get
        {
            // Edge case of a /32 CIDR
            if ((_cidr.HasValue && _cidr.Value == 32) || (DefaultNetmask.cidr.HasValue && DefaultNetmask.cidr.Value == 32))
            {
                return this;
            }

            if (WildcardMask is null)
            {
                return null;
            }

            var bytes = _bytes.Select((x, i) => (byte)(x | WildcardMask[i])).ToArray();
            // Edge case of a PTP "Network"
            if (!((_cidr.HasValue && _cidr.Value == 31) || (DefaultNetmask.cidr.HasValue && DefaultNetmask.cidr.Value == 31)))
            {
                bytes[3] -= 1;
            }
            return new IPv4Address(bytes);
        }
    }

    public char? NetworkClass => _bytes[0] switch
    {
        < 127 => 'A',
        127 => null,
        < 192 => 'B',
        < 224 => 'C',
        < 240 => 'D',
        _ => 'E'
    };

    public uint? HostsInNetwork
    {
        get
        {
            if (_cidr.HasValue)
            {
                return HostsInNetwork(_cidr.Value);
            }

            if (DefaultNetmask.cidr.HasValue)
            {
                return HostsInNetwork(DefaultNetmask.cidr.Value);
            }

            return null;

            static uint HostsInNetwork(int cidr) => cidr switch
            {
                31 => 2,
                32 => 1,
                _ => (uint)Math.Pow(2, 32 - cidr) - 2
            };
        }
    }

    public byte[]? WildcardMask
    {
        get
        {
            if (_netmask is not null)
            {
                return WildcardMask(_netmask);
            }

            if (DefaultNetmask.mask is not null)
            {
                return WildcardMask(DefaultNetmask.mask);
            }

            return null;

            static byte[] WildcardMask(byte[] mask)
            {
                return mask.Select(x => (byte)~x).ToArray();
            }
        }
    }

    public (int? cidr, byte[]? mask) DefaultNetmask => _bytes[0] switch
    {
        < 127 => (8, new byte[] { 255, 0, 0, 0 }),
        127 => (null, null),
        < 192 => (16, new byte[] { 255, 255, 0, 0 }),
        < 224 => (24, new byte[] { 255, 255, 255, 0 }),
        < 240 => (4, null),
        _ => (null, null),
    };
    #endregion Public Properties

    #region Public Methods
    public override string ToString()
    {
        return string.Join('.', _bytes);
    }

    public string ToBinaryRepresentation()
    {
        return ToBinaryRepresentation(_bytes);
    }

    public static string ToBinaryRepresentation(byte[] bytes!!)
    {
        return string.Join('.', bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
    }
    #endregion
}