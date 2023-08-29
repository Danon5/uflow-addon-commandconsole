using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace UFlow.Addon.CommandConsole.Runtime
{
    public class CommandManager
    {
        private readonly Dictionary<string, List<MethodInfo>> m_commands = new();
        private readonly Dictionary<Type, IParser> m_parsers = new();
        private readonly Trie m_trie;
        private readonly ILogger m_logger;

        public CommandManager(ILogger logger, List<string> assemblyNames)
        {
            m_logger = logger;
            List<string> commandNames = new List<string>();

            assemblyNames.Add(typeof(CommandManager).Assembly.FullName);
            foreach (var methodInfo in GetMethodsWithAttribute<CommandAttribute>(assemblyNames))
            {
                CommandAttribute commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>();
                m_commands.TryAdd(commandAttribute.name, new List<MethodInfo>());
                m_commands[commandAttribute.name].Add(methodInfo);
                
                commandNames.Add(commandAttribute.name);
            }
            
            m_trie = new Trie(commandNames);

            foreach (Type type in GetAllTypesAssignableFrom<IParser>(assemblyNames))
            {
                IParser parser = (IParser) Activator.CreateInstance(type);
                if (m_parsers.ContainsKey(parser.Type)) continue;
                m_parsers.Add(parser.Type, parser);
            }
        }

        protected virtual List<string> SplitParameters(string parametersString) // Parsers need to deal with spaces
        {
            List<string> parameters = new List<string>();

            parametersString.TrimStart(' '); // end is already trimmed of spaces
            if (parametersString.Length == 0) return parameters;

            bool insideQuote = false;

            int currentParamStart = 0;
            bool startedParam = false;
            int unclosedParentheses = 0;
            for (int i = 0; i < parametersString.Length; i++)
            {
                char c = parametersString[i];
                bool isQuote = c == '"';

                if (!char.IsWhiteSpace(c) && !startedParam)
                {
                    currentParamStart = i;
                    startedParam = true;
                }

                if (isQuote)
                    insideQuote = !insideQuote;
                else if (!insideQuote)
                {
                    unclosedParentheses += c == '(' ? 1 : 0;
                    unclosedParentheses -= c == ')' ? 1 : 0;
                }
                
                if (i == parametersString.Length - 1 || parametersString[i + 1] == ' ' && unclosedParentheses == 0 && !insideQuote)
                {
                    string subStr = parametersString.SubstringRange(currentParamStart, i);
                    subStr = subStr.Trim('\"');
                    parameters.Add(subStr);
                    startedParam = false;
                }
            }

            return parameters;
        }

        public void Log(in string message, LogMessageType type = LogMessageType.Message) => m_logger.Log(message, type);
        
        public void SendCommandLine(string commandLine)
        {
            if (commandLine.Length == 0) throw new WarningException("Empty command");

            commandLine = commandLine.Trim();
            ParseCommandLine(commandLine, out string commandName, out List<string> parameters);
            
            TryInvokeCommand(commandName, parameters.ToArray());
        }

        // FIX THIS TO NOT PARSE THE COMMAND, let SplitParameters output the command name
        protected virtual void ParseCommandLine(string commandLine, out string commandName, out List<string> parameters)
        {
            int firstSpace = commandLine.IndexOf(' ');
            if (firstSpace == -1)
            {
                commandName = commandLine;
                parameters = new List<string>();
            }
            else
            {
                commandName = commandLine.Substring(0, firstSpace);
                parameters = SplitParameters(commandLine.Substring(firstSpace + 1));
            }

            //Debug.Log($"Name: {commandName}, parameters: {parameters.StringEnumerate(" ", s => s)}");
        }
        
        private void TryInvokeCommand(string commandName, params string[] parameters)
        {
            if (!m_commands.TryGetValue(commandName, out List<MethodInfo> possibleMethods))
            {
                Log($"Unknown command '{commandName}'", LogMessageType.Error);
                return;
            }

            MethodInfo info = null;

            foreach (MethodInfo possibleMethod in possibleMethods)
            {
                if (IsValidOverload(possibleMethod, parameters))
                {
                    info = possibleMethod;
                    break;
                }
            }

            if (info == null)
            {
                Log($"No overload for command '{commandName}' matches given parameters.", LogMessageType.Error);
                return;
            }
            
            ParameterInfo[] funcParameters = info.GetParameters();
            object[] castedParameters = new object[funcParameters.Length];
            
            for (int i = 0; i < funcParameters.Length; i++)
            {
                Type paramType = funcParameters[i].ParameterType;
                object paramValue;

                if (paramType == typeof(CommandManager))
                    paramValue = this;
                else if (i >= parameters.Length)
                    paramValue = funcParameters[i].DefaultValue;
                else
                {
                    paramValue = m_parsers[paramType].Parse(parameters[i]); //TypeDescriptor.GetConverter(paramType).ConvertFromString(parameters[i]);
                }

                castedParameters[i] = paramValue;
            }
            
            info.Invoke(null, castedParameters);
        }

        public List<string> GetAllCommandNames() => m_trie.GetAllWords();
        public int SuggestCommands(string prefix, string[] suggestions, bool includePrefixIfFullWord = false) =>
            m_trie.SuggestNonAlloc(prefix, suggestions, includePrefixIfFullWord);
        
        public List<string> SuggestCommands(string prefix, bool includePrefixIfFullWord = false)
            => m_trie.Suggest(prefix, includePrefixIfFullWord);
        
        private bool TryGetPossibleMethods(string commandName, out List<MethodInfo> possibleMethods, bool log = true)
        {
            if (!m_commands.TryGetValue(commandName, out possibleMethods))
            {
                if (log)
                    Log($"Unknown command '{commandName}'", LogMessageType.Error);
                return false;
            }

            return true;
        }

        [UsedImplicitly]
        [Command("help", "Provides help information for commands.", "[command name]")]
        private static void HelpCommand(string command, CommandManager manager)
        {
            string helpString = "";

            if (!manager.TryGetPossibleMethods(command, out List<MethodInfo> possibleMethods, false))
            {
                manager.Log($"No command found with name '{command}', to see a list of all commands use 'list'.", 
                    LogMessageType.Error);
                return;
            }

            for (var i = 0; i < possibleMethods.Count; i++)
            {
                var info = possibleMethods[i];
                CommandAttribute commandAttribute = info.GetCustomAttribute<CommandAttribute>();
                
                if (commandAttribute.hasPrototype && commandAttribute.hasDescription)
                    helpString += $"{commandAttribute.description}\n\t{command} {commandAttribute.prototype}";
                else if (commandAttribute.hasPrototype && !commandAttribute.hasDescription)
                    helpString += $"\t{command} {commandAttribute.prototype}";
                else if (!commandAttribute.hasPrototype && commandAttribute.hasDescription)
                    helpString += $"{commandAttribute.description}";
                else
                    continue;
                
                if (i != possibleMethods.Count - 1)
                    helpString += "\n";
            }

            if (helpString == string.Empty)
                helpString = $"There is no help page created for '{command}'.";

            manager.Log(helpString);
        }

        [UsedImplicitly]    
        [Command("help", "Provides help information for the 'help' command.")]
        private static void HelpCommand(CommandManager manager)
        {
            HelpCommand("help", manager);
        }

        [UsedImplicitly]
        [Command("list", "Lists all commands.")]
        private static void ListAllCommands(CommandManager manager)
        {
            var listStr = new System.Text.StringBuilder();
            
            List<string> commands = manager.m_commands.Keys.ToList();
            for (int i = 0; i < commands.Count; i++)
            {
                listStr.Append(commands[i]);
                if (i != commands.Count - 1)
                    listStr.Append("\n");
            }

            manager.Log(listStr.ToString());
        }

        [ItemCanBeNull]
        private static List<Type> GetAllTypesAssignableFrom<T>(List<string> assemblyNames)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string name in assemblyNames)
            {
                assemblies.Add(Assembly.Load(name));
            }

            List<Type> types = new List<Type>();
            foreach (Assembly assembly in assemblies)
            {
                types.AddRange(assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface).ToArray());
            }

            return types;
        }
        
        private static List<MethodInfo> GetMethodsWithAttribute<T>(List<string> assemblyNames) where T : Attribute
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string name in assemblyNames)
            {
                assemblies.Add(Assembly.Load(name));
            }

            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Assembly assembly in assemblies)
            {
                 methods.AddRange(assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                                                      | BindingFlags.Static | BindingFlags.Instance))
                    .Where(m => m.GetCustomAttributes(typeof(T), false).Length > 0));
            }

            return methods;
        }

        private static int GetTypeArrayHashCode(in Type[] types)
        {
            var hash = types[0].GetHashCode();

            for (var i = 1; i < types.Length; i++)
                hash = HashCode.Combine(hash, types[i].GetHashCode());

            return hash;
        }
        
        private bool IsValidOverload(in MethodInfo method, in string[] inputParams)
        {
            var allParameters = method.GetParameters().Where(info => info.ParameterType != typeof(CommandManager)).ToArray();
            var requiredParameters = allParameters.Where(param => !param.IsOptional).ToArray();

            if (inputParams.Length < requiredParameters.Length || inputParams.Length > allParameters.Length)
                return false;

            return CanConvertParameters(allParameters, inputParams);
        }
        

        private bool CanConvertParameters(in ParameterInfo[] parameters, in string[] inputParams)
        {
            for (var i = 0; i < inputParams.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (!m_parsers.TryGetValue(parameterType, out IParser parser) || !parser.TryParse(inputParams[i], out object parsedStr))
                    return false;
            }

            return true;
        }
        
        private static Type[] GetTypesFromMethodInfo(MethodInfo info) =>
            info.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToArray();
    }
}