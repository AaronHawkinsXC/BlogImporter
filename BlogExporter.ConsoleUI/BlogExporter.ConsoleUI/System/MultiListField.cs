using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BlogExporter.ConsoleUI.System
{
    public class MultiListField : Field
    {
        public MultiListField(XElement xmlDoc)
            : base(xmlDoc)
        { }

        public IEnumerable<string> IdList => Value.Split('|');

        public void AddItemsFromMultiListField(MultiListField fieldToCombineWith)
        {
            if (fieldToCombineWith == null)
                return;

            var currentItems = IdList.ToList();
            currentItems.AddRange(fieldToCombineWith.IdList);
            Value = string.Join("|", currentItems);
        }
    }
}
