﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RestClient
{
    public class Document
    {
        private Dictionary<string, string>? _variables = null;

        private Document()
        { }

        public List<Token> Tokens { get; } = new List<Token>();

        public List<Token> Hierarchy { get; } = new List<Token>();

        public Dictionary<string, string>? Variables
        {
            get
            {
                if (_variables == null)
                {
                    ExpandVariables();
                }

                return _variables;
            }
        }

        public IEnumerable<Request> Requests => Hierarchy.OfType<Request>();

        public static Document FromLines(params string[] lines)
        {
            var doc = new Document();
            var parser = new Tokenizer(doc);

            parser.Parse(lines);

            doc.CreateHierarchyOfChildren();
            doc.ValidateDocument();

            return doc;
        }

        private void ValidateDocument()
        {
            // Variable references
            foreach (Token? token in Tokens)
            {
                foreach (Reference reference in token.References)
                {
                    if (Variables != null && reference.Value != null && !Variables.ContainsKey(reference.Value.Text))
                    {
                        reference.Errors.Add($"The variable \"{reference.Value.Text}\" is not defined.");
                    }
                }
            }

            // URLs
            foreach (Url? url in Tokens.OfType<Url>())
            {
                var uri = url.Uri?.ExpandVariables();

                if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
                {
                    url.Errors.Add($"\"{uri}\" is not a valid absolute URI");
                }
            }
        }

        private void CreateHierarchyOfChildren()
        {
            Request? currentRequest = null;

            foreach (Token? token in Tokens)
            {
                if (token is Variable)
                {
                    Hierarchy.Add(token);
                }

                else if (token is Url url)
                {
                    currentRequest = new Request(token.Start, token.Text, this)
                    {
                        Url = url
                    };

                    Hierarchy.Add(currentRequest);
                    currentRequest?.Children?.Add(url);
                }

                else if (currentRequest != null)
                {
                    if (token is Header header)
                    {
                        currentRequest?.Headers?.Add(header);
                        currentRequest?.Children?.Add(header);
                    }
                    else if (token is BodyToken body)
                    {
                        if (string.IsNullOrWhiteSpace(body.Text))
                        {
                            continue;
                        }

                        var prevEmptyLine = body.Previous is BodyToken && string.IsNullOrWhiteSpace(body.Previous.Text) ? body.Previous.Text : "";

                        if (currentRequest.Body != null)
                        {
                            currentRequest.Body.Increase(prevEmptyLine + body.Text);
                        }
                        else
                        {
                            currentRequest.Body = body;
                            currentRequest?.Children?.Add(body);
                        }
                    }
                    else if (token is Comment comment)
                    {
                        if (comment.IsSeparator)
                        {
                            currentRequest = null;
                            Hierarchy.Add(comment);
                        }
                        else
                        {
                            currentRequest?.Children?.Add(comment);
                        }
                    }
                }
                else
                {
                    if (token is Comment)
                    {
                        Hierarchy.Add(token);
                    }
                }
            }
        }

        private void ExpandVariables()
        {
            IEnumerable<Variable>? variables = Tokens.OfType<Variable>();

            Dictionary<string, string> expandedVars = new();

            // Start by expanding all variable definitions
            foreach (Variable variable in variables)
            {
                var value = variable.Value!.Text;

                foreach (var key in expandedVars.Keys)
                {
                    value = value.Replace("{{" + key + "}}", expandedVars[key].Trim());
                }

                expandedVars[variable.Name!.Text] = value;
            }

            _variables = expandedVars;
        }

        public Token? GetTokenFromPosition(int position)
        {
            Token token = Tokens.LastOrDefault(t => t.IntersectsWith(position));

            if (token is Url url && url.Uri!.IntersectsWith(position))
            {
                return GetVariableFromPosition(url.Uri, position);
            }

            if (token is Header header)
            {
                if (header.Name!.IntersectsWith(position))
                {
                    return GetVariableFromPosition(header.Name, position);
                }

                if (header.Value!.IntersectsWith(position))
                {
                    return GetVariableFromPosition(header.Value, position);
                }
            }

            if (token is BodyToken body)
            {
                return GetVariableFromPosition(body, position);
            }

            return token;
        }

        private Token? GetVariableFromPosition(Token token, int position)
        {
            return token.References.FirstOrDefault(v => v.IntersectsWith(position));
        }
    }
}
