using System.Collections.Generic;
using System.Text;
using System.Xml;
using log4net;

namespace ENTM.Utility
{
    public abstract class ConfigPrinter
    {
        private static readonly ILog _logger = LogManager.GetLogger("Config");

        public static void Print(XmlElement config)
        {
            List<string> lines = new List<string>();
            PrintNodeRecursive(config, lines);

            foreach (string line in lines)
            {
                _logger.Info(line);
            }
        }

        private static void PrintNodeRecursive(XmlElement node, List<string> lines)
        {
            if (node.ChildNodes.Count > 1) lines.Add(""); // Nested
            lines.Add(node.Name);

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child is XmlComment) continue;

                if (child.HasChildNodes) // Text counts as child
                {
                    PrintNodeRecursive(child as XmlElement, lines);
                }
                else if (child is XmlText)
                {
                    lines[lines.Count - 1] =  $"{lines[lines.Count - 1]} : {child.InnerText}";
                }
            }
        }
    }
}
