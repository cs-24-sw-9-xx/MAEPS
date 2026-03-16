using System.Diagnostics.CodeAnalysis;

namespace MAEPS.Data.Processor.Utilities;

public class ArgumentParser
{
    private readonly Dictionary<string, string> _arguments = new();
    
    public void ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No arguments provided.");
        }
        
        for (var i = 0; i < args.Length; i += 2)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    var value = args[i + 1];
                    _arguments[key] = value;
                }
                else
                {
                    throw new ArgumentException($"Invalid argument for {args[i]}");
                }
            }
            else
            {
                throw new ArgumentException($"Invalid argument {args[i]}");
            }
        }
    }
    
    public delegate bool ParserFunc<T>([NotNullWhen(true)]string? value, out T result);
    public T GetArgument<T>(string key, ParserFunc<T> parserFunc)
    {
        if (!_arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Argument {key} not found.");
        }

        if (parserFunc(value, out var parsedValue))
        {
            return parsedValue;
        }
        throw new ArgumentException($"Argument {value} for --{key} is not of type {typeof(T)}.");
    }
    
    public T GetArgument<T>(string key, ParserFunc<T> parserFunc, T defaultValue)
    {
        if (!_arguments.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (parserFunc(value, out var parsedValue))
        {
            return parsedValue;
        }
        throw new ArgumentException($"Argument {value} for --{key} is not of type {typeof(T)}.");
    }
    
    public string GetArgument(string key)
    {
        if (!_arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Argument {key} not found.");
        }
        
        return value;
    }
    
    public string[] GetArgumentList(string key)
    {
        if (!_arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Argument {key} not found.");
        }

        value = value.Trim('(', ')');
        
        
        return value.Split(',').Select(v => v.Trim()).ToArray();
    }
    
    public string[] GetArgumentList(string key, string[] defaultValue)
    {
        if (!_arguments.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        value = value.Trim('(', ')');
        
        
        return value.Split(',').Select(v => v.Trim()).ToArray();
    }
}