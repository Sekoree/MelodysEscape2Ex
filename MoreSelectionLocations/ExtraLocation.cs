using System.Xml.Serialization;

namespace MoreSelectionLocations
{
    [XmlRoot("extraLocation")]
    public class ExtraLocation
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "path")]
        public string Path { get; set; }
    }
}