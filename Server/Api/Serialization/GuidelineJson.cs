// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AccessService.Api.Serialization;

/// <summary>
/// Central System.Text.Json configuration for the JSON blob columns of the guideline projection
/// (MappingsJson, ComplexDataJson, DomainJson, RelationsJson, AssignmentJson, ExtraJson).
/// <para>
/// Two things make the Guideline.Model types awkward for a plain serializer, both handled here:
/// their properties are declared as interfaces (System.Text.Json cannot instantiate those), and the
/// ComplexData tree is cyclic via <c>IComplexDataTreeNode.Parent</c>.
/// </para>
/// <para>
/// The guideline <em>file</em> itself is never (de)serialized with these options — that is
/// <c>GuidelineReaderWriter</c>'s job, which owns the on-the-wire schema.
/// </para>
/// </summary>
public static class GuidelineJson
{
    private static readonly Assembly ModelAssembly = typeof(Guideline.Model.Model.Guideline).Assembly;

    /// <summary>Interface → single concrete implementation, resolved once per interface.</summary>
    private static readonly ConcurrentDictionary<Type, Type?> ImplementationCache = new();

    /// <summary>
    /// The options used for every blob column. <see cref="ReferenceHandler.Preserve"/> mirrors the
    /// previous Newtonsoft behaviour (<c>PreserveReferencesHandling.All</c>) and is what keeps the
    /// cyclic ComplexData tree serializable; it emits <c>$id</c>/<c>$ref</c>/<c>$values</c>, so blobs
    /// must always be read back through these same options.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { UseModelTypeFactories }
            }
        };
    }

    /// <summary>
    /// Serializes a value to a compact JSON string, or <see langword="null"/> for a null value.
    /// The runtime type is used, so anonymous types and interface-typed values both round-trip.
    /// </summary>
    public static string? SerializeCompact(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, value.GetType(), Options);
    }

    /// <summary>
    /// Deserializes a blob written by <see cref="SerializeCompact"/>, or <see langword="null"/> for
    /// a null/empty blob.
    /// </summary>
    public static T? Deserialize<T>(string? json) where T : class
    {
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Gives the serializer a way to construct the Guideline.Model types, which it cannot do on its own:
    /// <list type="bullet">
    /// <item>An interface with exactly one concrete implementation is constructed as that implementation.
    /// Interfaces with several (<c>IProperty</c>, <c>IPropertyAssignment</c>) are left alone — those are
    /// never stored under their interface type; the transformation writes them as concrete types with an
    /// explicit discriminator instead.</item>
    /// <item>A concrete type without a parameterless constructor (<c>ComplexDataTreeNode</c>) is built via
    /// its constructor with default arguments and then filled through its setters. Reference preservation
    /// cannot use parameterized constructors, and that type is exactly where the cycle lives.</item>
    /// </list>
    /// </summary>
    private static void UseModelTypeFactories(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type.Assembly != ModelAssembly || typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        if (typeInfo.Type.IsInterface)
        {
            var implementation = ResolveImplementation(typeInfo.Type);
            if (implementation is not null)
            {
                typeInfo.CreateObject = CreateFactory(implementation);
                AddMissingProperties(typeInfo, implementation);
            }

            return;
        }

        if (typeInfo.CreateObject is null && typeInfo.Type.GetConstructor(Type.EmptyTypes) is null)
        {
            typeInfo.CreateObject = CreateFactory(typeInfo.Type);
        }

        AddMissingProperties(typeInfo, typeInfo.Type);
    }

    /// <summary>
    /// Makes every readable/writable property of <paramref name="source"/> part of the contract, including
    /// the ones the default resolver left out. The model marks <c>ComplexDataItem.Root</c> with
    /// <c>[JsonIgnore]</c> — an annotation the previous Newtonsoft serializer did not honour, so the blobs
    /// used to contain the ComplexData tree. Dropping it here would silently truncate the ExtraJson of
    /// tree-shaped properties, so the blob keeps storing everything the model carries.
    /// <para>
    /// An ignored property is still present in the contract, only with no accessors — that case is
    /// re-activated rather than added a second time.
    /// </para>
    /// </summary>
    private static void AddMissingProperties(JsonTypeInfo typeInfo, Type source)
    {
        foreach (var property in source.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetMethod is null || property.SetMethod is null
                || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var existing = typeInfo.Properties.FirstOrDefault(p => p.Name == property.Name);
            if (existing is not null)
            {
                // Only touch properties that are fully ignored; leave deliberately read-only ones alone.
                if (existing.Get is null && existing.Set is null)
                {
                    existing.Get = property.GetValue;
                    existing.Set = property.SetValue;
                    existing.ShouldSerialize = null;
                }

                continue;
            }

            var jsonProperty = typeInfo.CreateJsonPropertyInfo(property.PropertyType, property.Name);
            jsonProperty.Get = property.GetValue;
            jsonProperty.Set = property.SetValue;
            typeInfo.Properties.Add(jsonProperty);
        }
    }

    /// <summary>
    /// Builds a parameterless factory for <paramref name="type"/>, falling back to its shortest
    /// constructor with default arguments when it has no parameterless one.
    /// </summary>
    private static Func<object> CreateFactory(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) is not null)
        {
            return () => Activator.CreateInstance(type)!;
        }

        var constructor = type.GetConstructors()
            .OrderBy(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Type '{type.FullName}' has no public constructor.");

        var defaults = constructor.GetParameters()
            .Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)
            .ToArray();

        return () => constructor.Invoke((object?[])defaults.Clone());
    }

    private static Type? ResolveImplementation(Type interfaceType)
    {
        return ImplementationCache.GetOrAdd(interfaceType, static type =>
        {
            var implementations = ModelAssembly.GetExportedTypes()
                .Where(t => t is { IsInterface: false, IsAbstract: false }
                            && type.IsAssignableFrom(t)
                            && t.GetConstructors().Length > 0)
                .ToList();

            return implementations.Count == 1 ? implementations[0] : null;
        });
    }
}
