using SMY.SqlToEntity.Common;

namespace SMY.SqlToEntity.Models;

/// <summary>
/// 登录dto
/// </summary>
public class LoginDto
{
    public DatabaseType Type { get; set; }
    /// <summary>
    /// 服务器
    /// </summary>
    public string? Server { get; set; }
    /// <summary>
    /// 数据库
    /// </summary>
    public string? DataBase { get; set; }
    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }
}