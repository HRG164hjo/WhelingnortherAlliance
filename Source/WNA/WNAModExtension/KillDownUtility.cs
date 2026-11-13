using System.Collections.Generic;
using System.Xml;
using Verse;

namespace WNA.WNAModExtension
{
    internal class KillDownUtility : DefModExtension
    {
        public int canDieThreshold = 5;
        public bool canEverBeDowned = true;
        public CapacityMinLevels minCapacities = null;
    }

    public class CapacityMinLevels
    {
        private readonly Dictionary<string, float> levels = new Dictionary<string, float>();

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            levels.Clear();
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                levels[node.Name] = ParseHelper.FromString<float>(node.InnerText);
            }
        }

        public bool TryGetLevel(PawnCapacityDef capacity, out float level)
        {
            if (capacity != null && levels.TryGetValue(capacity.defName, out level))
                return true;
            level = 0f;
            return false;
        }
    }
}
