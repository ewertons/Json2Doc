using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace DocGen
{
    class Program
    {
        static void RenderDocumentDivItem(JToken json, StreamWriter sw, string divClassName)
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

        static void RenderDocumentItem(JToken json, StreamWriter sw)
        {
            if (String.Equals((string)json["type"], "text"))
            {
                JToken content = json["content"];

                if (content != null)
                {
                    if (content.Type == JTokenType.String)
                    {
                        sw.Write($"<p class=\"text\">{(string)content}</p>");
                    }
                }
            }
            else if (String.Equals((string)json["type"], "title"))
            {
                RenderDocumentDivItem(json, sw, "title");
            }
            else if (String.Equals((string)json["type"], "overview"))
            {
                RenderDocumentDivItem(json, sw, "overview");
            }
            else if (String.Equals((string)json["type"], "code"))
            {
                RenderDocumentDivItem(json, sw, "code");
            }
            else if (String.Equals((string)json["type"], "console"))
            {
                RenderDocumentDivItem(json, sw, "console");
            }
            else if (String.Equals((string)json["type"], "command"))
            {
                RenderDocumentDivItem(json, sw, "command");
            }
            else if (String.Equals((string)json["type"], "output"))
            {
                RenderDocumentDivItem(json, sw, "output");
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
