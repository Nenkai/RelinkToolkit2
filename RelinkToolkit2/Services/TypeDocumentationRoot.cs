using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;
using System.ComponentModel;

namespace RelinkToolkit2.Services;

[XmlRoot("Types", IsNullable = false)]
public class TypeDocumentationRoot
{
    [XmlElement("Component")]
    public List<TypeDocumentation> Components { get; set; } = [];
}

public class TypeDocumentation
{
    /// <summary>
    /// Name of the type
    /// </summary>
    [XmlAttribute]
    public required string Name { get; set; }

    /// <summary>
    /// Description for this type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Previewing format string
    /// </summary>
    public string? PreviewFormatString { get; set; }

    /// <summary>
    /// Child Types
    /// </summary>
    public List<TypeDocumentation> ChildTypes { get; set; } = [];

    /// <summary>
    /// Properties
    /// </summary>
    [XmlArrayItem("Property", IsNullable = false)]
    public List<TypePropertyDocumentation> Properties { get; set; } = [];
}

public class TypePropertyDocumentation
{
    /// <summary>
    /// Property name
    /// </summary>
    [XmlAttribute]
    public required string Name { get; set; }

    /// <summary>
    /// Property description
    /// </summary>
    public string? Description { get; set; }
}