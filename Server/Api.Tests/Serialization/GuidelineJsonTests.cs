// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Serialization;
using Guideline.Model.Model;

namespace AccessService.Api.Tests.Serialization;

/// <summary>
/// Guards the System.Text.Json configuration used for the projection's JSON blob columns.
/// The two things that would silently break it are covered here: interface-typed members
/// (System.Text.Json cannot instantiate those on its own) and the cyclic ComplexData tree.
/// </summary>
public class GuidelineJsonTests
{
	[Fact]
	public void RoundTrip_InterfaceTypedCollection_ResolvesConcreteImplementation()
	{
		var mappings = new List<IMapping>
		{
			new Mapping
			{
				ID = "map-1",
				ClassificationMap = new ClassificationMapping { SourceClassificationName = "Wall" }
			}
		};

		var json = GuidelineJson.SerializeCompact(mappings);
		var restored = GuidelineJson.Deserialize<List<IMapping>>(json);

		Assert.NotNull(restored);
		var single = Assert.Single(restored!);
		Assert.IsType<Mapping>(single);
		Assert.Equal("map-1", single.ID);
		// ClassificationMap is itself interface-typed and must resolve too.
		Assert.Equal("Wall", single.ClassificationMap!.SourceClassificationName);
	}

	[Fact]
	public void RoundTrip_CyclicComplexDataTree_PreservesParentReference()
	{
		// ComplexDataTreeNode.Parent points back at its owner. Without reference preservation
		// this would either throw or recurse until the depth limit.
		// ComplexDataTreeNode has no parameterless constructor — another thing the options handle.
		var root = new ComplexDataTreeNode("root", 0, null);
		var child = new ComplexDataTreeNode("child", 1, root);
		root.Children = new List<IComplexDataTreeNode> { child };

		var item = new ComplexDataItem { Identifier = "item-1", Root = root };

		var json = GuidelineJson.SerializeCompact(item);
		var restored = GuidelineJson.Deserialize<ComplexDataItem>(json);

		Assert.NotNull(restored);
		Assert.Equal("item-1", restored!.Identifier);
		Assert.Equal("root", restored.Root!.Name);

		var restoredChild = Assert.Single(restored.Root.Children!);
		Assert.Equal("child", restoredChild.Name);
		// The cycle is restored as an actual reference, not as a duplicated node.
		Assert.Same(restored.Root, restoredChild.Parent);
	}

	[Fact]
	public void RoundTrip_ComplexData_WithInterfaceTypedItems()
	{
		var complexData = new ComplexData
		{
			Identifier = "cd-1",
			Name = "Complex",
			Items = new List<IComplexDataItem>
			{
				new ComplexDataItem { Identifier = "item-1", Root = new ComplexDataTreeNode("root", 0, null) }
			}
		};

		var json = GuidelineJson.SerializeCompact(complexData);
		var restored = GuidelineJson.Deserialize<ComplexData>(json);

		Assert.NotNull(restored);
		Assert.Equal("cd-1", restored!.Identifier);
		var item = Assert.Single(restored.Items!);
		Assert.Equal("item-1", item.Identifier);
		Assert.Equal("root", item.Root!.Name);
	}

	[Fact]
	public void SerializeCompact_Null_ReturnsNull()
	{
		Assert.Null(GuidelineJson.SerializeCompact(null));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Deserialize_NullOrEmpty_ReturnsNull(string? json)
	{
		Assert.Null(GuidelineJson.Deserialize<ComplexData>(json));
	}

	[Fact]
	public void RoundTrip_AnonymousType_MatchesWhatTheTransformationWrites()
	{
		// The transformation writes several blobs (RelationsJson, AssignmentJson, …) from
		// anonymous types; those must serialize by runtime type, not as an empty object.
		var json = GuidelineJson.SerializeCompact(new { ParentId = "p-1", ChildIds = new List<string> { "c-1", "c-2" } });

		Assert.NotNull(json);
		using var document = System.Text.Json.JsonDocument.Parse(json!);
		Assert.Equal("p-1", document.RootElement.GetProperty("ParentId").GetString());
	}
}
