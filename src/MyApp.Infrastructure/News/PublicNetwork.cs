using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace MyApp.Infrastructure.News;

/// <summary>
/// Защита от SSRF: адреса страниц берутся из чужих лент, поэтому ходить по ним можно только в публичный интернет.
/// Проверка делается при каждом соединении (в том числе после редиректов), по уже разрешённому IP.
/// </summary>
public static class PublicNetwork
{
    public static bool IsPublic(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.None))
            return false;

        var b = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return false;
            return (b[0] & 0xFE) != 0xFC;   // fc00::/7: локальные адреса
        }

        return !(b[0] is 0 or 10 or 127
                 || (b[0] == 172 && b[1] is >= 16 and <= 31)
                 || (b[0] == 192 && b[1] == 168)
                 || (b[0] == 169 && b[1] == 254)            // link-local, в том числе метаданные облака 169.254.169.254
                 || (b[0] == 100 && b[1] is >= 64 and <= 127)   // CGNAT
                 || b[0] >= 224);
    }

    /// <summary>Подставляется в SocketsHttpHandler.ConnectCallback.</summary>
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = (await Dns.GetHostAddressesAsync(host, ct)).Where(IsPublic).ToArray();
        if (addresses.Length == 0)
            throw new HttpRequestException($"Адрес {host} не относится к публичной сети");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
