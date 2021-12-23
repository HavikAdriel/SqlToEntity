namespace SMY.SqlToEntity.Models;

/// <summary>
/// 表详细信息
/// </summary>
public class TableDetails
{
    /// <summary>
    /// 表名
    /// </summary>
    public string? TableName { get; set; }
    /// <summary>
    /// 字段名
    /// </summary>
    public string? ColName { get; set; }
    /// <summary>
    /// 字段类型
    /// </summary>
    public string? TypeName { get; set; }
    /// <summary>
    /// 注释
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// 是否可为空 0不可为空 1可空
    /// </summary>
    public int? IsNullable { get; set; }
}