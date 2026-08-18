# C# / Script Scanner v1.0

Implemented lexical source analysis for:
- namespaces
- classes/structs/interfaces/enums
- base types
- attributes
- RequireComponent
- common Unity lifecycle methods
- SerializeField / SerializeReference / public fields
- diagnostics

This is intentionally not yet a full Roslyn semantic model.
Next semantic layers can add symbol resolution, method call graphs, field/type references,
assembly semantics, Unity API classification, and compile diagnostics.
