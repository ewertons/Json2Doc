using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace DocGen
{
    class Program
    {
        enum DocumentItemType
        {
            Title,
            Overview,
            Text,
            Console,
            Command,
            Output,
            Code,
            BulletList,
            NumberedList,
            Unknown
        }

        // E.g.:
        // This is a text [style1]{This is a text [style2]{This is a} text This is a} text This is a text This is a text This is a text 
        // This is a text <span class="style1">This is a text <span class="style2">This is a</span> text This is a</span> text This is a text This is a text This is a text 

        static string TranslateFromStyleTagToHtml(string text)
        {
            string result = "";
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
                        else
                        {
                            result += text.Substring(textStart, i - 1 - textStart);
                            textStart = i;
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

        static string TranslateFromLinkTagToHTml(string text)
        {
            string result = "";
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
                        else
                        {
                            result += text.Substring(textStart, i - 1 - textStart);
                            textStart = i;
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

        static string TranslateSpecialSyntaxToHTml(string text)
        {
            return TranslateFromStyleTagToHtml(text);
        }

        static void RenderDocumentItem(DocumentItemType docItemType, JToken json, StreamWriter sw, string divClassName)
        {
            if (docItemType == DocumentItemType.Text)
            {
                JToken content = json["content"];

                if (content != null)
                {
                    if (content.Type == JTokenType.String)
                    {
                        sw.Write($"<p class=\"text\">{TranslateSpecialSyntaxToHTml((string)content)}</p>");
                    }
                }
            }
            else
            {
                sw.Write($"<div class=\"{divClassName}\">");

                JToken content = json["content"];

                if (content != null)
                {
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
                }

                sw.Write("</div>");
            }

        }

        static DocumentItemType GetDocumentItemType(string typeString)
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
                else if (String.Equals(typeString, "overview"))
                {
                    type = DocumentItemType.Overview;
                }
                else if (String.Equals(typeString, "command"))
                {
                    type = DocumentItemType.Command;
                }
                else if (String.Equals(typeString, "output"))
                {
                    type = DocumentItemType.Output;
                }
                else if (String.Equals(typeString, "console"))
                {
                    type = DocumentItemType.Console;
                }
                else if (String.Equals(typeString, "bullet-list"))
                {
                    type = DocumentItemType.BulletList;
                }
                else if (String.Equals(typeString, "numbered-list"))
                {
                    type = DocumentItemType.NumberedList;
                }
            }

            return type;
        }

        static void RenderDocumentItem(JToken json, StreamWriter sw)
        {
            DocumentItemType itemType = GetDocumentItemType((string)json["type"]);

            if (itemType != DocumentItemType.Unknown)
            {
                RenderDocumentItem(itemType, json, sw, "title");
            }
        }

        static void Main(string[] args)
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
