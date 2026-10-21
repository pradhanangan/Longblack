namespace Longblack.Infrastructure.Purchasing;

public class CompanyOptions
{
    public const string SectionName = "Company";

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
}
