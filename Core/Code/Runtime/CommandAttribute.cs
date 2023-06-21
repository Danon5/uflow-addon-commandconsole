using System;

namespace UFlow.Addon.CommandConsole.Runtime
{
     [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
     public sealed class CommandAttribute : Attribute
     {
          public readonly string name;
          public readonly string description;
          public readonly string prototype;
          public readonly bool hasDescription;
          public readonly bool hasPrototype;

          public CommandAttribute(string name)
          {
               this.name = name;
               hasDescription = false;
               hasPrototype = false;
          }

          public CommandAttribute(string name, string description)
          {
               this.name = name;
               this.description = description;
               hasDescription = true;
               hasPrototype = false;
          }
          
          public CommandAttribute(string name, string description, string prototype)
          {
               this.name = name;
               this.description = description;
               this.prototype = prototype;
               hasDescription = description != string.Empty;
               hasPrototype = prototype != string.Empty;
          }
     }
}
