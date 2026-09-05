using Avalonia.Platform.Storage;

using GBFRDataTools.FSM;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services.FSM;

public class FSMFileService
{
    private readonly ILogger? _logger;
    private readonly ILoggerFactory _loggerFactory;

    public FSMFileService(ILogger<FSMFileService>? logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    private string? _fsmPath;
    public async Task VerifyAllBTs(string behaviorTreeFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorTreeFolderPath);

        foreach (var file in Directory.GetFiles(behaviorTreeFolderPath, "*.msg", SearchOption.AllDirectories))
        {
            var parser = new BTParser(_loggerFactory);

            try
            {
                parser.Parse(File.ReadAllBytes(file), file.EndsWith(".msg"));
                if (parser.HasErrors)
                {
                    _logger?.LogError("BT Parsed ({}) with errors.", file);
                }
                else
                    _logger?.LogInformation("BT Parsed ({}).", file);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception while parsing BT");
            }
        }
    }

    public async Task VerifyAllFSMs(string fsmFolderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fsmFolderPath);

        _fsmPath = fsmFolderPath;

        foreach (var file in Directory.GetFiles(_fsmPath, "*.msg", SearchOption.AllDirectories))
        {
            _logger?.LogInformation(file);

            var parser = new FSMParser(App.Current.Services.GetRequiredService<ILoggerFactory>());
            await parser.Parse(File.ReadAllBytes(file), file.EndsWith(".msg"), OnLocateFSMFile);
            if (parser.HasErrors)
            {
                _logger?.LogError("FSM Parsed ({}) with errors.", file);
            }
            else
                _logger?.LogInformation("FSM Parsed ({}).", file);
        }
    }

    public async Task<string?> OnLocateFSMFile(string className, string fsmName)
    {
        string file = $"{className}_{fsmName}_fsm_ingame.msg";
        return $"{_fsmPath}/{className}/{file}";
    }
}
