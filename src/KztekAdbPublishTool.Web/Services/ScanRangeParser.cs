using System.Net;

namespace KztekAdbPublishTool.Web.Services;

/// <summary>
/// Parse dải IP cho network scan. Pure static, không có side-effect.
/// Hỗ trợ 3 định dạng:
///   "192.168.1.1-254"              → last-octet range (cùng /24)
///   "192.168.1.1-192.168.1.254"    → full IP range (cùng /24)
///   "192.168.1.0/24"               → CIDR /24 (chỉ hỗ trợ /24)
///   "192.168.1.100"                → single IP
/// Ràng buộc: tất cả IP phải trong cùng /24, tối đa 512 địa chỉ.
/// </summary>
public static class ScanRangeParser
{
    /// <returns>true nếu parse thành công, false nếu có lỗi (error chứa thông báo tiếng Việt).</returns>
    public static bool TryParse(string rangeText, out List<string>? ips, out string? error)
    {
        ips = null;
        error = null;
        rangeText = rangeText?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rangeText))
        {
            error = "Vui lòng nhập dải IP cần quét.";
            return false;
        }

        List<string>? result;

        if (rangeText.Contains('/'))
        {
            if (!TryParseCidr(rangeText, out result, out error))
                return false;
        }
        else if (rangeText.Contains('-'))
        {
            if (!TryParseDashRange(rangeText, out result, out error))
                return false;
        }
        else
        {
            // Single IP
            if (!IPAddress.TryParse(rangeText, out _))
            {
                error = $"Địa chỉ IP không hợp lệ: {rangeText}";
                return false;
            }
            result = [rangeText];
        }

        if (result!.Count > 512)
        {
            error = $"Dải IP quá rộng ({result.Count} địa chỉ). Tối đa 512 địa chỉ mỗi lần quét.";
            return false;
        }

        ips = result;
        return true;
    }

    private static bool TryParseCidr(string cidr, out List<string>? ips, out string? error)
    {
        ips = null;
        error = null;

        var parts = cidr.Split('/', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var prefix))
        {
            error = $"Định dạng CIDR không hợp lệ: {cidr}";
            return false;
        }

        if (prefix != 24)
        {
            error = "Chỉ hỗ trợ subnet /24. Dải IP phải thuộc cùng một mạng /24.";
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out _))
        {
            error = $"Địa chỉ IP không hợp lệ: {parts[0]}";
            return false;
        }

        var octets = parts[0].Split('.');
        if (octets.Length != 4)
        {
            error = $"Địa chỉ IP không hợp lệ: {parts[0]}";
            return false;
        }

        var subnet = $"{octets[0]}.{octets[1]}.{octets[2]}";
        ips = Enumerable.Range(0, 256).Select(i => $"{subnet}.{i}").ToList();
        return true;
    }

    private static bool TryParseDashRange(string range, out List<string>? ips, out string? error)
    {
        ips = null;
        error = null;

        // Tìm vị trí dấu '-' từ bên phải để phân tách start và end
        // (vì có thể end là last octet hoặc full IP)
        var dashIdx = range.LastIndexOf('-');
        if (dashIdx < 1)
        {
            error = $"Định dạng dải IP không hợp lệ: {range}";
            return false;
        }

        var startStr = range[..dashIdx].Trim();
        var endStr = range[(dashIdx + 1)..].Trim();

        if (!IPAddress.TryParse(startStr, out _))
        {
            error = $"Địa chỉ IP đầu không hợp lệ: {startStr}";
            return false;
        }

        var startOctets = startStr.Split('.');
        if (startOctets.Length != 4)
        {
            error = $"Địa chỉ IP đầu không hợp lệ: {startStr}";
            return false;
        }

        int startLast, endLast;
        string subnet = $"{startOctets[0]}.{startOctets[1]}.{startOctets[2]}";

        if (int.TryParse(endStr, out endLast))
        {
            // Dạng "192.168.1.1-254" → end chỉ là octet cuối
            startLast = int.Parse(startOctets[3]);
        }
        else if (IPAddress.TryParse(endStr, out _))
        {
            // Dạng "192.168.1.1-192.168.1.254" → full IP
            var endOctets = endStr.Split('.');
            if (endOctets.Length != 4)
            {
                error = $"Địa chỉ IP cuối không hợp lệ: {endStr}";
                return false;
            }

            // Kiểm tra cùng /24
            if (startOctets[0] != endOctets[0] ||
                startOctets[1] != endOctets[1] ||
                startOctets[2] != endOctets[2])
            {
                error = "Dải IP phải thuộc cùng một mạng /24 (3 octet đầu phải giống nhau).";
                return false;
            }

            startLast = int.Parse(startOctets[3]);
            endLast = int.Parse(endOctets[3]);
        }
        else
        {
            error = $"Giá trị IP cuối không hợp lệ: {endStr}";
            return false;
        }

        if (startLast < 0 || startLast > 255 || endLast < 0 || endLast > 255)
        {
            error = $"Octet IP phải từ 0 đến 255.";
            return false;
        }

        if (endLast < startLast)
        {
            error = $"IP cuối phải lớn hơn hoặc bằng IP đầu ({startLast} > {endLast}).";
            return false;
        }

        ips = Enumerable.Range(startLast, endLast - startLast + 1)
            .Select(i => $"{subnet}.{i}")
            .ToList();

        return true;
    }
}
