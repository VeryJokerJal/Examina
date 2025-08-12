using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ExamLab.Models.ImportExport;

namespace ExamLab.Services;

/// <summary>
/// XML序列化服务 - 处理ExamExportDto的XML序列化和反序列化
/// </summary>
public static class XmlSerializationService
{
    /// <summary>
    /// 将ExamExportDto序列化为XML字符串
    /// </summary>
    /// <param name="examExportDto">要序列化的对象</param>
    /// <returns>XML字符串</returns>
    public static string SerializeToXml(ExamExportDto examExportDto)
    {
        if (examExportDto == null)
        {
            throw new ArgumentNullException(nameof(examExportDto));
        }

        XmlSerializer serializer = new XmlSerializer(typeof(ExamExportDto));
        
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using StringWriter stringWriter = new StringWriter();
        using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings);
        
        serializer.Serialize(xmlWriter, examExportDto);
        return stringWriter.ToString();
    }

    /// <summary>
    /// 从XML字符串反序列化为ExamExportDto
    /// </summary>
    /// <param name="xmlContent">XML字符串</param>
    /// <returns>反序列化的ExamExportDto对象</returns>
    public static ExamExportDto DeserializeFromXml(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new ArgumentException("XML内容不能为空", nameof(xmlContent));
        }

        XmlSerializer serializer = new XmlSerializer(typeof(ExamExportDto));
        
        using StringReader stringReader = new StringReader(xmlContent);
        using XmlReader xmlReader = XmlReader.Create(stringReader);
        
        object? result = serializer.Deserialize(xmlReader);
        if (result is ExamExportDto examExportDto)
        {
            return examExportDto;
        }
        
        throw new InvalidOperationException("XML反序列化失败：无法转换为ExamExportDto类型");
    }

    /// <summary>
    /// 验证XML内容是否为有效的ExamExportDto格式
    /// </summary>
    /// <param name="xmlContent">XML字符串</param>
    /// <returns>是否为有效格式</returns>
    public static bool IsValidExamProjectXml(string xmlContent)
    {
        try
        {
            DeserializeFromXml(xmlContent);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
