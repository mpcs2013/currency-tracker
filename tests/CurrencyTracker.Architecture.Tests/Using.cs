global using FluentAssertions;
global using NetArchTest.Rules;
global using Xunit;

// Anchors imported by their fully-qualified namespaces inside each [Fact];
// no global using for them — the typeof(...) expressions need the full path
// to keep "what layer does this test inspect?" visible at the call site.
