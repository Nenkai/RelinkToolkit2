using GBFRDataTools.Entities;
using GBFRDataTools.FSM.Components;

using MessagePack.Formatters;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RelinkToolkit2.Services;

public class ComponentDocumentationService : IHostedService
{
    private readonly ILogger _logger;

    public const string DOC_FILE_NAME = "component_doc.xml";

    public Dictionary<string, Component> Components { get; set; } = [];

    public ComponentDocumentationService(ILogger<ComponentDocumentationService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cts)
    {
        //DebugDump();
        LoadDocumentFile();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Components.Clear();
    }

    public string BuildPropertyKey(PropertyDescriptor propertyDescriptor)
    {
        return $"{BuildKey(propertyDescriptor.ComponentType)}.{propertyDescriptor.Name}";
    }

    public string BuildKey(Type type)
    {
        string key = "";

        while (type.IsNested)
        {
            key += $".{type.Name}";
            type = type.DeclaringType;
        }

        key = $"{type.Name}{key}";

        return key;
    }

    /// <summary>
    /// Gets a description string for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetComponentString(string key)
    {
        Component? component = GetContainingComponent(key);
        if (component is null)
            return null;

        return component.Description;
    }

    /// <summary>
    /// Gets a description string for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetPropertyString(string key)
    {
        Component? component = GetContainingComponent(key);
        if (component is null)
            return null;

        foreach (var spl in key.AsSpan().Split("."))
        {
            ReadOnlySpan<char> range = key.AsSpan(spl);

            if (spl.End.Value == key.Length)
            {
                var props = component.Properties.GetAlternateLookup<ReadOnlySpan<char>>();
                if (props.TryGetValue(range, out string? desc))
                    return desc;

                return null;
            }
        }

        return null;
    }

    private Component? GetContainingComponent(string key)
    {
        var alternate = Components.GetAlternateLookup<ReadOnlySpan<char>>();

        Component? component = null;
        foreach (var spl in key.AsSpan().Split("."))
        {
            ReadOnlySpan<char> range = key.AsSpan(spl);

            if (spl.End.Value == key.Length)
            {
                if (component is not null)
                {
                    return component;
                }
                else if (alternate.TryGetValue(key, out Component? elem))
                {
                    return elem;
                }

                return null;
            }
            else
            {
                alternate.TryGetValue(range, out component);
                if (component is null)
                    return null;
            }
        }

        return null;
    }


    public void LoadDocumentFile()
    {
        if (!File.Exists(DOC_FILE_NAME))
        {
            _logger?.LogError("Components file {} is missing! No FSM component docs.", DOC_FILE_NAME);
            return;
        }

        Components.Clear();

        var doc = XDocument.Load(DOC_FILE_NAME);
        var root = doc.Root; // <Types>

        if (root is null)
            return;

        var components = new Dictionary<string, Component>();
        foreach (XElement? typeElement in root.Elements("Component"))
        {
            var component = DebugReadType(typeElement);
            if (component is not null)
                components[component.Name] = component;
        }

        Components = components;

        _logger.LogInformation("Loaded FSM component documentation with {} element(s).", Components.Count);
    }



    private Component? DebugReadType(XElement typeElement)
    {
        XAttribute? componentNameAttribute = typeElement.Attribute("Name");
        if (componentNameAttribute is null)
        {
            _logger.LogWarning("Component has missing or invalid 'Name' attribute.");
            return null;
        }

        var component = new Component
        {
            Name = componentNameAttribute.Value
        };

        XElement? descriptionElement = typeElement.Element("Description");
        if (descriptionElement is not null)
            component.Description = descriptionElement.Value;

        var propertiesElement = typeElement.Element("Properties");
        if (propertiesElement != null)
        {
            foreach (XElement? propElement in propertiesElement.Elements("Property"))
            {
                XAttribute? nameAttribute = propElement.Attribute("Name");
                if (nameAttribute is null)
                {
                    _logger.LogWarning("Property documentation for component {} has invalid or missing 'Name' attribute.", component.Name);
                    continue;
                }

                XElement? propertyDescriptionElement = propElement.Element("Description");
                if (propertyDescriptionElement is null)
                {
                    _logger.LogWarning("Property documentation for component {} has invalid or missing 'Description' element.", component.Name);
                    continue;
                }

                string name = nameAttribute.Value;
                string description = propertyDescriptionElement.Value;
                component.Properties[name] = description;
            }
        }

        XElement? childComponentsElement = typeElement.Element("ChildComponents");
        if (childComponentsElement != null)
        {
            component.ChildComponents = [];
            foreach (XElement? childElement in childComponentsElement.Elements("Type"))
            {
                Component? childType = DebugReadType(childElement);
                if (childType is not null)
                    component.ChildComponents.Add(childType);
            }
        }

        return component;
    }

