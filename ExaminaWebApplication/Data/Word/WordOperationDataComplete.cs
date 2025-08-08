using ExaminaWebApplication.Models.Word;

namespace ExaminaWebApplication.Data.Word;

/// <summary>
/// Word完整操作点数据定义类 - 包含所有67个操作点
/// </summary>
public static class WordOperationDataComplete
{
    /// <summary>
    /// 获取所有Word操作点定义（共67项）
    /// </summary>
    /// <returns></returns>
    public static List<WordOperationPoint> GetWordOperationPoints()
    {
        return new List<WordOperationPoint>
        {
            // 第一类：段落操作（共14项）
            // （一）段落文字样式（字体、字号、字形、颜色等）
            new WordOperationPoint
            {
                Id = 1, OperationNumber = 1, Name = "设置段落的字体",
                Description = "设置指定段落的字体类型", Category = WordOperationCategory.ParagraphTextStyle
            },
            new WordOperationPoint
            {
                Id = 2, OperationNumber = 2, Name = "设置段落的字号",
                Description = "设置指定段落的字体大小", Category = WordOperationCategory.ParagraphTextStyle
            },
            new WordOperationPoint
            {
                Id = 3, OperationNumber = 3, Name = "设置段落的字形",
                Description = "设置指定段落的字体样式（粗体、斜体等）", Category = WordOperationCategory.ParagraphTextStyle
            },
            new WordOperationPoint
            {
                Id = 4, OperationNumber = 4, Name = "设置段落字间距",
                Description = "设置指定段落的字符间距", Category = WordOperationCategory.ParagraphTextStyle
            },
            new WordOperationPoint
            {
                Id = 5, OperationNumber = 5, Name = "设置段落文字的颜色",
                Description = "设置指定段落文字的RGB颜色", Category = WordOperationCategory.ParagraphTextStyle
            },

            // （二）段落格式设置（文字位置与布局）
            new WordOperationPoint
            {
                Id = 6, OperationNumber = 6, Name = "设置段落对齐方式",
                Description = "设置指定段落的对齐方式", Category = WordOperationCategory.ParagraphFormat
            },
            new WordOperationPoint
            {
                Id = 7, OperationNumber = 7, Name = "设置段落缩进",
                Description = "设置指定段落的首行缩进、左缩进、右缩进", Category = WordOperationCategory.ParagraphFormat
            },
            new WordOperationPoint
            {
                Id = 8, OperationNumber = 8, Name = "设置行间距",
                Description = "设置指定段落的行间距值", Category = WordOperationCategory.ParagraphFormat
            },
            new WordOperationPoint
            {
                Id = 9, OperationNumber = 9, Name = "首字下沉",
                Description = "设置指定段落的首字下沉形式", Category = WordOperationCategory.ParagraphFormat
            },

            // （三）段落间距与边框
            new WordOperationPoint
            {
                Id = 10, OperationNumber = 10, Name = "设置段落间距",
                Description = "设置指定段落的段前间距和段后间距", Category = WordOperationCategory.ParagraphSpacingBorder
            },
            new WordOperationPoint
            {
                Id = 11, OperationNumber = 11, Name = "设置段落边框的颜色",
                Description = "设置指定段落边框的颜色值", Category = WordOperationCategory.ParagraphSpacingBorder
            },
            new WordOperationPoint
            {
                Id = 12, OperationNumber = 12, Name = "设置段落边框的线型",
                Description = "设置指定段落边框的线条样式", Category = WordOperationCategory.ParagraphSpacingBorder
            },
            new WordOperationPoint
            {
                Id = 13, OperationNumber = 13, Name = "设置段落边框的线宽",
                Description = "设置指定段落边框的线条宽度", Category = WordOperationCategory.ParagraphSpacingBorder
            },

            // （四）段落背景设置
            new WordOperationPoint
            {
                Id = 14, OperationNumber = 14, Name = "设置段落底纹",
                Description = "设置指定段落的底纹图案、前景色、背景色", Category = WordOperationCategory.ParagraphBackground
            },

            // 第二类：页面设置（共15项）
            // （一）页面尺寸与边距设置
            new WordOperationPoint
            {
                Id = 15, OperationNumber = 15, Name = "设置纸张大小",
                Description = "设置文档的纸张类型和尺寸", Category = WordOperationCategory.PageSetup
            },
            new WordOperationPoint
            {
                Id = 16, OperationNumber = 16, Name = "设置页边距",
                Description = "设置文档的上下左右页边距", Category = WordOperationCategory.PageSetup
            },

            // （二）页眉设置
            new WordOperationPoint
            {
                Id = 17, OperationNumber = 17, Name = "设置页眉中的文字",
                Description = "设置页眉的文字内容", Category = WordOperationCategory.HeaderFooter
            },
            new WordOperationPoint
            {
                Id = 18, OperationNumber = 18, Name = "页眉中文字的字体",
                Description = "设置页眉文字的字体类型", Category = WordOperationCategory.HeaderFooter
            },
            new WordOperationPoint
            {
                Id = 19, OperationNumber = 19, Name = "页眉中文字的字号",
                Description = "设置页眉文字的字号大小", Category = WordOperationCategory.HeaderFooter
            },
            new WordOperationPoint
            {
                Id = 20, OperationNumber = 20, Name = "页眉中文字的对齐方式",
                Description = "设置页眉文字的对齐方式", Category = WordOperationCategory.HeaderFooter
            },

            // （三）页脚设置
            new WordOperationPoint
            {
                Id = 21, OperationNumber = 21, Name = "设置页脚中的文字",
                Description = "设置页脚的文字内容", Category = WordOperationCategory.HeaderFooter
            },
            new WordOperationPoint
            {
                Id = 22, OperationNumber = 22, Name = "页脚中文字的字体",
                Description = "设置页脚文字的字体类型", Category = WordOperationCategory.HeaderFooter
            },
            new WordOperationPoint
            {
                Id = 23, OperationNumber = 23, Name = "页脚中文字的字号",
                Description = "设置页脚文字的字号大小", Category = WordOperationCategory.HeaderFooter
            },
            new WordOperationPoint
            {
                Id = 24, OperationNumber = 24, Name = "页脚中文字的对齐方式",
                Description = "设置页脚文字的对齐方式", Category = WordOperationCategory.HeaderFooter
            },

            // （四）页码与背景设置
            new WordOperationPoint
            {
                Id = 25, OperationNumber = 25, Name = "设置页码",
                Description = "为文档设置页码", Category = WordOperationCategory.PageNumberBackground
            },
            new WordOperationPoint
            {
                Id = 26, OperationNumber = 26, Name = "设置页面背景",
                Description = "设置页面的背景颜色或纹理", Category = WordOperationCategory.PageNumberBackground
            },

            // （五）页面边框设置
            new WordOperationPoint
            {
                Id = 27, OperationNumber = 27, Name = "页面边框的颜色",
                Description = "设置页面边框的颜色值", Category = WordOperationCategory.PageBorder
            },
            new WordOperationPoint
            {
                Id = 28, OperationNumber = 28, Name = "页面边框的线型",
                Description = "设置页面边框的线条样式", Category = WordOperationCategory.PageBorder
            },
            new WordOperationPoint
            {
                Id = 29, OperationNumber = 29, Name = "页面边框的线宽",
                Description = "设置页面边框的线条宽度", Category = WordOperationCategory.PageBorder
            },

            // 第三类：水印设置（共4项）
            new WordOperationPoint
            {
                Id = 30, OperationNumber = 30, Name = "设置水印的文字",
                Description = "设置水印的文字内容", Category = WordOperationCategory.Watermark
            },
            new WordOperationPoint
            {
                Id = 31, OperationNumber = 31, Name = "水印文字的字体",
                Description = "设置水印文字的字体类型", Category = WordOperationCategory.Watermark
            },
            new WordOperationPoint
            {
                Id = 32, OperationNumber = 32, Name = "水印文字的字号",
                Description = "设置水印文字的字号大小", Category = WordOperationCategory.Watermark
            },
            new WordOperationPoint
            {
                Id = 33, OperationNumber = 33, Name = "设置水印中文字的水平或倾斜",
                Description = "设置水印文字的倾斜角度", Category = WordOperationCategory.Watermark
            },

            // 第四类：项目符号与编号（共1项）
            new WordOperationPoint
            {
                Id = 34, OperationNumber = 34, Name = "设置项目编号",
                Description = "为指定段落设置项目符号或编号", Category = WordOperationCategory.ListNumbering
            },

            // 第五类：表格操作（共10项）
            new WordOperationPoint
            {
                Id = 35, OperationNumber = 35, Name = "设置表格的行数和列数",
                Description = "创建指定行数和列数的表格", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 36, OperationNumber = 36, Name = "设置表格底纹",
                Description = "设置表格指定区域的底纹颜色", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 37, OperationNumber = 37, Name = "设置表格行高",
                Description = "设置表格指定行的高度", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 38, OperationNumber = 38, Name = "设置表格列宽",
                Description = "设置表格指定列的宽度", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 39, OperationNumber = 39, Name = "设置单元格内容",
                Description = "设置表格指定单元格的内容", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 40, OperationNumber = 40, Name = "设置表格单元格对齐方式",
                Description = "设置表格指定区域单元格的对齐方式", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 41, OperationNumber = 41, Name = "设置整个表格对齐方式",
                Description = "设置整个表格的对齐方式", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 42, OperationNumber = 42, Name = "合并单元格",
                Description = "合并表格指定区域的单元格", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 43, OperationNumber = 43, Name = "设置表头第一个单元格的内容",
                Description = "设置表头指定单元格的内容", Category = WordOperationCategory.Table
            },
            new WordOperationPoint
            {
                Id = 44, OperationNumber = 44, Name = "设置表头第一个单元格的对齐方式",
                Description = "设置表头指定单元格的对齐方式", Category = WordOperationCategory.Table
            },

            // 第六类：图形和图片设置（共16项）
            // （一）自选图形插入与样式设置
            new WordOperationPoint
            {
                Id = 45, OperationNumber = 45, Name = "插入自选图形类型",
                Description = "插入指定类型的自选图形", Category = WordOperationCategory.Shape
            },
            new WordOperationPoint
            {
                Id = 46, OperationNumber = 46, Name = "设置自选图形大小",
                Description = "设置自选图形的宽度和高度", Category = WordOperationCategory.Shape
            },
            new WordOperationPoint
            {
                Id = 47, OperationNumber = 47, Name = "设置自选图形线条颜色",
                Description = "设置自选图形边框线条的颜色", Category = WordOperationCategory.Shape
            },
            new WordOperationPoint
            {
                Id = 48, OperationNumber = 48, Name = "设置自选图形填充颜色",
                Description = "设置自选图形的填充颜色", Category = WordOperationCategory.Shape
            },

            // （二）自选图形文字设置
            new WordOperationPoint
            {
                Id = 49, OperationNumber = 49, Name = "自选图形中文字大小",
                Description = "设置自选图形中文字的字号", Category = WordOperationCategory.ShapeText
            },
            new WordOperationPoint
            {
                Id = 50, OperationNumber = 50, Name = "自选图形中文字颜色",
                Description = "设置自选图形中文字的颜色", Category = WordOperationCategory.ShapeText
            },
            new WordOperationPoint
            {
                Id = 51, OperationNumber = 51, Name = "设置自选图形中文字",
                Description = "设置自选图形中的文字内容", Category = WordOperationCategory.ShapeText
            },

            // （三）自选图形位置设置
            new WordOperationPoint
            {
                Id = 52, OperationNumber = 52, Name = "设置自选图形的位置",
                Description = "设置自选图形的水平和垂直位置", Category = WordOperationCategory.ShapePosition
            },

            // （四）插入图片样式设置
            new WordOperationPoint
            {
                Id = 53, OperationNumber = 53, Name = "设置插入图片边框的复合类型",
                Description = "设置插入图片边框的复合线型", Category = WordOperationCategory.Picture
            },
            new WordOperationPoint
            {
                Id = 54, OperationNumber = 54, Name = "设置插入图片边框的短划线类型",
                Description = "设置插入图片边框的短划线样式", Category = WordOperationCategory.Picture
            },
            new WordOperationPoint
            {
                Id = 55, OperationNumber = 55, Name = "设置插入图片边框线宽",
                Description = "设置插入图片边框的线条宽度", Category = WordOperationCategory.Picture
            },
            new WordOperationPoint
            {
                Id = 56, OperationNumber = 56, Name = "设置插入图片边框颜色",
                Description = "设置插入图片边框的颜色", Category = WordOperationCategory.Picture
            },
            new WordOperationPoint
            {
                Id = 57, OperationNumber = 57, Name = "设置插入图片阴影的类型和颜色",
                Description = "设置插入图片的阴影效果和颜色", Category = WordOperationCategory.Picture
            },
            new WordOperationPoint
            {
                Id = 58, OperationNumber = 58, Name = "设置插入图片的环绕方式",
                Description = "设置插入图片的文字环绕方式", Category = WordOperationCategory.Picture
            },

            // （五）插入图片尺寸与位置设置
            new WordOperationPoint
            {
                Id = 59, OperationNumber = 59, Name = "插入图片的高度和宽度",
                Description = "设置插入图片的尺寸大小", Category = WordOperationCategory.PictureSize
            },
            new WordOperationPoint
            {
                Id = 60, OperationNumber = 60, Name = "设置插入图片的位置",
                Description = "设置插入图片的水平和垂直位置", Category = WordOperationCategory.PictureSize
            },

            // 第七类：文本框设置（共5项）
            new WordOperationPoint
            {
                Id = 61, OperationNumber = 61, Name = "设置文本框边框颜色",
                Description = "设置文本框边框的颜色", Category = WordOperationCategory.TextBox
            },
            new WordOperationPoint
            {
                Id = 62, OperationNumber = 62, Name = "设置文本框中文字",
                Description = "设置文本框中的文字内容", Category = WordOperationCategory.TextBox
            },
            new WordOperationPoint
            {
                Id = 63, OperationNumber = 63, Name = "设置文本框中文字大小",
                Description = "设置文本框中文字的字号", Category = WordOperationCategory.TextBox
            },
            new WordOperationPoint
            {
                Id = 64, OperationNumber = 64, Name = "文本框的位置",
                Description = "设置文本框的水平和垂直位置", Category = WordOperationCategory.TextBox
            },
            new WordOperationPoint
            {
                Id = 65, OperationNumber = 65, Name = "文本框环绕方式",
                Description = "设置文本框的文字环绕方式", Category = WordOperationCategory.TextBox
            },

            // 第八类：其他操作（共2项）
            new WordOperationPoint
            {
                Id = 66, OperationNumber = 66, Name = "查找和替换",
                Description = "查找指定文本并替换为新文本", Category = WordOperationCategory.Other
            },
            new WordOperationPoint
            {
                Id = 67, OperationNumber = 67, Name = "设置某一指定文字的字号",
                Description = "设置文档中指定文字的字号大小", Category = WordOperationCategory.Other
            }
        };
    }
}
