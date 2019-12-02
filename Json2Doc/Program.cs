using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace DocGen
{
    class Program
    {
        private enum DocumentItemType
        {
            Title,
            Index,
            Text,
            Box,
            BulletList,
            NumberedList,
            Table,
            Unknown
        }

        // E.g.:
        // This is a text [style1]{This is a text [style2]{This is a} text This is a} text This is a text This is a text This is a text 
        // This is a text <span class="style1">This is a text <span class="style2">This is a</span> text This is a</span> text This is a text This is a text This is a text 
        private static string TranslateFromStyleTagToHtml(string text)
        {
            string result = String.Empty;
            int textStart = 0;
            int tagStart = -1;
            int tagEnd = -1;
            int counter = 0;
            int i;

            for (i = 0; i < text.Length; i++)
            {
                if (text[i] == '[')
                {
                    tagStart = i;
                }
                else if (text[i] == ']')
                {
                    if (tagStart != -1)
                    {
                        tagEnd = i;
                    }
                }
                else if (text[i] == '{')
                {
                    if (tagEnd != -1 && (tagEnd + 1) == i)
                    {
                        result += text.Substring(textStart, tagStart - textStart);
                        result += $"<span class=\"{text.Substring(tagStart + 1, tagEnd - tagStart - 1)}\">";
                        counter++;
                        tagStart = -1;
                        tagEnd = -1;
                        textStart = i + 1;
                    }
                }
                else if (text[i] == '}')
                {
                    if (counter > 0)
                    {
                        if (text[i - 1] != '\\')
                        {
                            result += text.Substring(textStart, i - textStart);
                            result += "</span>";
                            counter--;
                            textStart = i + 1;
                        }
                    }
                }
            }

            if (textStart < i)
            {
                result += text.Substring(textStart, i - textStart);
            }

            while (counter-- > 0)
            {
                result += "</p>";
            }

            return result;
        }

        private static string TranslateFromLinkTagToHTml(string text)
        {
            string result = String.Empty;
            string linkText = String.Empty;
            int textStart = 0;
            int tagStart = -1;
            int tagEnd = -1;
            int urlStart = -1;
            int i;

            for (i = 0; i < text.Length; i++)
            {
                if (text[i] == '[')
                {
                    if (i == 0 || text[i - 1] != '\\')
                    {
                        if (tagStart == -1)
                        {
                            tagStart = i;
                        }
                        else
                        {
                            tagStart = -1;
                            tagEnd = -1;
                            urlStart = -1;
                        }
                    }
                }
                else if (text[i] == ']')
                {
                    if (tagStart != -1 && text[i - 1] != '\\')
                    {
                        if (tagEnd == -1)
                        {
                            tagEnd = i;
                        }
                        else
                        {
                            tagStart = -1;
                            tagEnd = -1;
                            urlStart = -1;
                        }
                    }
                }
                else if (text[i] == '(')
                {
                    if (tagEnd != -1 /* Tag already found and ... */ && urlStart == -1 /* ... Url not found yet */ )
                    {
                        if ((tagEnd + 1) == i)
                        {
                            urlStart = i;
                        }
                        else
                        {
                            tagStart = -1;
                            tagEnd = -1;
                        }
                    }
                }
                else if (text[i] == ')')
                {
                    if (urlStart != -1)
                    {
                        if (text[i - 1] != '\\')
                        {
                            result += text.Substring(textStart, tagStart);
                            result += "<a href=\"" + text.Substring(urlStart + 1, i - urlStart - 1) + "\">";
                            result += text.Substring(tagStart + 1, tagEnd - tagStart - 1);
                            result += "</a>";

                            tagStart = -1;
                            tagEnd = -1;
                            urlStart = -1;

                            textStart = i + 1;
                        }
                    }
                }
            }

            if (textStart < i)
            {
                result += text.Substring(textStart, i - textStart);
            }

            return result;
        }

        private static string UnescapeString(string text)
        {
            string result = String.Empty;
            int textStart = 0;
            int i;
            int previousBackSlash = -1;

            for (i = 1; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    previousBackSlash = i;
                }
                else if (previousBackSlash == (i - 1))
                {
                    char[] specialChars = { '[', ']', '(', ')', '{', '}' };

                    foreach (char specialChar in specialChars)
                    {
                        if (text[i] == specialChar)
                        {
                            result += text.Substring(textStart, i - textStart - 1);
                            result += specialChar;
                            previousBackSlash = -1;
                            textStart = i + 1;
                            break;
                        }
                    }
                }
            }

            if (textStart < i)
            {
                result += text.Substring(textStart, i - textStart);
            }

            return result;
        }

        private static string TranslateSpecialSyntaxToHTml(string text)
        {
            text = TranslateFromLinkTagToHTml(text);
            text = TranslateFromStyleTagToHtml(text);
            text = UnescapeString(text);
            return text;
        }

        private static void RenderDocumentItem(DocumentItemType docItemType, JToken json, StreamWriter sw)
        {
            if (docItemType == DocumentItemType.Text)
            {
                JToken content = json["content"];

                if (content != null)
                {
                    string style = (string)json["style"] ?? "text";

                    if (content.Type == JTokenType.String)
                    {
                        sw.WriteLine($"<span class=\"{style}\">{TranslateSpecialSyntaxToHTml((string)content)}</span>");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid content type for text element ({content.Type})");
                    }
                }
            }
            else if (docItemType == DocumentItemType.Title)
            {
                JToken content = json["content"];

                if (content != null)
                {
                    string level = (string)json["level"];
                    uint number;

                    if (!uint.TryParse(level, out number))
                    {
                        number = 0;
                    }

                    if (content.Type == JTokenType.String)
                    {
                        sw.Write($"<p class=\"title_level{number}\">{TranslateSpecialSyntaxToHTml((string)content)}</p>");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid content type for title element ({content.Type})");
                    }
                }
            }
            else if (docItemType == DocumentItemType.Box)
            {
                JToken content = json["content"];

                if (content != null)
                {
                    string style = (string)json["style"];

                    sw.Write($"<pre class=\"{style}\">");

                    if (content.Type == JTokenType.Array)
                    {
                        foreach (var contentItem in content.Children())
                        {
                            RenderDocumentItem(contentItem, sw);
                        }
                    }
                    else if (content.Type == JTokenType.Object)
                    {
                        RenderDocumentItem(content, sw);
                    }

                    sw.Write("</pre>");
                }
            }
            else if (docItemType == DocumentItemType.Index)
            {

            }
            else if (docItemType == DocumentItemType.BulletList)
            {
                JToken content = json["content"];

                if (content != null)
                {
                    string style = (string)json["style"] ?? "defaultBulletList";

                    sw.Write($"<ul class=\"{style}\">");

                    if (content.Type == JTokenType.Array)
                    {
                        foreach (var contentItem in content.Children())
                        {
                            sw.Write($"<li>");
                            RenderDocumentItem(contentItem, sw);
                            sw.Write($"</li>");
                        }
                    }

                    sw.Write("</ul>");
                }
            }
            else if (docItemType == DocumentItemType.NumberedList)
            {
                JToken content = json["content"];

                if (content != null)
                {
                    string style = (string)json["style"] ?? "1";

                    sw.Write($"<ol type=\"{style}\">");

                    if (content.Type == JTokenType.Array)
                    {
                        foreach (var contentItem in content.Children())
                        {
                            sw.Write($"<li>");
                            RenderDocumentItem(contentItem, sw);
                            sw.Write($"</li>");
                        }
                    }

                    sw.Write("</ol>");
                }
            }
            else if (docItemType == DocumentItemType.Table)
            {
                string style = (string)json["style"];

                sw.Write($"<table class=\"{style}\">");

                JToken headers = json["headers"];

                if (headers != null && headers.Type == JTokenType.Array)
                {
                    sw.Write($"<tr>");

                    foreach (var contentItem in headers.Children())
                    {
                        sw.Write($"<th>");
                        RenderDocumentItem(contentItem, sw);
                        sw.Write($"</th>");
                    }

                    sw.Write($"</tr>");
                }

                JToken rows = json["rows"];

                if (rows != null && rows.Type == JTokenType.Array)
                {
                    foreach (var row in rows.Children())
                    {
                        sw.Write($"<tr>");

                        JToken cells = row["row"];

                        if (cells != null)
                        {
                            foreach (var cell in cells.Children())
                            {
                                sw.Write($"<td>");
                                RenderDocumentItem(cell, sw);
                                sw.Write($"</td>");
                            }
                        }

                        sw.Write($"</tr>");
                    }
                }

                sw.Write("</table>");
            }
        }

        private static DocumentItemType GetDocumentItemType(string typeString)
        {
            DocumentItemType type = DocumentItemType.Unknown;

            if (typeString != null)
            {
                if (String.Equals(typeString, "text"))
                {
                    type = DocumentItemType.Text;
                }
                else if (String.Equals(typeString, "title"))
                {
                    type = DocumentItemType.Title;
                }
                else if (String.Equals(typeString, "box"))
                {
                    type = DocumentItemType.Box;
                }
                else if (String.Equals(typeString, "index"))
                {
                    type = DocumentItemType.Index;
                }
                else if (String.Equals(typeString, "bullet-list"))
                {
                    type = DocumentItemType.BulletList;
                }
                else if (String.Equals(typeString, "numbered-list"))
                {
                    type = DocumentItemType.NumberedList;
                }
                else if (String.Equals(typeString, "table"))
                {
                    type = DocumentItemType.Table;
                }
            }

            return type;
        }

        private static void RenderDocumentItem(JToken json, StreamWriter sw)
        {
            DocumentItemType itemType = GetDocumentItemType((string)json["type"]);

            if (itemType != DocumentItemType.Unknown)
            {
                RenderDocumentItem(itemType, json, sw);
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new Exception("Invalid number of arguments");
            }

            if (!File.Exists(args[0]))
            {
                throw new Exception($"Cannot access file '{args[0]}'");
            }

            if (!File.Exists(args[1]))
            {
                throw new Exception($"Cannot access file '{args[1]}'");
            }

            FileStream fw = new FileStream("document.html", FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fw);
            sw.AutoFlush = true;

            sw.Write("<html><title>Document</title>");
            
            sw.Write("<style>");
            sw.Write(File.ReadAllText(args[1]));
            sw.Write("</style>");
            sw.Write("<body>");

            JArray json = JArray.Parse(File.ReadAllText(args[0]));

            foreach (var item in json.Children())
            {
                RenderDocumentItem(item, sw);
            }

            sw.Write("</body></html>");

            sw.Close();
        }
    }
}
