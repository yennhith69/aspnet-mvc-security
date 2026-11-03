public class HomeDashboardViewModel
{
    public bool IsAuthenticated { get; set; }
    public int EmployeeCount { get; set; }
    public int ExpiringContractCount { get; set; }
    public int EvidenceCount { get; set; }
    public int ConfirmedProfileCount { get; set; }
    public IReadOnlyList<HomeEmployeeRow> RecentEmployees { get; set; } = Array.Empty<HomeEmployeeRow>();
}

public class HomeEmployeeRow
{
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public string ProfileStatus { get; set; } = "";
}
