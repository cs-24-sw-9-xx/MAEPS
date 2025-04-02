using System;
using System.Collections.Generic;
using System.Linq;

using Maes.Algorithms;
using Maes.Algorithms.Patrolling.Components;
using Maes.Utilities;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;

namespace Editor.Analyzers
{
    public sealed class ForbiddenKnowledgeAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal static readonly Descriptor IssueDescriptor = new Descriptor(
            "FAC0001",
            "Forbidden Knowledge",
            Areas.Quality,
            "Accessing forbidden property / method in Algorithm / Component is not allowed. Access to this information is not realistic.",
            "Avoid using the apis, see if there is an alternative.")
        {
            MessageFormat = "'{0}' uses forbidden knowledge ({1})",
            DefaultSeverity = Severity.Major,
        };

        private static readonly Type[] ForbiddenKnowledgeTypes = { typeof(IComponent), typeof(IAlgorithm) };
        private static readonly string[] ForbiddenKnowledgeTypesNames = ForbiddenKnowledgeTypes.Select(t => t.FullName).ToArray();

        private static readonly Type ForbiddenKnowledgeAttributeType = typeof(ForbiddenKnowledgeAttribute);
        private static readonly string ForbiddenKnowledgeAttributeTypeName = ForbiddenKnowledgeAttributeType.FullName;

        private static readonly Dictionary<TypeDefinition, bool> TypeCache = new Dictionary<TypeDefinition, bool>();

        public override IReadOnlyCollection<OpCode> opCodes { get; } = new[] { OpCodes.Call, OpCodes.Callvirt };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            TypeCache.Clear();

            registerDescriptor(IssueDescriptor);
        }

        // This code is run for every call or callvirt opcode in the whole program as defined in opCodes.
        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            var methodDefinition = context.MethodDefinition;
            var instruction = context.Instruction;
            if (!methodDefinition.HasBody)
            {
                return null;
            }

            var declaringType = methodDefinition.DeclaringType;

            // Special handling of IEnumerable methods with yield return statements.
            // The compiler rewrites the code to a statemachine, it has a special name (contains <MethodName>).
            // I'm not too happy about this, but it works.
            if (methodDefinition.FullName.Contains('<') && methodDefinition.Name == "MoveNext")
            {
                declaringType = declaringType.DeclaringType;
            }

            // Check if the type already exist in the cache.
            if (TypeCache.TryGetValue(declaringType, out var cachedValue))
            {
                if (!cachedValue)
                {
                    return null;
                }
            }
            // Type is not cached check its interfaces.
            else if (!ForbiddenKnowledgeTypesNames.Any(n => DoesMethodDeclareTypeImplementInterface(declaringType, n)))
            {
                TypeCache[declaringType] = false;
                return null;
            }

            TypeCache[declaringType] = true;

            // Extract the referenced method from the instruction.
            var referencedMethod = (MethodReference)instruction.Operand;
            var referencedMethodDefinition = referencedMethod.Resolve();

            // Some methods are implemented by the runtime and thus has no definition.
            // We are not interested in those.
            if (referencedMethodDefinition == null)
            {
                return null;
            }

            return HandleCallInstruction(referencedMethodDefinition, methodDefinition, context);
        }


        private static ReportItemBuilder HandleCallInstruction(MethodDefinition referencedMethodDefinition,
            MethodDefinition methodDefinition, InstructionAnalysisContext context)
        {
            // If the method is implements the getter or setter of a property
            // Use the attribute on the property itself.
            if (IsProperty(referencedMethodDefinition))
            {
                var property = GetProperty(referencedMethodDefinition);

                // Check if the property has the Forbidden Knowledge attribute
                if (property.CustomAttributes.All(a =>
                        a.AttributeType.FullName != ForbiddenKnowledgeAttributeTypeName))
                {
                    return null;
                }

                return context.CreateIssue(IssueCategory.Code, IssueDescriptor.Id, methodDefinition.Name,
                    property.Name);
            }

            // Check if the method has the Forbidden Knowledge attribute
            if (referencedMethodDefinition.CustomAttributes.All(a => a.AttributeType.FullName != ForbiddenKnowledgeAttributeTypeName))
            {
                return null;
            }

            return context.CreateIssue(IssueCategory.Code, IssueDescriptor.Id, methodDefinition.Name,
                referencedMethodDefinition.Name);
        }

        // Whether or not the method is a property getter or setter.
        private static bool IsProperty(MethodDefinition method)
        {
            var methodName = method.Name;
            return methodName.StartsWith("get_") || methodName.StartsWith("set_");
        }

        // Gets the property by looking it up by removing the 'get_' or 'set_' prefix.
        private static PropertyDefinition GetProperty(MethodDefinition method)
        {
            var methodName = method.Name;
            var propertyName = methodName["get_".Length..];

            return method.DeclaringType.Properties.First(p => p.Name == propertyName);
        }

        // Whether or not a type directly or indirectly implements an interface.
        private static bool DoesMethodDeclareTypeImplementInterface(TypeDefinition declaringType, string interfaceFullName)
        {
            if (ImplementsInterface(declaringType, interfaceFullName))
            {
                return true;
            }

            // Check if any of the base types implement the interface
            while (declaringType.BaseType != null)
            {
                declaringType = declaringType.BaseType.Resolve();
                if (ImplementsInterface(declaringType, interfaceFullName))
                {
                    return true;
                }
            }

            return false;
        }

        // Whether or not a type directly implements an interface.
        private static bool ImplementsInterface(TypeDefinition type, string interfaceFullName)
        {
            // Check if the current type implements the interface directly
            foreach (var @interface in type.Interfaces)
            {
                // Compare the full name of the interface
                if (@interface.InterfaceType.FullName == interfaceFullName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}