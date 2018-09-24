using System;

namespace BlogExporter.ConsoleUI.Models
{
    public class Blob
    {
        public string Name { get; set; }
        public Byte[] Data { get; set; }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
