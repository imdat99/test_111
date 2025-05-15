namespace Acm.Api.Models;
public class BlockRule
{
    public string Type { get; set; } = default!;
    public List<string> Fields { get; set; } = new();
    public string UnderField { get; set; } = default!;
}

public class BlockConfig
{
    public List<BlockRule> BlockRules { get; set; } = new();
}