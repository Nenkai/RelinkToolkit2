using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using RelinkToolkit2.Services.Configuration.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services.Configuration;

public class ConfigurationService
{
    public static readonly string DefaultPath = Path.GetFullPath(Path.Combine("config", "toolkit_config.json"));

    private readonly RootConfig _root = new();

    public ConfigurationService(IOptions<RootConfig> options)
    {
        _root = options.Value;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DefaultPath)!);

        var json = JsonSerializer.Serialize(_root, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(DefaultPath, json);
    }
}
