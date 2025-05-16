namespace Acm.Api.Models;
public class BlockRule
{
    public List<string> Fields { get; set; } = new();
    public string Query { get; set; } = default!;
}

public class BlockConfig
{
    public List<BlockRule> BlockRules { get; set; } = new();
}