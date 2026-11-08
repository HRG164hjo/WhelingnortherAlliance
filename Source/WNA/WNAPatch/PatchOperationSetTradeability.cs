using System.Collections.Generic;
using System.Xml;
using Verse;

namespace WNA.WNAPatch
{
    // XML patches run before XmlInheritance.Resolve, so a def's own destroyOnDrop node alone is not
    // enough: abstract bases propagate their tradeability to children that add destroyOnDrop, and
    // concrete defs may inherit destroyOnDrop from a parent. Resolve ParentName chains here instead.
    public class PatchOperationSetTradeability : PatchOperation
    {
        private const string TradeabilityAll = "All";

        protected override bool ApplyWorker(XmlDocument xml)
        {
            XmlNodeList thingDefs = xml.SelectNodes("Defs/ThingDef");
            if (thingDefs == null)
            {
                return false;
            }

            Dictionary<string, XmlElement> defsByName = new Dictionary<string, XmlElement>();
            for (int i = 0; i < thingDefs.Count; i++)
            {
                XmlElement def = thingDefs[i] as XmlElement;
                string name = def?.GetAttribute("Name");
                if (!string.IsNullOrEmpty(name) && !defsByName.ContainsKey(name))
                {
                    defsByName.Add(name, def);
                }
            }

            HashSet<XmlElement> destroyOnDropDefs = new HashSet<XmlElement>();
            for (int i = 0; i < thingDefs.Count; i++)
            {
                XmlElement def = thingDefs[i] as XmlElement;
                if (def == null)
                {
                    continue;
                }
                List<XmlElement> chain = HierarchyFor(def, defsByName);
                for (int j = 0; j < chain.Count; j++)
                {
                    if (DeclaresDestroyOnDrop(chain[j]))
                    {
                        destroyOnDropDefs.UnionWith(chain);
                        break;
                    }
                }
            }

            bool changed = false;
            for (int i = 0; i < thingDefs.Count; i++)
            {
                XmlElement def = thingDefs[i] as XmlElement;
                if (def == null || destroyOnDropDefs.Contains(def))
                {
                    continue;
                }
                XmlElement tradeability = def["tradeability"];
                if (tradeability == null)
                {
                    continue;
                }
                tradeability.InnerText = TradeabilityAll;
                changed = true;
            }
            return changed;
        }

        private static List<XmlElement> HierarchyFor(XmlElement def, Dictionary<string, XmlElement> defsByName)
        {
            List<XmlElement> chain = new List<XmlElement>();
            HashSet<XmlElement> visited = new HashSet<XmlElement>();
            XmlElement current = def;
            while (current != null && visited.Add(current))
            {
                chain.Add(current);
                string parentName = current.GetAttribute("ParentName");
                if (string.IsNullOrEmpty(parentName) || !defsByName.TryGetValue(parentName, out current))
                {
                    break;
                }
            }
            return chain;
        }

        private static bool DeclaresDestroyOnDrop(XmlElement def)
        {
            XmlElement node = def["destroyOnDrop"];
            return node != null && bool.TryParse(node.InnerText.Trim(), out bool value) && value;
        }
    }
}
