using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace BenchSuite.Models;

/// <summary>
/// 试卷导出数据传输对象 - 支持JSON和XML格式
/// </summary>
[XmlRoot("ExamProject")]
public class ExamExportModel
{
    /// <summary>
    /// 试卷信息
    /// </summary>
    [JsonPropertyName("exam")]
    [XmlElement("Exam")]
    public ExamModel Exam { get; set; } = new();

    /// <summary>
    /// 导出元数据
    /// </summary>
    [JsonPropertyName("metadata")]
    [XmlElement("Metadata")]
    public ExportMetadata Metadata { get; set; } = new();
}

/// <summary>
/// 导出元数据
/// </summary>
public class ExportMetadata
{
    /// <summary>
    /// 导出版本
    /// </summary>
    [JsonPropertyName("exportVersion")]
    [XmlElement("ExportVersion")]
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// 导出日期
    /// </summary>
    [JsonPropertyName("exportDate")]
    [XmlElement("ExportDate")]
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导出者
    /// </summary>
    [JsonPropertyName("exportedBy")]
    [XmlElement("ExportedBy")]
    public string ExportedBy { get; set; } = "ExamLab";

    /// <summary>
    /// 总科目数
    /// </summary>
    [JsonPropertyName("totalSubjects")]
    [XmlElement("TotalSubjects")]
    public int TotalSubjects { get; set; }

    /// <summary>
    /// 总题目数
    /// </summary>
    [JsonPropertyName("totalQuestions")]
    [XmlElement("TotalQuestions")]
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 总操作点数
    /// </summary>
    [JsonPropertyName("totalOperationPoints")]
    [XmlElement("TotalOperationPoints")]
    public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 导出级别
    /// </summary>
    [JsonPropertyName("exportLevel")]
    [XmlElement("ExportLevel")]
    public string ExportLevel { get; set; } = "Complete";

    /// <summary>
    /// 导出格式
    /// </summary>
    [JsonPropertyName("exportFormat")]
    [XmlElement("ExportFormat")]
    public string ExportFormat { get; set; } = "XML";
}
