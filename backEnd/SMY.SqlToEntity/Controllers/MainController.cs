using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using SMY.SqlToEntity.Common;
using SMY.SqlToEntity.Models;

namespace SMY.SqlToEntity.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class MainController : ControllerBase
{
    private static IDbConnection _conn;
    private static string _connString=string.Empty;
    public MainController()
    {
        // _connString=@"Data Source=192.168.0.137\MSSQL2019;Initial Catalog=BootstrapAdmin;
        //     User ID=sa;Password=cqprow;Pooling=true;max pool size=512;MultipleActiveResultSets=true;";
        // _conn=new SqlConnection(_connString);
    }
    [HttpPost("Login")]
    public IActionResult Login([FromForm]string connStr)
    {
        _connString = connStr;
        try
        {
            _conn = new SqlConnection(_connString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e.Message);
        }

        return Ok("连接成功");
    }
    [HttpPost("GetEntityBySelect")]
    public string GetEntityBySelect([FromForm]string sql,[FromForm]AdvanceConfig config)
    {
        if (_conn==null)
        {
            throw new Exception("请先进行数据库连接");
        }
        StringBuilder sb = new StringBuilder();
        using (_conn=new SqlConnection(_connString))
        {
            var reader = _conn.ExecuteReader(sql);
            if (reader!=null)
            {
                reader.Read();
                sb.AppendLine("namespace 命名空间");
                sb.AppendLine("{");
                sb.Append(new string(' ', 4));
                sb.AppendLine("/// <summary>");
                sb.Append(new string(' ', 4));
                sb.AppendLine("/// 类名注释");
                sb.Append(new string(' ', 4));
                sb.AppendLine("/// </summary>");
                sb.Append(new string(' ', 4));
                sb.AppendLine("public class 你的类名");
                sb.Append(new string(' ', 4));
                sb.AppendLine("{");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var colName=reader.GetName(i);
                    var type = reader.GetDataTypeName(i);
                    var isNullable = reader.IsDBNull(i);
                    sb.Append(new string(' ', 8));
                    var typeNameMe = isNullable? $"{type}?" : $"{type}";
                    string attrVisible = ";";
                    if (config.CheckList is {Count:>0})
                    {
                        if (config.CheckList.Count==2)
                        {
                            attrVisible = " { get;set; } ";
                        }
                        else
                        {
                            if (config.CheckList.Contains("get"))
                            {
                                attrVisible = " { get; } ";
                            }   
                            if (config.CheckList.Contains("set"))
                            {
                                attrVisible = " { set; } ";
                            }      
                        }
                    }
                    sb.AppendLine("public " + typeNameMe + " " + colName+attrVisible);
                }
                sb.Append(new string(' ', 4));
                sb.AppendLine("}");
                sb.AppendLine("}");
            }
        }

