using System.Text;
using System.Xml;

namespace ENTM.Utility
{
    public abstract class ConfigPrinter
    {
        public static string Print(XmlElement config)
        {
            StringBuilder builder = new StringBuilder();
            PrintNodeRecursive(config, builder);

            return builder.Append("\n").ToString();
        }

        private static void PrintNodeRecursive(XmlElement node, StringBuilder builder)
        {
            if (node.ChildNodes.Count > 1) builder.Append("\n"); // Nested
            builder.Append($"\n{node.Name}: ");

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child is XmlComment) continue;

                if (child.HasChildNodes) // Text counts as child
                {
                    PrintNodeRecursive(child as XmlElement, builder);
                }
                else if (child is XmlText)
                {
                    builder.Append($"{child.InnerText}");
                }
            }
        }
    }
}
