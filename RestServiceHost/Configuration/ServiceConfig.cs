using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RestServiceHost.Configuration
{
    [XmlRoot("Configuration", Namespace = "http://www.voxsola.se/ServiceHost/ServiceConfig.xsd")]
    public class ServiceConfig
    {
        [XmlArray("Assemblies")]
        [XmlArrayItem("Assembly")]
        public List<Assembly> Assemblies { get; set; }

        [XmlArray("Services")]
        [XmlArrayItem("Service")]
        public List<Service> Services { get; set; }

        public static ServiceConfig Load(string file)
        {
            return ServiceConfig.Load(File.OpenRead(file));
        }
        public static ServiceConfig Load(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(ServiceConfig));
            return (ServiceConfig)serializer.Deserialize(stream);
        }
    }
    public class Assembly
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Path")]
        public string Path { get; set; }
    }
    public class Service
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Controllers")]
        [XmlArrayItem("Controller")]
        public List<Controller> Controllers { get; set; }

        [XmlArray("Urls")]
        [XmlArrayItem("Url")]
        public List<string> Urls { get; set; }
    }
    public class Controller
    {
        [XmlAttribute("Assembly")]
        public string Assembly { get; set; }

        [XmlAttribute("FullyQualifiedName")]
        public string FullyQualifiedName { get; set; }
    }
}