        return ChangeWords(sb.ToString());
    }
    /// <summary>
    /// 显示系统有权访问的数据库
    /// </summary>
    /// <returns></returns>
    [HttpGet("ShowDataBases")]
    public IEnumerable<string> ShowDataBases()
    {
        if (_conn==null)
        {
            throw new Exception("请先进行数据库连接");
        }
        using (_conn=new SqlConnection(_connString))
        {
            var sql = "use master;select name from sysdatabases order by name asc";
            return _conn.Query<string>(sql);
        }
    }
    /// <summary>
    /// 获取指定表生成的实体类
    /// </summary>
    /// <returns></returns>
    [HttpPost("GetEntityByTableName")]
    public string GetEntityByTableName([FromForm]string tableName,[FromForm]AdvanceConfig config)
    {
        if (_conn==null)
        {
            throw new Exception("请先进行数据库连接");
        }
        List<TableDetails>? list = null;
        using (_conn=new SqlConnection(_connString))
        {
            var sql =@"select syscolumns.name as ColName,systypes.name as TypeName,
                sys.extended_properties.value as Description,sysobjects.name
                as TableName,syscolumns.isnullable as IsNullable from syscolumns
                inner join sysobjects on syscolumns.id=sysobjects.id
                inner join systypes on syscolumns.xtype=systypes.xtype
                left join sys.extended_properties on sys.extended_properties.major_id=syscolumns.id
                and sys.extended_properties.minor_id=syscolumns.colorder
                where sysobjects.name=@tableName and systypes.name<>'sysname'
                order by syscolumns.name asc";
            var data=_conn.Query<TableDetails>(sql,new {tableName=tableName});
            list = new List<TableDetails>(data);
        }
        StringBuilder sb = new StringBuilder();
        if (list.Count>0)
        {
            tableName = tableName.Substring(0, 1).ToUpper() + tableName.Substring(1);
            sb.AppendLine("namespace 命名空间");
            sb.AppendLine("{");
            sb.Append(new string(' ', 4));
            sb.AppendLine("/// <summary>");
            sb.Append(new string(' ', 4));
            sb.AppendLine("/// "+tableName);
            sb.Append(new string(' ', 4));
            sb.AppendLine("/// </summary>");
            sb.Append(new string(' ', 4));
            sb.AppendLine("public class " + tableName);
            sb.Append(new string(' ', 4));
            sb.AppendLine("{");
            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    sb.Append(new string(' ', 8));
                    sb.AppendLine("/// <summary>");
                    sb.Append(new string(' ', 8));
                    sb.AppendLine("///"+item.Description);
                    sb.Append(new string(' ', 8));
                    sb.AppendLine("/// </summary>");
                }
                var typeNameMe = item.IsNullable == 1 ? $"{item.TypeName}?" : $"{item.TypeName}";
                sb.Append(new string(' ', 8));
                sb.AppendLine($"[Column(\"{item.ColName}\")]");
                sb.Append(new string(' ', 8));
                string attrVisible =";";
                if (config.CheckList is {Count:>0})
                {
                    if (config.CheckList.Count==2)
                    {
                        attrVisible = " { get;set; } ";
                    }
                    else
                    {
                        if (config.CheckList.Contains("get"))
                        {
                            attrVisible = " { get; } ";
                        }   
                        if (config.CheckList.Contains("set"))
                        {
                            attrVisible = " { set; } ";
                        }      
                    }
                }
                sb.AppendLine("public " + typeNameMe + " " + item.ColName+attrVisible);
            }
            sb.Append(new string(' ', 4));
            sb.AppendLine("}");
            sb.AppendLine("}");
        }
        return ChangeWords(sb.ToString());
    }
    private string ChangeWords(string content)
    {
        string result = Regex.Replace(content, "nvarchar\\?", "string?");
        result = Regex.Replace(result, "varchar\\?", "string?");
        result = Regex.Replace(result, "nchar\\?", "string?");
        result = Regex.Replace(result, "char\\?", "string?");
        result = Regex.Replace(content, "nvarchar", "string");
        result = Regex.Replace(result, "varchar", "string");
        result = Regex.Replace(result, "nchar", "string");
        result = Regex.Replace(result, "char", "string");
        result = Regex.Replace(result, "uniqueidentifier", "Guid");
        result = Regex.Replace(result, "tinyint", "int");
        result = Regex.Replace(result, "smallint", "int");
        result = Regex.Replace(result, "bigint", "int");
        result = Regex.Replace(result, "datetime", "DateTime");
        result = Regex.Replace(result, "text", "string");
        result = Regex.Replace(result, "bit\\?", "bool?");
        return result;
    }
    [HttpPost("ShowTables")]
    public IEnumerable<string> ShowTables([FromForm]string database)
    {
        if (_conn==null)
        {
            throw new Exception("请先进行数据库连接");
        }
        using (_conn=new SqlConnection(_connString))
        {
            var sql = $"use {database};select name from sysobjects where xtype='U' order by name asc";
            return _conn.Query<string>(sql);
        }
    }
    [HttpPost("Generate")]
    public IActionResult Generate([FromForm]string className,[FromForm]string path,[FromForm] string top,[FromForm]string classContent)
    {
        try
        {
            
            if (!Directory.Exists(path))
            {
                return Content("路径不存在");
            }
            string filePath = Path.Combine(path.Trim(), className + ".cs");
            if (System.IO.File.Exists(filePath))
            {
                return Content("文件已存在"); 
            }
            using (StreamWriter sw=new StreamWriter(filePath,false,Encoding.Default))
            {
                sw.Write(top);
                sw.Write("\r\n\r\n");
                sw.Write(classContent);
                sw.Flush();
                sw.Close();
            }
            return Ok("生成成功");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}