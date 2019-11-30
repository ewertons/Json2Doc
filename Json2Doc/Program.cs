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

        private static void RenderDocumentItem(DocumentItemType docItemType, JToken json, StreamWriter sw, string divClassName)
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

        private static void RenderDocumentItem(JToken json, StreamWriter sw)
        {
            DocumentItemType itemType = GetDocumentItemType((string)json["type"]);

            if (itemType != DocumentItemType.Unknown)
            {
                RenderDocumentItem(itemType, json, sw, "title");
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
