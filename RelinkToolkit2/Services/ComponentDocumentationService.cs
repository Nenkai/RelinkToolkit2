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
using System.Xml.Serialization;

namespace RelinkToolkit2.Services;

public class ComponentDocumentationService : IHostedService
{
    private readonly ILogger _logger;

    public static readonly string DOC_FILE_NAME = Path.Combine("Data", "component_doc.xml");

    public Dictionary<string, DocumentedType> Components { get; set; } = [];

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

    /// <summary>
    /// Adds or updates a description for a component with the specified key.
    /// </summary>
    /// <param name="key">Component key</param>
    /// <param name="value">Description text</param>
    public void SetComponentString(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key must not be null or empty.", nameof(key));

        DocumentedType component = GetOrCreateComponentByPath(key.AsSpan());
        component.Description = value;
    }

    /// <summary>
    /// Adds or updates a preview format string for a component with the specified key.
    /// </summary>
    /// <param name="key">Component key</param>
    /// <param name="value">Description text</param>
    public void SetComponentPreviewFormatString(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key must not be null or empty.", nameof(key));

        DocumentedType component = GetOrCreateComponentByPath(key.AsSpan());
        component.PreviewFormatString = value;
    }

    /// <summary>
    /// Adds or updates a description for the property with the specified key
    /// </summary>
    /// <param name="key">Property key (component path + "." + property name).</param>
    /// <param name="value">Description text to set.</param>
    public void SetPropertyString(string key, string? value)
    {
        ReadOnlySpan<char> span = key.AsSpan();
        int lastDot = span.LastIndexOf('.');
        if (lastDot < 0)
            throw new ArgumentException("Key must be in the form 'Component.Path.PropertyName'.", nameof(key));

        ReadOnlySpan<char> componentPath = span[..lastDot];
        ReadOnlySpan<char> propertyName = span[(lastDot + 1)..];

        DocumentedType component = GetOrCreateComponentByPath(componentPath);
        component.Properties[propertyName.ToString()] = value;
    }

    /// <summary>
    /// Gets a description string for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetComponentString(Type type)
    {
        var key = BuildTypeKey(type);
        DocumentedType? component = FindComponentByPath(key.AsSpan());
        return component?.Description;
    }

    /// <summary>
    /// Gets a preview format string for the specified type.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetComponentPreviewFormatString(Type type)
    {
        var key = BuildTypeKey(type);
        DocumentedType? component = FindComponentByPath(key.AsSpan());
        return component?.PreviewFormatString;
    }

    /// <summary>
    /// Gets a preview format string for the specified component key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetComponentPreviewFormatString(string key)
    {
        DocumentedType? component = FindComponentByPath(key.AsSpan());
        return component?.PreviewFormatString;
    }

    /// <summary>
    /// Gets a description string for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetComponentString(string key)
    {
        DocumentedType? component = FindComponentByPath(key.AsSpan());
        return component?.Description;
    }

    /// <summary>
    /// Gets a description string for the specified property descriptor.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetPropertyString(PropertyDescriptor propertyDescriptor)
    {
        var key = BuildPropertyKey(propertyDescriptor);
        return GetPropertyString(key);
    }

    /// <summary>
    /// Gets a description string for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>null if not found.</returns>
    public string? GetPropertyString(string key)
    {
        ReadOnlySpan<char> span = key.AsSpan();
        int lastDot = span.LastIndexOf('.');
        if (lastDot < 0)
            return null;

        ReadOnlySpan<char> componentPath = span[..lastDot];
        ReadOnlySpan<char> propertyName = span[(lastDot + 1)..];

        DocumentedType? component = FindComponentByPath(componentPath);
        if (component is null)
            return null;

        var props = component.Properties.GetAlternateLookup<ReadOnlySpan<char>>();
        if (props.TryGetValue(propertyName, out string? desc))
            return desc;

        return null;
    }

    /// <summary>
    /// Returns a key for the specified property descriptor
    /// </summary>
    /// <param name="propertyDescriptor"></param>
    /// <returns></returns>
    public string BuildPropertyKey(PropertyDescriptor propertyDescriptor)
    {
        return $"{BuildTypeKey(propertyDescriptor.ComponentType)}.{propertyDescriptor.Name}";
    }

    /// <summary>
    /// Returns a key for the specified type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string BuildTypeKey(Type type)
    {
        var names = new List<string>();

        Type? currentType = type;
        while (currentType is not null)
        {
            names.Add(currentType.Name);
            currentType = currentType.DeclaringType;
        }

        var sb = new StringBuilder();
        for (int i = names.Count - 1; i >= 0; i--)
        {
            if (sb.Length > 0)
                sb.Append('.');
            sb.Append(names[i]);
        }

        return sb.ToString();
    }

    private DocumentedType? FindComponentByPath(ReadOnlySpan<char> path)
    {
        var alternate = Components.GetAlternateLookup<ReadOnlySpan<char>>();

        DocumentedType? component = null;
        foreach (var range in path.Split('.'))
        {
            ReadOnlySpan<char> segment = path[range];

            if (component is null)
            {
                if (!alternate.TryGetValue(segment, out component))
                    return null;
            }
            else
            {
                component = FindChildComponent(component, segment);
                if (component is null)
                    return null;
            }
        }

        return component;
    }

