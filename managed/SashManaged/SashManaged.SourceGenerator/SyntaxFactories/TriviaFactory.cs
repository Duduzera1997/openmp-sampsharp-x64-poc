﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SashManaged.SourceGenerator.SyntaxFactories;

/// <summary>
/// Creates trivia syntax.
/// </summary>
public static class TriviaFactory
{
    public static SyntaxTrivia AutoGeneratedComment() => SyntaxFactory.Comment("// <auto-generated />");
}