using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ChessChallenge.Application
{
    public class SyntaxTreeHashGenerator
    {
        public static string GenerateHash(string code)
        {
            var codeWithoutVariablesAndComments = RemoveVariablesAndComments(code);

            // Generate a hash from the serialized string using SHA256
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(codeWithoutVariablesAndComments);
                var hashBytes = sha256.ComputeHash(bytes);
                var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hash;
            }
        }

        public static string RemoveVariablesAndComments(string code)
        {
            // Parse the C# code into a syntax tree
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Create a syntax tree visitor to remove variable names and comments
            var syntaxRewriter = new SyntaxRewriter();
            var modifiedSyntaxTree = syntaxRewriter.Visit(syntaxTree.GetRoot());

            // Serialize the modified syntax tree back into a string representation
            var codeWithoutVariablesAndComments = modifiedSyntaxTree.ToString();

            var codeWithNormalizedWhitespace = Regex.Replace(codeWithoutVariablesAndComments, @"\u00A0+", " ");

            return codeWithNormalizedWhitespace;
        }

        private class SyntaxRewriter : CSharpSyntaxRewriter
        {
            private int variableCounter = 0;
            private Dictionary<string, string> variableNameMap = new();

            // add special placeholder whitespace that can be replaced by a single space later to avoid changing whitespace in strings.
            public override SyntaxNode? Visit(SyntaxNode? node) => base.Visit(node)
                ?.WithLeadingTrivia(SyntaxFactory.Whitespace("\u00A0"))
                .WithTrailingTrivia(SyntaxFactory.Whitespace("\u00A0"));

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                return SyntaxFactory.Whitespace("\u00A0");
            }

            public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                // Assign unique names to variables in the syntax tree
                var resetVariables = node.Variables.Select(ResetVariableName);
                return base.VisitVariableDeclaration(node.WithVariables(SyntaxFactory.SeparatedList(resetVariables)));
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                // Replace identifier names with the assigned unique names
                if (variableNameMap.TryGetValue(node.Identifier.Text, out string? uniqueName))
                {
                    return node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, uniqueName, node.Identifier.TrailingTrivia));
                }

                return base.VisitIdentifierName(node);
            }

            private VariableDeclaratorSyntax ResetVariableName(VariableDeclaratorSyntax node)
            {
                // Assign a unique name to the variable
                string uniqueName = $"var{variableCounter++}";
                variableNameMap[node.Identifier.Text] = uniqueName;
                return node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, uniqueName, node.Identifier.TrailingTrivia));
            }
        }
    }
}