    private DocumentedType GetOrCreateComponentByPath(ReadOnlySpan<char> path)
    {
        DocumentedType? component = null;

        foreach (var range in path.Split('.'))
        {
            ReadOnlySpan<char> segment = path[range];

            if (component is null)
            {
                var alternate = Components.GetAlternateLookup<ReadOnlySpan<char>>();
                if (!alternate.TryGetValue(segment, out component))
                {
                    string name = segment.ToString();
                    component = new DocumentedType { Name = name };
                    Components[name] = component;
                }
            }
            else
            {
                component.ChildComponents ??= [];

                DocumentedType? child = FindChildComponent(component, segment);
                if (child is null)
                {
                    child = new DocumentedType { Name = segment.ToString() };
                    component.ChildComponents[child.Name] = child;
                }

                component = child;
            }
        }

        return component!;
    }

    private static DocumentedType? FindChildComponent(DocumentedType parent, ReadOnlySpan<char> name)
    {
        var alternate = parent.ChildComponents.GetAlternateLookup<ReadOnlySpan<char>>();
        return alternate.TryGetValue(name, out DocumentedType? child) ? child : null;
    }

    public void LoadDocumentFile()
    {
        if (!File.Exists(DOC_FILE_NAME))
        {
            _logger?.LogError("Components file {} is missing! No FSM component docs.", DOC_FILE_NAME);
            return;
        }

        Components.Clear();

        try
        {
            using var fs = File.OpenText(DOC_FILE_NAME);
            XmlSerializer ser = new XmlSerializer(typeof(TypeDocumentationRoot));
            TypeDocumentationRoot root = (TypeDocumentationRoot)ser.Deserialize(fs);

            foreach (TypeDocumentation type in root.Components)
            {
                DocumentedType component = VisitType(type);
                Components.Add(type.Name, component);
            }

            static DocumentedType VisitType(TypeDocumentation typeDoc)
            {
                var component = new DocumentedType
                {
                    Name = typeDoc.Name,
                    Description = typeDoc.Description,
                    PreviewFormatString = typeDoc.PreviewFormatString,
                };

                foreach (TypePropertyDocumentation prop in typeDoc.Properties)
                {
                    component.Properties[prop.Name] = prop.Description;
                }

                foreach (TypeDocumentation child in typeDoc.ChildTypes)
                {
                    var subComponent = VisitType(child);
                    component.ChildComponents[subComponent.Name] = subComponent;
                }

                return component;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load documentation file at {}", DOC_FILE_NAME);
            throw;
        }

        _logger.LogInformation("Loaded FSM component documentation with {} element(s).", Components.Count);
    }

    public void SaveDocumentFile()
    {
        var root = new TypeDocumentationRoot
        {
            Components = new(Components.Count)
        };

        foreach (DocumentedType component in Components.Values)
        {
            var rootTypeDoc = CreateTypeDocumentation(component);
            root.Components.Add(rootTypeDoc);
        }

        XmlSerializer serializer = new(typeof(TypeDocumentationRoot));

        string? dirName = Path.GetDirectoryName(Path.GetFullPath(DOC_FILE_NAME));
        if (!Directory.Exists(dirName))
            Directory.CreateDirectory(dirName);

        using var fs = File.Create(DOC_FILE_NAME);
        serializer.Serialize(fs, root);

        _logger?.LogInformation("Saved FSM component documentation with {} element(s).", Components.Count);

        static TypeDocumentation CreateTypeDocumentation(DocumentedType component)
        {
            var typeDoc = new TypeDocumentation
            {
                Name = component.Name,
                Description = component.Description,
                PreviewFormatString = component.PreviewFormatString,
                Properties = new(component.Properties.Count),
                ChildTypes = new(component.ChildComponents.Count)
            };

            foreach (var property in component.Properties)
            {
                var propDoc = new TypePropertyDocumentation
                {
                    Name = property.Key,
                    Description = property.Value
                };
                typeDoc.Properties.Add(propDoc);
            }

            foreach (DocumentedType child in component.ChildComponents.Values)
            {
                var childTypeDoc = CreateTypeDocumentation(child);
                typeDoc.ChildTypes.Add(childTypeDoc);
            }

            return typeDoc;
        }
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
            DocumentedType? component;

            var componentDescription = type.GetCustomAttribute<DescriptionAttribute>();
            if (componentDescription is not null  && !string.IsNullOrWhiteSpace(componentDescription.Description))
            {
                if (!Components.TryGetValue($"{prefix}{type.Name}", out component))
                {
                    component = new DocumentedType() { Name = type.Name };
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
                        component = new DocumentedType() { Name = type.Name };
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
    }

    #endregion

    public class DocumentedType
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? PreviewFormatString { get; set; }
        public Dictionary<string, string?> Properties { get; set; } = [];
        public Dictionary<string, DocumentedType> ChildComponents { get; set; } = [];
    }
}