    #region Debug Dumping

    /// <summary>
    /// Dumps all 'Description' attributes of types BehaviorTreeComponent into a XML doc file.
    /// </summary>
    public void DebugDump()
    {
        Dictionary<string, Type> types = [];

        foreach (Type type in Assembly.GetAssembly(typeof(BehaviorTreeComponent)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract))
        {
            types.TryAdd(type.Name, type);
        }

        foreach (Type type in Assembly.GetAssembly(typeof(TextDataFile)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract))
        {
            types.TryAdd(type.Name, type);
        }

        foreach (KeyValuePair<string, Type> type in types)
        {
            ProcessType("", type.Value);
        }

        void ProcessType(string prefix, Type type)
        {
            Component? component;

            var componentDescription = type.GetCustomAttribute<DescriptionAttribute>();
            if (componentDescription is not null  && !string.IsNullOrWhiteSpace(componentDescription.Description))
            {
                if (!Components.TryGetValue($"{prefix}{type.Name}", out component))
                {
                    component = new Component() { Name = type.Name };
                    Components[type.Name] = component;
                }

                component.Description = componentDescription.Description;
            }

            foreach (PropertyInfo prop in type.GetProperties())
            {
                var propDescription = prop.GetCustomAttribute<DescriptionAttribute>();
                if (propDescription is not null && !string.IsNullOrWhiteSpace(propDescription.Description))
                {
                    if (!Components.TryGetValue($"{prefix}{type.Name}", out component))
                    {
                        component = new Component() { Name = type.Name };
                        Components[type.Name] = component;
                    }

                    component.Properties.TryAdd(prop.Name, propDescription.Description);
                }
            }

            foreach (Type i in type.GetNestedTypes())
            {
                ProcessType($"{prefix}{type.Name}.", i);
            }
        }

        DebugWriteXml(DOC_FILE_NAME);
    }

    private void DebugWriteXml(string fileName)
    {
        using var xmlWriter = XmlWriter.Create(fileName, new XmlWriterSettings()
        {
            Indent = true,
        });
        xmlWriter.WriteStartDocument();

        xmlWriter.WriteStartElement("Types");
        foreach (var component in Components)
            DebugWriteType(xmlWriter, component.Value);
        xmlWriter.WriteEndElement();

        xmlWriter.WriteEndDocument();
    }

    private static void DebugWriteType(XmlWriter xmlWriter, Component component)
    {
        xmlWriter.WriteStartElement("Component");
        xmlWriter.WriteAttributeString("Name", component.Name);

        if (!string.IsNullOrWhiteSpace(component.Description))
            xmlWriter.WriteElementString("Description", component.Description);

        xmlWriter.WriteStartElement("Properties");
        {
            foreach (var prop in component.Properties)
            {
                xmlWriter.WriteStartElement("Property");
                xmlWriter.WriteAttributeString("Name", prop.Key);
                xmlWriter.WriteElementString("Description", prop.Value);
                xmlWriter.WriteEndElement();
            }
        }
        xmlWriter.WriteEndElement();

        if (component.ChildComponents?.Count > 0)
        {
            xmlWriter.WriteStartElement("ChildComponents");
            foreach (var childType in component.ChildComponents)
                DebugWriteType(xmlWriter, childType);
            xmlWriter.WriteEndElement();
        }

        xmlWriter.WriteEndElement();
    }
    #endregion

    public class Component
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string> Properties { get; set; } = [];
        public List<Component>? ChildComponents { get; set; }
    }
}
