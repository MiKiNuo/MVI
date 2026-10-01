using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MiKiNuo.Mvi.Generators;

internal static class StateShapeValidation
{
    internal static bool IsImmutable(ITypeSymbol type, Compilation compilation, HashSet<ITypeSymbol> visiting, out ISymbol invalid)
    {
        invalid = type;
        if (type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_String
            or SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal
            || type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return IsImmutable(named.TypeArguments[0], compilation, visiting, out invalid);
        }

        if (SymbolEqualityComparer.Default.Equals(named.ContainingAssembly, compilation.GetSpecialType(SpecialType.System_Int32).ContainingAssembly)
            && named.ContainingNamespace.ToDisplayString() == "System"
            && named.Name is "Guid" or "DateTime" or "DateTimeOffset" or "TimeSpan" or "DateOnly" or "TimeOnly")
        {
            return true;
        }

        if (named.ContainingAssembly.Name == "System.Collections.Immutable"
            && named.ContainingNamespace.ToDisplayString() == "System.Collections.Immutable"
            && named.Name is "ImmutableArray" or "ImmutableList" or "ImmutableQueue" or "ImmutableStack")
        {
            return named.TypeArguments.All(argument => IsImmutable(argument, compilation, visiting, out _));
        }

        if (!named.IsRecord || named.DeclaringSyntaxReferences.IsEmpty
            || (named.IsReferenceType ? !named.IsSealed : !named.IsReadOnly))
        {
            return false;
        }

        if (!visiting.Add(named))
        {
            return true;
        }

        for (INamedTypeSymbol? current = named; current is not null && current.IsRecord; current = current.BaseType)
        {
            if (current.DeclaringSyntaxReferences.IsEmpty)
            {
                invalid = current;
                return false;
            }

            foreach (ISymbol member in current.GetMembers().Where(static member => !member.IsStatic && !member.IsImplicitlyDeclared))
            {
                if (member is IFieldSymbol or IEventSymbol)
                {
                    invalid = member;
                    return false;
                }

                if (member is IPropertySymbol property
                    && (property.IsIndexer || property.ReturnsByRef || property.ReturnsByRefReadonly
                        || (property.SetMethod is not null && !property.SetMethod.IsInitOnly)
                        || !IsImmutable(property.Type, compilation, visiting, out invalid)))
                {
                    invalid = property;
                    return false;
                }
            }
        }

        visiting.Remove(named);
        return true;
    }
}
