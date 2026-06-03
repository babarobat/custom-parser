namespace CustomParser.Parser;

internal abstract record AstNode;

internal sealed record AccessPathNode(string Root, IReadOnlyList<AccessSegment> Segments) : AstNode;

internal abstract record AccessSegment;

internal sealed record MemberSegment(string Name) : AccessSegment;

internal sealed record IndexSegment(int Index) : AccessSegment;

internal sealed record ParsedPlaceholder(AstNode Expression, string? Format);
